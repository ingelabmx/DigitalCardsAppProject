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

Estos scripts no instalan servicios ni imprimen secretos. El runbook para
convertirlos en operacion estable, con logs, reinicio, Data Protection y
rollback, esta en:

```text
docs/migration-context/36-production-service-hosting-v1.md
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

Configuracion de transicion:

```json
{
  "DigitalCards": {
    "WalletLinks": {
      "AllowLegacyCardIdTokens": true
    }
  }
}
```

Con compatibilidad activa, links viejos por `CardID` siguen funcionando. Los
links nuevos usan tokens opacos guardados solo como hash. En un PR futuro se
puede cambiar `AllowLegacyCardIdTokens` a `false`.

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
- Crear negocio: `http://localhost:5031/Admin/CreateBusiness`
- Administrar negocio: `http://localhost:5031/Admin/BusinessProfile/{businessId}`

Con `DigitalCards:Pilot:Enabled=true`, un negocio puede usar el flujo moderno
si esta habilitado en `ModernPilotBusiness` o si sigue en el allowlist temporal
de `appsettings.Local.json`. La recomendacion operativa es mover los negocios
a `/Admin/Businesses` y dejar `AllowedBusinessEmails`/`AllowedBusinessIds` solo
como fallback. Ya no hay allowlist operativo de clientes: el admin habilita el
negocio y el negocio habilitado asocia al cliente desde `/Business/Enroll` u
opera la tarjeta desde `/Business/Cards`.

## Soporte admin

`/Admin/Support` permite buscar por cliente, negocio o tarjeta para revisar:

- sellos actuales e historicos;
- estado Google Wallet y Apple Wallet;
- dispositivos Apple registrados;
- ultimos eventos de `StampLedger`;
- estado de configuracion de `LegacyWalletSync`.

Tambien permite descargar un JSON de diagnostico desde `Exportar diagnostico
JSON` para evidencia interna de soporte. Es una vista solo lectura. No muestra
tokens Wallet, enrollment tokens, push
tokens, JWTs, passwords, hashes, certificados ni connection strings.

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

No hay email automatico al negocio en esta version. El admin define y comunica
el password inicial manualmente; la app no lo muestra despues del submit.

## Administracion admin de negocios

Desde `/Admin/Businesses`, el boton `Administrar` abre
`/Admin/BusinessProfile/{businessId}`.

El admin puede:

- corregir `BusinessName`;
- corregir `BusinessEmail`;
- ajustar `BusinessLogo` como ruta manual compatible con `varchar(100)`;
- habilitar/deshabilitar piloto y editar notas;
- resetear contrasena del negocio.

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

## Cutover por negocio

La migracion productiva se hace por negocio usando el estado de activacion en
`/Admin/BusinessProfile/{businessId}`:

- `LegacyOnly`: opera Web Forms.
- `PilotModern`: prueba controlada en moderno.
- `ModernPrimary`: moderno es el flujo diario, Web Forms queda como fallback.
- `LegacyRetired`: estado futuro para negocio sin pantallas legacy.

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
      "EnableWalletDiagnostics": true
    }
  }
}
```

Endpoint:

```text
/internal/wallet-diagnostics/{CardID-or-enrollment-token}
```

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
