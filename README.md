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

## Login negocio moderno

El flujo ASP.NET Core moderno usa cookie auth para negocio:

- Login: `http://localhost:5031/Business/Login`
- Dashboard protegido: `http://localhost:5031/Business/Dashboard`
- Tarjetas y sellos: `http://localhost:5031/Business/Cards`
- Logout: `http://localhost:5031/Business/Logout`

Las paginas `/Business/Dashboard`, `/Business/Enroll`, `/Business/Cards` y
`/Business/Stamp` requieren cookie valida. Ya no se debe pasar `businessId` por
URL ni por hidden input; el negocio se toma desde los claims de la sesion.

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
negocios y clientes allowlisted usen las pantallas modernas:

```json
{
  "DigitalCards": {
    "Pilot": {
      "Enabled": true,
      "AllowedBusinessIds": [],
      "AllowedBusinessEmails": ["NEGOCIO_TEST_EMAIL"],
      "AllowedClientEmails": ["CLIENTE_TEST_EMAIL"],
      "AllowedClientEmailDomains": ["example.test"]
    }
  }
}
```

Con el piloto activo:

- negocios fuera del allowlist pueden iniciar sesion, pero ven bloqueo en
  `/Business/Dashboard`, `/Business/Enroll`, `/Business/Cards` y
  `/Business/Stamp`;
- Wallet landing, Apple Wallet Web Service y descargas `.pkpass` siguen
  publicas por token/autorizacion propia;
- si no hay allowlist de clientes, los enrolamientos/sellos modernos quedan
  bloqueados para clientes existentes.

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
- Negocios piloto: `http://localhost:5031/Admin/Businesses`

Con `DigitalCards:Pilot:Enabled=true`, un negocio puede usar el flujo moderno
si esta habilitado en `ModernPilotBusiness` o si sigue en el allowlist temporal
de `appsettings.Local.json`. La recomendacion operativa es mover los negocios
a `/Admin/Businesses` y dejar `AllowedBusinessEmails`/`AllowedBusinessIds` solo
como fallback.

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

Despues valida manualmente: login negocio allowlisted, enroll cliente
allowlisted, correo real, Apple Wallet en iPhone, Google Wallet, agregar sello
moderno y update en ambas Wallets.
