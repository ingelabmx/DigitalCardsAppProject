# DigitalCardsApp

## Ejecutar local

```powershell
cd C:\Users\eguillen\source\repos\DigitalCardsAppProject
dotnet build DigitalCardsApp.Modern.sln
dotnet run --project src\DigitalCards.Web\DigitalCards.Web.csproj --launch-profile http
```

La app escucha en:

```text
http://localhost:5031
```

La pagina principal `/` es la puerta de entrada publica de Puntelio:

- clientes entran a `/Client/Login` o crean cuenta desde `/Register`;
- negocios entran a `/Business/Login`;
- admins entran a `/Admin/Login`;
- si una cookie de cliente, negocio o admin ya existe, la home muestra una accion para continuar a su dashboard.

Los tres logins usan un shell visual comun con link de regreso a `/`, manteniendo cookies separadas para admin, negocio y cliente.
El registro general `/Register` y el registro por negocio `/Enroll/{businessToken}`
usan una experiencia publica guiada; el enrolamiento por negocio muestra branding
cuando el negocio ya tiene nombre publico, logo o colores configurados.
Los dashboards incluyen estados vacios accionables para guiar al admin, negocio
o cliente cuando todavia no hay datos operativos.
Los formularios POST aplican UX compartida de validacion/envio para evitar
doble-submit y enfocar errores visibles.
El panel admin mantiene paridad visual incremental con Web Forms mediante
comandos principales, filtros en paneles y estados operativos mas escaneables.
El negocio tiene una ruta visual diaria: buscar o escanear cliente, validar la
tarjeta y agregar sello desde herramientas optimizadas para mostrador.

Si hay una instancia previa corriendo:

```powershell
Get-Process DigitalCards.Web -ErrorAction SilentlyContinue | Stop-Process
```

## Configuracion real unica

La app moderna carga una sola configuracion externa real:

```text
C:\Users\eguillen\.digitalcards\appsettings.Local.json
```

Ese archivo queda fuera del repo y debe contener MySQL HostGator, Google
Wallet, Apple Wallet, SMTP y `PublicBaseUrl`.

Para crear una base segura desde el ejemplo sin copiar secretos al repo:

```powershell
New-Item -ItemType Directory "$env:USERPROFILE\.digitalcards" -Force
Copy-Item docs\config\appsettings.local.example.json "$env:USERPROFILE\.digitalcards\appsettings.Local.json"
notepad "$env:USERPROFILE\.digitalcards\appsettings.Local.json"
```

Validar que el JSON sea correcto sin imprimir secretos:

```powershell
Get-Content "$env:USERPROFILE\.digitalcards\appsettings.Local.json" -Raw | ConvertFrom-Json | Out-Null
```

Para pruebas automatizadas con fakes, se puede ignorar esta configuracion real:

```powershell
$env:DigitalCards__SkipUserLocalConfiguration='true'
```

## Cloudflare con `app.puntelio.com`

`app.puntelio.com` es la URL canonica para correos, Google Wallet y Apple
Wallet. Apple Wallet guarda esta URL dentro del `.pkpass`, asi que no debe
cambiarse despues de instalar tarjetas.

Crear tunnel nombrado:

```powershell
cloudflared tunnel login
cloudflared tunnel create puntelio-app
cloudflared tunnel route dns puntelio-app app.puntelio.com
```

Config local sugerida de Cloudflare:

```yaml
tunnel: puntelio-app
credentials-file: C:\Users\eguillen\.cloudflared\<TUNNEL_ID>.json

ingress:
  - hostname: app.puntelio.com
    service: http://localhost:5031
  - service: http_status:404
```

Ejecutar:

```powershell
cloudflared tunnel run puntelio-app
dotnet run --project src\DigitalCards.Web\DigitalCards.Web.csproj --launch-profile http
```

Validar:

```powershell
Invoke-WebRequest https://app.puntelio.com/health -UseBasicParsing
Invoke-WebRequest https://app.puntelio.com/health/ready -UseBasicParsing
```

En `appsettings.Local.json`, usar:

```json
{
  "DigitalCards": {
    "PublicBaseUrl": "https://app.puntelio.com",
    "GoogleWallet": {
      "Origins": ["https://app.puntelio.com"]
    }
  }
}
```

Tambien agrega `https://app.puntelio.com` como origin permitido en Google
Wallet.

Runbook completo:

```text
docs/migration-context/17-puntelio-single-environment.md
```

## Production readiness local

Para que cookies de negocio sobrevivan reinicios, configura Data Protection
fuera del repo:

```json
{
  "DigitalCards": {
    "Operations": {
      "EnableForwardedHeaders": true,
      "TrustAllForwardedHeaders": false,
      "KnownProxies": [],
      "DataProtectionKeysPath": "C:\\Users\\eguillen\\.digitalcards\\data-protection-keys",
      "RequireDataProtectionKeysForReadiness": true
    }
  }
}
```

`/health` valida que la app este viva. `/health/ready` valida configuracion
critica y MySQL cuando `PersistenceProvider=MySql`.

Runbook:

```text
docs/migration-context/19-production-readiness.md
```

## Hosting operativo de `app.puntelio.com`

Para operar app y tunnel con pasos repetibles, usa:

```powershell
.\ops\windows\start-puntelio-app.ps1
.\ops\windows\start-puntelio-tunnel.ps1
.\ops\windows\check-puntelio-health.ps1
```

Para operar en background con PID/logs:

```powershell
.\ops\windows\start-puntelio-stack.ps1
.\ops\windows\get-puntelio-status.ps1
.\ops\windows\show-puntelio-logs.ps1
```

Reinicio y paro:

```powershell
.\ops\windows\restart-puntelio-stack.ps1
.\ops\windows\stop-puntelio-stack.ps1
```

Estos scripts no instalan servicios ni imprimen secretos. El runbook para
operacion estable, con logs, reinicio, Data Protection y rollback, esta en:

```text
docs/migration-context/36-production-service-hosting-v1.md
docs/migration-context/50-ops-service-hosting-v2.md
```

## Login negocio moderno

El flujo ASP.NET Core moderno usa cookie auth para negocio:

- Login: `http://localhost:5031/Business/Login`
- Dashboard protegido: `http://localhost:5031/Business/Dashboard`
- Tarjetas y sellos: `http://localhost:5031/Business/Cards`
- Branding Wallet: `http://localhost:5031/Business/Branding`
- Logout: `http://localhost:5031/Business/Logout`

Las paginas `/Business/Dashboard`, `/Business/Enroll`, `/Business/Cards` y
`/Business/Stamp` requieren cookie valida. `/Business/Branding` tambien requiere
cookie valida y negocio habilitado en piloto. Ya no se debe pasar `businessId`
por URL ni por hidden input; el negocio se toma desde los claims de la sesion.

## Operacion moderna de tarjetas

El flujo recomendado para negocio es `/Business/Cards`:

1. buscar cliente por username o correo;
2. abrir la tarjeta cliente-negocio;
3. revisar sellos, Google Wallet, Apple Wallet y dispositivos Apple;
4. reenviar el correo/link Wallet si hace falta;
5. agregar sello desde el detalle de la tarjeta.

La accion de sello valida que la tarjeta pertenezca al negocio autenticado.
Web Forms sigue vivo como fallback, pero el dashboard moderno ya dirige la
operacion de sellos a `Tarjetas y sellos`.

`/Business/Dashboard` muestra un resumen operativo seguro del negocio
autenticado:

- tarjetas recientes;
- sellos actuales e historicos del lote reciente;
- conteo Google Wallet y Apple Wallet;
- alertas recientes de Wallet;
- eventos recientes de `StampLedger`.

El dashboard no muestra `businessId`, tokens Wallet, JWTs, push tokens,
passwords ni connection strings. Para operar una tarjeta, abre
`/Business/Cards` desde el link de tarjeta reciente.

Las pantallas de negocio usan el shell visual tipo Web Forms con sidebar,
cards, tablas/listas y acciones de operacion diaria. `/Business/Cards` tambien
incluye un lector QR progresivo: si el navegador soporta `BarcodeDetector` y
camara, llena la busqueda existente; si no, el negocio sigue usando username o
correo.

## Auditoria de sellos

Antes de probar auditoria contra HostGator, ejecuta:

```text
docs/migration-context/21-stamp-ledger-v1-hostgator.sql
```

La tabla `StampLedger` registra sellos modernos y cambios detectados por
`LegacyWalletSync`. El detalle de `/Business/Cards` muestra los ultimos eventos
con origen, sellos antes/despues y estado de updates Apple/Google.

No hay backfill historico. La auditoria empieza desde el despliegue de este
PR y no guarda tokens, JWTs, push tokens, passwords ni connection strings.

## Wallet links opacos

Los correos nuevos ya no deben exponer `CardID` directo. Antes de probar contra
HostGator, ejecuta:

```text
docs/migration-context/20-wallet-link-token-hardening-hostgator.sql
```

Configuracion segura por default:

```json
{
  "DigitalCards": {
    "WalletLinks": {
      "AllowLegacyCardIdTokens": false
    }
  }
}
```

Los links nuevos usan tokens opacos guardados solo como hash. Si necesitas una
emergencia temporal para links antiguos por `CardID`, cambia
`AllowLegacyCardIdTokens` a `true` en `appsettings.Local.json` y vuelve a
`false` al terminar la ventana de soporte.

## Piloto controlado

Cuando `app.puntelio.com` use datos reales, activa el piloto para que solo los
negocios habilitados usen las pantallas modernas. El negocio habilitado es quien
asocia/habilita al cliente dentro de su programa:

```json
{
  "DigitalCards": {
    "Pilot": {
      "Enabled": true,
      "AllowedBusinessIds": [],
      "AllowedBusinessEmails": ["NEGOCIO_TEST_EMAIL"]
    }
  }
}
```

Con el piloto activo:

- negocios fuera del allowlist pueden iniciar sesion, pero ven bloqueo en
  `/Business/Dashboard`, `/Business/Enroll`, `/Business/Cards` y
  `/Business/Stamp`;
- un negocio habilitado puede asociar clientes desde `/Business/Enroll` y operar
  sus tarjetas desde `/Business/Cards`;
- Wallet landing, Apple Wallet Web Service y descargas `.pkpass` siguen
  publicas por token/autorizacion propia.

La landing Wallet publica usa branding de negocio cuando existe: logo, nombre
publico, colores, sellos visuales y botones Apple/Google responsive.

Rollback rapido:

```json
{
  "DigitalCards": {
    "Pilot": {
      "Enabled": false
    },
    "LegacyWalletSync": {
      "Enabled": false
    }
  }
}
```

Runbook:

```text
docs/migration-context/18-pilot-readiness.md
```

## Admin piloto

Antes de administrar negocios piloto desde la app moderna contra HostGator,
ejecuta:

```text
docs/migration-context/22-admin-pilot-management-hostgator.sql
```

El admin moderno usa usuarios legacy de `UserClient` con `RoleID=1`.

- Login admin: `http://localhost:5031/Admin/Login`
- Dashboard admin: `http://localhost:5031/Admin/Dashboard`
- Administradores: `http://localhost:5031/Admin/AdminUsers`
- Crear admin: `http://localhost:5031/Admin/CreateAdmin`
- Negocios piloto: `http://localhost:5031/Admin/Businesses`
- Soporte: `http://localhost:5031/Admin/Support`
- Reportes: `http://localhost:5031/Admin/Reports`
- Auditoria: `http://localhost:5031/Admin/Audit`
- Crear negocio: `http://localhost:5031/Admin/CreateBusiness`
- Administrar negocio: `http://localhost:5031/Admin/BusinessProfile/{businessId}`

Con `DigitalCards:Pilot:Enabled=true`, un negocio puede usar el flujo moderno
si esta habilitado en `ModernPilotBusiness` o si sigue en el allowlist temporal
de `appsettings.Local.json`. La recomendacion operativa es mover los negocios
a `/Admin/Businesses` y dejar `AllowedBusinessEmails`/`AllowedBusinessIds` solo
como fallback. Ya no hay allowlist operativo de clientes: el admin habilita el
negocio y el negocio habilitado asocia al cliente desde `/Business/Enroll` u
opera la tarjeta desde `/Business/Cards`.

## Reportes de negocio

`/Business/Reports` muestra un resumen read-only del negocio autenticado:

- tarjetas y clientes recientes;
- sellos actuales, historicos y eventos por periodo;
- Wallets Google/Apple emitidas o pendientes;
- alertas Wallet recientes.

El `BusinessID` se toma de la cookie del negocio. La pantalla no acepta
`businessId` por query string y no muestra tokens, push tokens, passwords,
hashes ni connection strings.

## Desactivacion de negocio

El admin puede marcar un negocio como `Inactivo` desde
`/Admin/BusinessProfile/{businessId}`. Ese estado no borra el negocio ni toca
Web Forms, pero bloquea el login moderno de `/Business/Login` y no emite cookie
de negocio. Para reactivar, cambiar el estado a `PilotModern` o
`ModernPrimary`.

`LegacyRetired` no bloquea el flujo moderno. Es un guardrail operativo: admin y
soporte muestran advertencias para recordar que Web Forms debe bloquearse
manualmente para ese negocio hasta que exista automatizacion legacy.

## Perfil de cliente

`/Client/Profile` permite que el cliente autenticado actualice nombre, apellido
y correo dentro de los limites legacy de `UserClient`. El username queda fijo
para no romper flujos existentes, soporte ni referencias operativas.

## QR real de cliente

`/Client/Dashboard` y `/Client/Cards` muestran un QR real con el `UserName`
del cliente. El negocio puede escanearlo para buscar/asociar la tarjeta en el
flujo moderno. El QR no contiene tokens Wallet, passwords, CardID ni datos
secretos.

## Modo mostrador

`/Business/CheckIn` permite al negocio escanear el QR real del cliente o
capturar username/correo, validar la tarjeta asociada al negocio autenticado y
agregar sello desde una pantalla rapida. Usa la misma logica de sellos que
`/Business/Cards`, por lo que conserva updates Apple/Google y `StampLedger`.

## Consentimiento de cliente

`/Register` y `/Enroll/{businessToken}` requieren aceptar terminos y privacidad.
La aceptacion se guarda en `ModernClientConsent` con version de politica,
origen y fecha. Para MySQL aplica manualmente:

```text
docs/migration-context/69-public-enrollment-consent-hostgator.sql
```

## Soporte admin

`/Admin/Support` permite buscar por cliente, negocio o tarjeta para revisar:

- sellos actuales e historicos;
- estado Google Wallet y Apple Wallet;
- dispositivos Apple registrados;
- ultimos eventos de `StampLedger`;
- ultimos errores seguros de Wallet;
- conteo de eventos detectados por `LegacyWalletSync`;
- estado de configuracion de `LegacyWalletSync`.

Tambien permite filtrar por negocio, cliente, rango de fecha y tarjetas con
alertas Wallet. Los exports `JSON` y `CSV` son para evidencia interna de
soporte. Es una vista solo lectura. No muestra tokens Wallet, enrollment
tokens, push tokens, JWTs, passwords, hashes, certificados ni connection
strings.

## Auditoria operativa

`/Admin/Audit` muestra eventos sensibles del flujo moderno: creacion/reset de
admins, creacion/edicion de negocios, cambios de branding, cambios piloto o
cutover, exports de soporte y reintentos Wallet. Requiere cookie admin y no
muestra passwords, hashes, tokens, JWTs, push tokens, certificados, rutas
locales ni connection strings.

Para MySQL/HostGator aplica manualmente:

```text
docs/migration-context/66-operational-audit-log-hostgator.sql
```

## Paridad con Web Forms

La paridad se controla por flujo, no por fecha. El checklist vivo esta en:

```text
docs/migration-context/35-legacy-parity-checklist.md
```

Antes de retirar una ruta Web Forms, el flujo moderno debe tener pruebas,
smoke real, diagnostico en `/Admin/Support` y rollback documentado.

El plan operativo para mover negocios de `Legacy Only` a `Modern Primary` esta
en:

```text
docs/migration-context/37-gradual-webforms-replacement.md
```

El inventario visual Web Forms que guia la paridad de UI moderna esta en:

```text
docs/migration-context/41-legacy-ui-inventory-v1.md
```

Las paginas autenticadas modernas ya usan un shell con sidebar/header/footer
inspirado en Web Forms. Detalle:

```text
docs/migration-context/42-modern-legacy-shell-v1.md
```

Las pantallas admin tienen ajustes iniciales de paridad visual en:

```text
docs/migration-context/43-admin-ui-parity-v1.md
```

La regla de trabajo es reemplazar Web Forms por negocio, no globalmente. Web
Forms sigue vivo como fallback hasta que cada negocio complete los gates de
paridad, smoke real, soporte y rollback.

## Dashboard cliente

El cliente moderno entra desde:

```text
http://localhost:5031/Client/Login
```

El login usa `UserClient.RoleID=2` y crea una cookie separada
`.DigitalCards.Client`. Desde `/Client/Dashboard`, el cliente abre
`/Client/Cards` para ver solo sus propias tarjetas. Los links Wallet mostrados
ahi usan tokens opacos nuevos y no exponen `CardID` directo.

El dashboard cliente muestra:

- perfil basico: usuario y correo;
- identificador visual estilo QR;
- conteo de tarjetas;
- sellos actuales e historicos;
- estado Google Wallet y Apple Wallet;
- vista previa de tarjetas con link Wallet.

`/Client/Cards` muestra el detalle por tarjeta, incluyendo ultimo sello,
dispositivos Apple registrados, sellos visuales y links Wallet seguros. La UI
usa el shell legacy con sidebar/header/footer para mantener paridad visual con
Web Forms.

## Password hardening cliente

Antes de usar hashes modernos de cliente contra HostGator, ejecuta:

```text
docs/migration-context/28-client-password-hardening-hostgator.sql
```

El login cliente moderno usa `UserClient.RoleID=2`. Si el cliente todavia no
tiene fila en `ModernClientCredential`, el primer login correcto con password
legacy crea el hash moderno. El registro moderno crea ambos valores: el hash
legacy en `UserClient.UserPassword` para Web Forms y el hash moderno para
ASP.NET Core.

El cliente puede cambiar su password desde:

```text
http://localhost:5031/Client/ChangePassword
```

El cambio actualiza `UserClient.UserPassword` y `ModernClientCredential`. La app
no muestra el password despues del submit y no registra passwords ni hashes en
logs.

## Administracion de acceso admin

Antes de crear o resetear admins desde la app moderna contra HostGator,
ejecuta:

```text
docs/migration-context/25-admin-access-management-hostgator.sql
```

El login admin sigue usando `UserClient.RoleID=1`, pero la app moderna migra el
password a `ModernAdminCredential` despues del primer login correcto. Desde
`/Admin/AdminUsers`, un admin autenticado puede:

- ver admins existentes;
- abrir `/Admin/CreateAdmin`;
- crear admins nuevos con `RoleID=1`;
- resetear passwords de admins existentes.

No hay endpoint publico de bootstrap ni auto-registro de admin. Si no existe
ningun admin usable, el bootstrap debe hacerse manualmente por SQL una sola vez.
La app no muestra passwords despues del submit y no registra passwords ni hashes
en logs.

## Registro admin de negocios

El admin moderno puede registrar negocios basicos sin tocar Web Forms desde
`/Admin/CreateBusiness`.

Antes de usarlo contra HostGator, confirma que ya se aplicaron:

```text
docs/migration-context/16-business-password-hardening-hostgator.sql
docs/migration-context/22-admin-pilot-management-hostgator.sql
```

Este flujo inserta en la tabla legacy `Business`, respeta los limites actuales
`BusinessName varchar(30)` y `BusinessEmail varchar(30)`, usa
`/img/demo-coffee.svg` como logo default y crea `ModernBusinessCredential` al
mismo tiempo. Si marcas `Habilitar piloto`, tambien crea/actualiza
`ModernPilotBusiness`.

El admin puede marcar `Enviar invitacion por correo` para que el negocio reciba
un link seguro de configuracion de acceso. Ese correo reutiliza el flujo de
reset de contrasena de negocio, con token one-time guardado por hash. La app no
muestra el password inicial despues del submit y no envia passwords planos por
correo.

## Administracion admin de negocios

Desde `/Admin/Businesses`, el boton `Administrar` abre
`/Admin/BusinessProfile/{businessId}`.

El admin puede:

- corregir `BusinessName`;
- corregir `BusinessEmail`;
- ajustar `BusinessLogo` como ruta manual compatible con `varchar(100)`;
- habilitar/deshabilitar piloto y editar notas;
- resetear contrasena del negocio.
- reenviar invitacion de acceso por correo usando el flujo seguro de reset.

Tambien puede ajustar el estado formal de activacion del negocio:

- `LegacyOnly`;
- `PilotModern`;
- `ModernPrimary`;
- `LegacyRetired`.

Antes de usar estos estados contra HostGator, ejecuta:

```text
docs/migration-context/38-admin-business-activation-status-hostgator.sql
```

El reset de contrasena actualiza ambos mecanismos: `Business.BusinessPassword`
con el hash legacy de 25 caracteres y `ModernBusinessCredential` con hash
moderno. Ese reset requiere las mismas tablas de password hardening y pilot
management ya documentadas; el SQL de activacion solo agrega el estado formal de
migracion por negocio.

## Branding de negocio

Antes de editar branding contra HostGator, ejecuta:

```text
docs/migration-context/31-business-branding-v1-hostgator.sql
```

Desde `/Admin/BusinessProfile/{businessId}`, la seccion `Branding Wallet`
permite configurar:

- nombre publico;
- logo publico como ruta o URL;
- color primario;
- color secundario;
- nombre y descripcion del programa.

La app moderna usa ese branding en:

- correos Wallet;
- `/Wallet/Select/{token}`;
- Apple Wallet `.pkpass` y passes actualizados por Apple Web Service;
- Google Wallet;
- dashboard y tarjetas del cliente.

Si no existe branding, la app usa `Business.BusinessName` y `BusinessLogo`. Web
Forms no depende de esta tabla.

El admin tambien puede subir logo publico desde la misma seccion. Los archivos
se guardan fuera del repo y se sirven desde `/uploads/business-logos/...`.

El negocio tambien puede editar branding publico desde `/Business/Branding`.
Ese self-service esta limitado a nombre publico, logo Wallet, colores, nombre
del programa y descripcion. El negocio no puede cambiar email, password, estado
piloto ni activacion desde esa pantalla; esos controles siguen siendo de admin.
Antes de habilitarlo contra HostGator, aplica:

```text
docs/migration-context/48-business-self-service-v1-hostgator.sql
```

Despues de cambiar logo, colores o nombre publico, admin y negocio pueden usar
`Refrescar Wallets recientes` para aplicar el branding a Wallets ya emitidas.
El refresh ejecuta patch Google y update Apple/APNs para tarjetas recientes con
Wallet trackeada, y registra `StampLedger.Source=BrandingRefresh`. No cambia
sellos, tokens ni datos legacy.

## Cutover por negocio

La migracion productiva se hace por negocio usando el estado de activacion en
`/Admin/BusinessProfile/{businessId}`:

- `LegacyOnly`: opera Web Forms.
- `PilotModern`: prueba controlada en moderno.
- `ModernPrimary`: moderno es el flujo diario, Web Forms queda como fallback.
- `LegacyRetired`: estado futuro para negocio sin pantallas legacy.

Web Forms ya lee `ModernPilotBusiness.ActivationStatus` para el flujo de
negocio. Si un negocio esta en `ModernPrimary`, Web Forms muestra aviso para
usar `app.puntelio.com` y queda como fallback. Si esta en `LegacyRetired`, Web
Forms bloquea el login/entrada legacy del negocio y muestra mensaje para usar
la app moderna. Si la tabla moderna no esta disponible, Web Forms conserva el
comportamiento legacy.

Checklist operativo:

```text
ops/pilot-cutover-checklist.md
```

Runbook completo:

```text
docs/migration-context/49-production-pilot-cutover-v1.md
```
Configuracion real recomendada:

```json
{
  "DigitalCards": {
    "Branding": {
      "LogoUploads": {
        "Path": "C:\\Users\\eguillen\\.digitalcards\\uploads\\business-logos",
        "RequestPath": "/uploads/business-logos",
        "MaxBytes": 2097152
      }
    }
  }
}
```

Google Wallet usa ese logo como URL publica HTTPS. Apple Wallet embebe
`logo.png` y `logo@2x.png` cuando el logo subido es PNG; JPG/JPEG/WebP quedan
para UI, correo y Google Wallet hasta agregar conversion de imagen.

## Plantillas de correo

La app moderna renderiza correos desde `IEmailTemplateRenderer`. El correo
Wallet que envia SMTP ya usa esa capa y conserva branding seguro del negocio.

Plantillas disponibles:

- Wallet enrollment;
- bienvenida;
- reset de contrasena;
- alerta interna.

Wallet enrollment y reset de contrasena ya se envian desde flujos activos. Las
plantillas de bienvenida y alerta interna quedan listas para proximos PRs.
Fake email sigue siendo default para CI y Playwright.

## Reset de contrasena por email

Antes de usar reset de contrasena contra HostGator, ejecuta:

```text
docs/migration-context/33-password-reset-flows-v1-hostgator.sql
```

Flujos modernos:

- Cliente: `/Client/ForgotPassword` y `/Client/ResetPassword/{token}`.
- Negocio: `/Business/ForgotPassword` y `/Business/ResetPassword/{token}`.

El reset envia correo por SMTP real o queda visible en `/Dev/Outbox` cuando el
provider de email es fake. El token publico del link se guarda solo como
`SHA-256` en `ModernPasswordResetToken`, vence en una hora y se marca como
usado despues del cambio exitoso. La tabla legacy `PasswordResetToken` de Web
Forms no se modifica.

El cambio actualiza tanto el password legacy (`UserClient.UserPassword` o
`Business.BusinessPassword`) como la credencial moderna
(`ModernClientCredential` o `ModernBusinessCredential`) para mantener
compatibilidad con Web Forms.

## Password hardening negocio

La app moderna migra passwords de negocio gradualmente a una tabla nueva,
sin modificar `Business.BusinessPassword` para no romper Web Forms.

Antes de probar este cambio contra MySQL HostGator, ejecutar el SQL:

```text
docs/migration-context/16-business-password-hardening-hostgator.sql
```

El primer login correcto con password legacy crea el hash moderno en
`ModernBusinessCredential`. Los siguientes logins modernos validan contra esa
credencial.

## Legacy Wallet Sync

Mientras Web Forms siga agregando sellos directo en HostGator, activa el worker
moderno solo en pruebas controladas:

```json
{
  "DigitalCards": {
    "LegacyWalletSync": {
      "Enabled": true,
      "PollIntervalSeconds": 60,
      "LookbackMinutes": 1440,
      "BatchSize": 50
    }
  }
}
```

El worker no cambia `ClientCard`; solo detecta cambios recientes y dispara patch
Google Wallet y push Apple Wallet. Los diagnosticos seguros se activan con:

```json
{
  "DigitalCards": {
    "Diagnostics": {
      "EnableDevOutbox": false,
      "EnableWalletDiagnostics": true
    }
  }
}
```

Endpoint:

```text
/internal/wallet-diagnostics/{CardID-or-enrollment-token}
```

En `Development`, `/Dev/Outbox` sigue disponible para Playwright y fakes. En
un ambiente real, Outbox queda apagado por default; si se necesita soporte
temporal, activa `DigitalCards:Diagnostics:EnableDevOutbox=true` y entra con
cookie admin. Al terminar, vuelve a `false`.

## Smoke real minimo

```powershell
Get-Content "$env:USERPROFILE\.digitalcards\appsettings.Local.json" -Raw | ConvertFrom-Json | Out-Null
cloudflared tunnel --config "$env:USERPROFILE\.cloudflared\config.yml" run puntelio-app
dotnet run --project src\DigitalCards.Web\DigitalCards.Web.csproj --launch-profile http
Invoke-WebRequest https://app.puntelio.com/health -UseBasicParsing
Invoke-WebRequest https://app.puntelio.com/health/ready -UseBasicParsing
```

Despues valida manualmente: login negocio habilitado por admin, asociar cliente
desde el negocio, correo real, Apple Wallet en iPhone, Google Wallet, agregar
sello moderno y update en ambas Wallets.

## Cutover por negocio

Usa `/Admin/Cutover` para revisar readiness antes de cambiar un negocio a
`ModernPrimary` o `LegacyRetired`. La consola muestra estado de activacion,
branding, tarjetas, sellos recientes, Wallets emitidas y errores seguros. Si un
negocio falla el smoke real, cambia su estado a `PilotModern` o `LegacyOnly`
desde la misma pantalla para rollback operativo.

`/Admin/Support` y `/Admin/Cutover` tambien muestran el ultimo estado in-memory
de `LegacyWalletSync`: ultimo run, candidatos, sincronizados, saltados, fallos y
error seguro. Este estado se pierde al reiniciar; para auditoria persistente por
tarjeta usa `StampLedger`.

Antes de guardar evidencia persistente de smoke contra HostGator, aplica
manualmente:

```text
docs/migration-context/73-cutover-smoke-evidence-hostgator.sql
```

Despues de ejecutar el smoke real, `/Admin/Cutover` permite registrar evidencia
por negocio: health, readiness, correo, Apple Wallet, Google Wallet, sello
moderno y revision de soporte. La evidencia queda fechada y ligada al admin que
la capturo; no guarda tokens, JWTs, push tokens, passwords ni connection
strings.

Antes de mover un negocio a `ModernPrimary`, usa el runbook:

```text
docs/migration-context/64-production-business-cutover-v1.md
```

La regla operativa es avanzar un negocio a la vez: smoke real en `PilotModern`,
cambiar a `ModernPrimary` si pasa, mantener Web Forms como fallback y usar
rollback por negocio si aparecen fallos.

## Registro publico por negocio

Antes de usar links publicos de registro, aplica manualmente:

```text
docs/migration-context/61-public-business-enrollment-v1-hostgator.sql
```

Luego entra a `/Admin/BusinessProfile/{businessId}` y genera el link publico.
El cliente abre `/Enroll/{businessToken}`, se registra, queda asociado al
negocio y recibe el correo Wallet. El token plano no se guarda en base de datos;
regenerar el link revoca tokens activos anteriores del mismo negocio.

El negocio tambien puede generar un link y QR desde `/Business/Dashboard`. El QR
se renderiza como SVG server-side y apunta al mismo flujo
`/Enroll/{businessToken}`.

## Reintento manual Wallet desde soporte

`/Admin/Support` permite reintentar updates Wallet para una tarjeta encontrada.
El boton no cambia sellos; usa el estado actual de `ClientCard`, intenta patch de
Google si existe objeto emitido, intenta update Apple si hay pass tracked y
registra el resultado en `StampLedger` con `Source=AdminRetry`.

Los errores se guardan como resumen seguro por tipo de excepcion. No se muestran
JWTs, tokens Apple, push tokens, passwords, hashes, certificados ni connection
strings.

## Seguridad publica

La app usa rate limiting nativo de ASP.NET Core para login, reset password,
registro publico y Wallet endpoints. Los limites se configuran en:

```json
{
  "DigitalCards": {
    "Security": {
      "RateLimiting": {
        "AuthPermitLimit": 20,
        "AuthWindowSeconds": 60,
        "PublicWritePermitLimit": 30,
        "PublicWriteWindowSeconds": 60,
        "WalletPermitLimit": 300,
        "WalletWindowSeconds": 60
      }
    }
  }
}
```

Las superficies admin/negocio/cliente/dev responden con no-cache y la app agrega
headers basicos contra framing, content sniffing y referrer leakage.

## Smoke de cutover por negocio

Smoke fake:

```powershell
$env:RUN_PILOT_BUSINESS_CUTOVER_SMOKE='1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter PilotBusinessCutoverSmoke
Remove-Item Env:\RUN_PILOT_BUSINESS_CUTOVER_SMOKE -ErrorAction SilentlyContinue
```

Smoke real controlado:

```powershell
$env:RUN_PILOT_BUSINESS_CUTOVER_REAL_SMOKE='1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter PilotBusinessCutoverRealSmoke
Remove-Item Env:\RUN_PILOT_BUSINESS_CUTOVER_REAL_SMOKE -ErrorAction SilentlyContinue
```

El real usa MySQL, SMTP, Google Wallet y Apple Wallet segun
`%USERPROFILE%\.digitalcards\appsettings.Local.json`.

## Checklist final de produccion controlada

Antes de operar un negocio real como `ModernPrimary`, usa:

```text
docs/migration-context/74-final-production-readiness-v1.md
```

Ese checklist consolida SQL aplicado, secretos fuera del repo, health/ready,
Data Protection, dominio `app.puntelio.com`, Wallets, SMTP, evidencia de smoke,
rollback por negocio y criterios para marcar `LegacyRetired`.

## Pulido visual del shell moderno

Las paginas autenticadas de admin, negocio y cliente usan un shell inspirado en
Web Forms: sidebar izquierdo, header superior, cards, metricas y footer
`Propiedad de IngeLabs`. En mobile, el sidebar se abre con el boton del header y
se cierra con overlay o tecla `Escape`.

Guia de esta capa visual:

```text
docs/migration-context/75-interactive-ui-polish-v1.md
```

La landing publica de Wallet tambien incluye una vista visual de tarjeta,
progreso de sellos y botones diferenciados para iPhone/Android:

```text
docs/migration-context/76-wallet-landing-visual-v2.md
```

El flujo operativo de `/Business/Cards` incluye resumen visual de resultados,
estado Wallet por tarjeta y una franja de acciones principales para reenviar
Wallet o agregar sello:

```text
docs/migration-context/77-business-cards-visual-v2.md
```

La pantalla `/Client/Cards` muestra resumen visual de tarjetas, sellos,
Wallets y progreso por tarjeta:

```text
docs/migration-context/78-client-cards-visual-v2.md
```

La experiencia cliente final agrega guia QR/Wallet en el dashboard, estados
Wallet mas claros por tarjeta y una explicacion del username fijo usado en QR y
mostrador:

```text
docs/migration-context/86-client-experience-final-v1.md
```

Las pantallas publicas de Wallet ahora muestran guia de instalacion por
plataforma y resaltan Apple Wallet o Google Wallet segun el dispositivo:

```text
docs/migration-context/87-wallet-install-guidance-v1.md
```

La guia visual de marca centraliza tokens CSS para colores, cards, botones,
footer y lockups Puntelio/DigitalCards:

```text
docs/migration-context/88-brand-consistency-v1.md
```

La revision final de paridad Web Forms vs moderno resume que flujos ya estan
listos, cuales siguen en piloto y que falta antes de retirar Web Forms por
negocio:

```text
docs/migration-context/89-webforms-parity-final-check-v1.md
```

La decision inicial de hosting mantiene `app.puntelio.com` sobre un host Windows
controlado con Cloudflare Tunnel, Data Protection persistente, backups externos
y Web Forms como fallback por negocio:

```text
docs/migration-context/90-production-hosting-decision-v1.md
```

El runbook final de cutover por negocio define el procedimiento para pasar de
`PilotModern` a `ModernPrimary`, validar Wallets y ejecutar rollback sin apagar
Web Forms globalmente:

```text
docs/migration-context/91-final-business-cutover-runbook-v1.md
```
