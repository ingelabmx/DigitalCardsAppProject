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
