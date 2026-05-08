# Wallet Analysis

## Estado actual de Google Wallet

Hay una implementacion real, aunque acoplada, en `DigitalCardsApp/GW-Methods/REST-API_Loyalty.cs`.

Capacidades detectadas:

- Autenticacion con Google service account.
- Creacion de clase Google Wallet al registrar negocio.
- Creacion de objeto Wallet al asociar cliente con negocio.
- Generacion de JWT para link `Save to Google Wallet`.
- Patch del objeto al agregar sellos.
- Uso de `Google.Apis.Walletobjects.v1`.
- Uso de `System.IdentityModel.Tokens.Jwt` para firma del link.

Flujo actual:

1. `AdminInsertionPage.aspx.cs` guarda negocio.
2. Luego llama `Loyalty.CreateClass` con un suffix derivado del nombre del negocio.
3. `BusinessInsertionPage.aspx.cs` genera un `CardIDGoogle` aleatorio.
4. Guarda `ClientCard`.
5. Llama `Loyalty.CreateObject`.
6. Llama `Loyalty.CreateJWTExistingObjects`.
7. Envia correo con link Google Wallet.
8. `BusinessCheckPage.aspx.cs` incrementa sellos y llama `Loyalty.PatchObject`.

Datos persistidos:

- `ClientCard.CardIDGoogle`: suffix local del objeto.
- La clase Google se deriva del nombre de negocio, pero no parece persistirse explicitamente como `GoogleClassId`.
- No se persiste el link JWT, estado de sync ni errores de Google Wallet.

## Riesgos e inconsistencias en Google Wallet

- El JSON de service account esta dentro del repo en `GW-K/*.json`. Debe considerarse comprometido y rotarse.
- El issuer id esta hardcodeado en codigo. No se copio aqui.
- `CreateObject` revisa existencia usando `Loyaltyobject.Get`, pero inserta con `Genericobject.Insert`; esa mezcla es inconsistente.
- Se usa `GenericClass` y `GenericObject` para una tarjeta de lealtad; puede ser valido, pero debe decidirse si conviene `LoyaltyClass/LoyaltyObject` o `Generic` segun producto.
- `CreateClass` devuelve `"Success"` al insertar, pero devuelve ids u otros valores en ramas de error/existencia. El calling code no maneja estados de forma robusta.
- Si la base de datos guarda `ClientCard` pero Google falla, no hay compensacion, retry ni estado pendiente.
- `CardIDGoogle` es texto aleatorio de 10 caracteres con `Random`, no criptograficamente fuerte.
- Las imagenes de hero/logo apuntan a URLs externas fijas, no necesariamente controladas por el negocio.
- No hay persistencia de version, `saveUri`, ultima sincronizacion, ultimo error ni plataforma preferida del cliente.
- No hay tests automatizados para el link generado ni para actualizacion de sellos.

## Estado actual de Apple Wallet

Apple Wallet no esta implementado de punta a punta.

Evidencia existente:

- Tabla `ApplePass`.
- Stored procedure `spInsertPassData`.
- Propiedad `CardsDetails.CardIDApple`.
- Plantilla `Tools/EmailFormat.html` con `{AppleURL}`.

No se encontro:

- Generacion de archivo `.pkpass`.
- `pass.json`.
- `manifest.json`.
- Firma `signature`.
- Certificados Apple Wallet, `.p12`, `.pem`, `.cer` o llaves relacionadas.
- Pass Type Identifier.
- Team Identifier.
- Endpoint para descargar pass.
- Endpoints Apple Wallet Web Service para registrar dispositivos, actualizar seriales, recibir push tokens o servir passes actualizados.
- Logica C# que use `ApplePass`.
- Envio real de link Apple en correo.

## Problemas especificos Apple detectados

- `ApplePass` tiene columna `IDUser`, pero `spInsertPassData` intenta insertar en `UserID`. Eso no coincide.
- `spInsertPassData` declara parametros para usuario/negocio/sellos, pero usa valores fijos en el insert.
- `ApplePass` no tiene foreign keys hacia `UserClient` y `Business`.
- `AuthToken` y `PushToken` se guardarian en claro si se usara la tabla actual.
- No hay almacenamiento de `deviceLibraryIdentifier`, requerido para el web service de Apple Wallet.
- No hay estrategia de push notifications via APNs.

## Que falta para produccion

Google Wallet:

- Mover credenciales a secret store y rotarlas.
- Persistir `GoogleIssuerId`, `GoogleClassId`, `GoogleObjectId`, estado, timestamps y errores.
- Hacer idempotentes `CreateClass`, `CreateObject` y `PatchObject`.
- Agregar reintentos y cola/worker para sincronizacion.
- Definir si se usara `GenericObject` o `LoyaltyObject`.
- Usar assets propios por negocio, con URLs HTTPS estables.
- Manejo de errores visible para soporte.

Apple Wallet:

- Certificado Pass Type ID, WWDR certificate y material de firma seguro.
- Generador `.pkpass` con `pass.json`, assets, `manifest.json` y `signature`.
- Endpoint de descarga para Apple Wallet.
- Web service Apple Wallet:
  - registrar dispositivo para un pass;
  - desregistrar dispositivo;
  - listar seriales actualizados;
  - descargar pass actualizado por serial;
  - autenticar con token del pass.
- APNs para notificar cambios.
- Persistencia de device registrations y push tokens.
- Politica de rotacion/renovacion de certificados.

## Endpoints necesarios

Endpoints publicos orientativos:

- `GET /wallet/select/{cardEnrollmentToken}`: landing desde email con deteccion de plataforma y botones.
- `GET /wallet/google/{cardEnrollmentToken}`: genera o devuelve link Google Wallet.
- `GET /wallet/apple/{cardEnrollmentToken}`: genera y descarga `.pkpass`.
- `GET /api/cards/{cardId}/wallet-state`: estado actual para UI o soporte.

Apple Wallet web service:

- `POST /apple-wallet/v1/devices/{deviceLibraryIdentifier}/registrations/{passTypeIdentifier}/{serialNumber}`.
- `DELETE /apple-wallet/v1/devices/{deviceLibraryIdentifier}/registrations/{passTypeIdentifier}/{serialNumber}`.
- `GET /apple-wallet/v1/devices/{deviceLibraryIdentifier}/registrations/{passTypeIdentifier}`.
- `GET /apple-wallet/v1/passes/{passTypeIdentifier}/{serialNumber}`.
- `POST /apple-wallet/v1/log`.

Endpoints internos/admin:

- `POST /business/cards/{cardId}/stamps`: agregar sello.
- `POST /wallet/sync/{cardId}`: reintento manual de sincronizacion.
- `GET /admin/wallet-errors`: monitoreo de fallas.

## Datos a almacenar por plataforma

Comun:

- `LoyaltyCardId`.
- `BusinessId`.
- `ClientId`.
- `Platform`.
- `Status`.
- `CreatedAt`.
- `UpdatedAt`.
- `LastSyncedAt`.
- `LastSyncError`.

Google:

- `IssuerId` o referencia de configuracion.
- `ClassId`.
- `ObjectId`.
- `SaveUrlIssuedAt` si se quiere auditar.
- `PayloadVersion`.

Apple:

- `PassTypeIdentifier`.
- `TeamIdentifier` como referencia no secreta.
- `SerialNumber`.
- `AuthenticationTokenHash`.
- `LastPackageHash`.
- `LastPassGeneratedAt`.
- `DeviceLibraryIdentifier`.
- `PushToken`.
- `RegisteredAt`.
- `UnregisteredAt`.

## Recomendaciones Google Wallet

- Crear `IGoogleWalletService` en `Application` e implementarlo en `Infrastructure`.
- Separar creacion de clase, creacion de objeto, generacion de link y patch.
- Persistir ids completos y evitar reconstruirlos desde nombre de negocio.
- Cambiar sufijos aleatorios a ids deterministas o UUIDs controlados.
- Agregar tests de contrato para payloads sin llamar a Google.
- Agregar adaptador con `IHttpClientFactory` o SDK inyectado, logging y retry.
- No enviar correo hasta tener objeto Google creado o estado `PendingWalletSync` claro.

## Recomendaciones Apple Wallet

- Implementar Apple desde cero como modulo propio; no intentar completar el stored procedure actual como base principal.
- Usar una libreria probada para generar `.pkpass` o encapsular bien la firma con pruebas binarias.
- Guardar certificados fuera del repo y cargarlos desde secret store.
- Disenar primero el contrato de `ApplePass` y `AppleDeviceRegistration`.
- Agregar endpoint de descarga antes de push, luego web service completo y APNs.
- Probar en dispositivo iOS real ademas de tests automatizados, porque Wallet valida firma/certificados de forma estricta.

