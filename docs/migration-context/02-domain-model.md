# Domain Model

## Entidades detectadas

### UserClient

Tabla `UserClient`. Representa usuarios del sistema, incluyendo clientes y administrador.

Campos relevantes:

- `UserID`.
- `UserName`.
- `UserPassword`.
- `FirstName`.
- `Lastname`.
- `UserEmail`.
- `RoleID`.

Modelo actual aproximado: `Models/ClientDetails.cs`.

Observaciones:

- `UserPassword` es `varchar(25)`, insuficiente para SHA-256 hex completo.
- `UserEmail` tambien es corto para correos reales.
- `RoleID` mezcla identidad de cliente/admin en una tabla simple.

### Role

Tabla `Role` con roles basicos.

Campos:

- `RoleID`.
- `RoleDescription`.

Uso actual:

- `RoleID = 1`: administrador.
- `RoleID = 2`: cliente.

### Business

Tabla `Business`. Representa un negocio que puede crear tarjetas de lealtad para clientes.

Campos:

- `BusinessID`.
- `BusinessName`.
- `BusinessPassword`.
- `BusinessEmail`.
- `BusinessLogo`.

Modelos actuales:

- `Models/BusinessDetails.cs`.
- `Models/AdminDetails.cs`.

Observaciones:

- Negocios tienen credenciales separadas del login de clientes/admin.
- `BusinessPassword` tambien es `varchar(25)`.
- `BusinessLogo` guarda una ruta relativa a archivo local.

### ClientCard

Tabla `ClientCard`. Es la relacion cliente-negocio-tarjeta de lealtad.

Campos:

- `CardID`.
- `CardIDGoogle`.
- `CreationDate`.
- `CheckQTY`.
- `LastCheck`.
- `UserID`.
- `BusinessID`.
- `HistoricCheckQTY`.

Modelo actual aproximado: `Models/CardsDetails.cs`.

Observaciones:

- `CardIDGoogle` guarda el suffix/identificador local del objeto Google Wallet.
- No hay campo equivalente para Apple Wallet en la tabla, aunque `CardsDetails` tiene `CardIDApple`.
- La relacion evita duplicados cliente-negocio por stored procedure, no por indice unico explicito.

### ApplePass

Tabla `ApplePass`. Es un intento inicial de persistencia para Apple Wallet.

Campos:

- `IDPass`.
- `SerialNumber`.
- `AuthToken`.
- `PushToken`.
- `CreationDate`.
- `CheckQTY`.
- `LastCheck`.
- `IDUser`.
- `BusinessID`.

Observaciones:

- No tiene foreign keys hacia `UserClient` o `Business`.
- El stored procedure `spInsertPassData` usa `UserID` en el insert, pero la tabla define `IDUser`; eso parece bug.
- El procedure ignora parametros de usuario/negocio y contiene valores fijos.
- No hay codigo C# que lo use.

### PasswordResetToken

Tabla `PasswordResetToken`.

Campos:

- `UserEmail`.
- `Token`.
- `Expiration`.

Uso actual:

- `RequestPasswordResetPage.aspx.cs` genera token.
- `ClientConnector.StorePasswordResetToken` guarda token.
- `ResetPasswordPage.aspx.cs` valida token, actualiza password y borra token.

## Modelos actuales

- `ClientDetails`: usuario/cliente con id, username, password, nombres, email y rol.
- `BusinessDetails`: negocio autenticado con id, nombre, password, email y logo.
- `AdminDetails`: DTO para alta/modificacion de negocio.
- `CardsDetails`: tarjeta con contadores, negocio, usuario y campos `CardIDGoogle`/`CardIDApple`.

Los modelos son DTOs anemicos y no expresan reglas de dominio. Las reglas viven en paginas Web Forms, conectores y stored procedures.

## Relacion usuario-negocio-tarjeta-sellos

Relaciones actuales:

- `UserClient` 1 a N `ClientCard`.
- `Business` 1 a N `ClientCard`.
- `ClientCard` representa una tarjeta unica para un cliente en un negocio.
- `ClientCard.CheckQTY` representa sellos actuales.
- `ClientCard.HistoricCheckQTY` representa sellos historicos acumulados.
- `ClientCard.LastCheck` representa la ultima fecha de sello.

Flujo:

1. Cliente se registra en `UserClient`.
2. Negocio se registra en `Business`.
3. Negocio asocia cliente con negocio mediante `ClientCard`.
4. La asociacion inicial crea `CheckQTY = 1` y `HistoricCheckQTY = 1`.
5. Cada sello llama `spIncreaseCheckQTY`.
6. Google Wallet se actualiza con el estado de `ClientCard`.

## Stored procedures usados o inferidos

Usados por conectores:

- `AdminConnector`: `spGetBusinessData`, `spInsertBusinessData`, `spGetBusinessDetails`, `spModifyBusinessData`, `spDeleteBusinessData`.
- `BusinessConnector`: `spGetCardDataBusiness`, `spSelectBusinessData`, `spGetUserClientInfo`, `spGetUserClientID`, `spGetCardCreatedTime`, `spInsertCardData`, `spGetLast5Checks`, `spIncreaseCheckQTY`, `spGetCardDataChecks`, `spGetYearData`.
- `ClientConnector`: `spInsertUserClientData`, `spSelectUserClientData`, `spUpdateClientPassword`, `spCheckIfEmailExist`, `spDeletePasswordResetToken`, `spStorePasswordResetToken`, `spValidateResetToken`, `spGetEmailByToken`, `spGetClientCards`.

Detectados pero no claramente usados:

- `GetBusinessDetailsByUserID`.
- `spGetCardDataClient`.
- `spGetUserNamesAutocomplete`.
- `spInsertPassData`.

## Campos relacionados con Wallets

Google Wallet:

- `ClientCard.CardIDGoogle`: suffix del objeto Google.
- Codigo `GW-Methods/REST-API_Loyalty.cs`: issuer hardcodeado, clase/objeto, JWT y patch.
- Carpeta `GW-K/*.json`: service account.
- Asset `Resources/GoogleButton/...png`: boton de correo.

Apple Wallet:

- `CardsDetails.CardIDApple`: propiedad existente, no persistida en `ClientCard`.
- `ApplePass.SerialNumber`.
- `ApplePass.AuthToken`.
- `ApplePass.PushToken`.
- `ApplePass.CreationDate`.
- `ApplePass.CheckQTY`.
- `ApplePass.LastCheck`.
- `ApplePass.IDUser`.
- `ApplePass.BusinessID`.
- `Tools/EmailFormat.html`: placeholder `{AppleURL}`.
- No se encontraron certificados, `.pkpass`, manifest/signature, pass type id, team id ni endpoints web service de Apple Wallet.

## Modelo recomendado para la migracion

Entidades de dominio recomendadas:

- `User`: identidad base o wrapper sobre ASP.NET Core Identity.
- `ClientProfile`: datos de cliente visibles para negocios.
- `Business`: negocio con nombre, email, logo, estado y configuracion.
- `BusinessUser`: credenciales/usuarios que administran un negocio, si se separa del cliente.
- `LoyaltyCard`: relacion cliente-negocio, estado de tarjeta, fechas y contadores.
- `StampLedgerEntry`: evento inmutable de sello agregado, canje o ajuste.
- `WalletPass`: abstraccion de una tarjeta emitida en una plataforma.
- `GoogleWalletPass`: ids de clase/objeto, estado, ultimo sync.
- `AppleWalletPass`: serial number, authentication token hash, push token, device registrations y ultimo paquete generado.
- `EmailMessage` o `Notification`: tracking de correos enviados, plantilla y estado.

Value objects sugeridos:

- `EmailAddress`.
- `BusinessSlug`.
- `WalletPlatform`: `Google`, `Apple`.
- `WalletObjectId`.
- `ApplePassSerialNumber`.
- `StampCount`.

Reglas recomendadas:

- Una `LoyaltyCard` activa por par `ClientProfile` + `Business`.
- La cantidad de sellos actuales se deriva de eventos o se mantiene como cache consistente.
- El historico no debe reducirse cuando los sellos actuales se reinician.
- La emision Wallet debe ser idempotente por plataforma.
- Actualizar sellos debe persistir primero el dominio y luego sincronizar Wallet con reintentos.
- Los tokens de Apple deben guardarse como hashes cuando sea posible.

Modelo de persistencia recomendado:

- Mantener compatibilidad inicial con MySQL.
- Introducir EF Core o Dapper con migraciones controladas. EF Core sirve bien si se quiere evolucionar el modelo; Dapper puede ser puente si se preservan stored procedures al inicio.
- Crear columnas/tabla nuevas para Wallet sin romper `ClientCard` actual.
- Agregar indice unico para `ClientCard(UserID, BusinessID)`.

