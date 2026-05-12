# 17 Puntelio Single Environment

## Objetivo

Usar `app.puntelio.com` como URL canonica unica para pruebas reales y
produccion inicial, sin carpetas `staging`/`production` ni perfiles de
configuracion. La unica configuracion externa real vive en:

```text
C:\Users\eguillen\.digitalcards\appsettings.Local.json
```

Apple Wallet guarda `webServiceURL` dentro del `.pkpass`; las tarjetas
instaladas desde `https://app.puntelio.com` seguiran apuntando a ese dominio.

## Dominio canonico

- `app.puntelio.com`: app moderna, correos, Google Wallet y Apple Wallet.
- `puntelio.com` y `www.puntelio.com`: libres para landing publica o redirect.
- `admin.puntelio.com`: opcional futuro si se separan pantallas admin.

No usar `staging.puntelio.com` en este flujo. Las pruebas reales se hacen contra
`app.puntelio.com` con clientes y negocios controlados.

## Cloudflare Tunnel

Crear un tunnel nombrado y DNS estable:

```powershell
cloudflared tunnel login
cloudflared tunnel create puntelio-app
cloudflared tunnel route dns puntelio-app app.puntelio.com
```

Config local sugerida:

```yaml
tunnel: puntelio-app
credentials-file: C:\Users\eguillen\.cloudflared\<TUNNEL_ID>.json

ingress:
  - hostname: app.puntelio.com
    service: http://localhost:5031
  - service: http_status:404
```

Ejecucion:

```powershell
cloudflared tunnel run puntelio-app
dotnet run --project src\DigitalCards.Web\DigitalCards.Web.csproj --launch-profile http
```

## Configuracion externa unica

La app lee configuracion externa en este orden relevante:

1. Configuracion normal de ASP.NET Core (`appsettings.json`, ambiente, etc.).
2. `C:\Users\eguillen\.digitalcards\appsettings.Local.json`, salvo que
   `DigitalCards__SkipUserLocalConfiguration=true`.
3. Variables de entorno.
4. Argumentos de comando.

Ejemplo seguro:

```powershell
New-Item -ItemType Directory "$env:USERPROFILE\.digitalcards" -Force
Copy-Item docs\config\appsettings.local.example.json "$env:USERPROFILE\.digitalcards\appsettings.Local.json"
notepad "$env:USERPROFILE\.digitalcards\appsettings.Local.json"
```

Valores clave:

```json
{
  "DigitalCards": {
    "PublicBaseUrl": "https://app.puntelio.com",
    "PersistenceProvider": "MySql",
    "GoogleWallet": {
      "Provider": "Google",
      "Origins": ["https://app.puntelio.com"]
    },
    "AppleWallet": {
      "Provider": "Apple"
    },
    "Email": {
      "Provider": "Smtp"
    },
    "LegacyWalletSync": {
      "Enabled": true
    }
  }
}
```

## Wallets

- Agregar `https://app.puntelio.com` como origin permitido en Google Wallet.
- `DigitalCards:PublicBaseUrl` debe ser `https://app.puntelio.com`.
- Apple Wallet usara `https://app.puntelio.com/apple-wallet` como
  `webServiceURL`.
- Si se cambia el dominio, las tarjetas Apple ya instaladas deben borrarse e
  instalarse de nuevo.

## Proteccion operativa

- Usar clientes, correos y negocios controlados para pruebas reales.
- Activar `DigitalCards:Pilot:Enabled=true` antes de usar datos reales para
  que solo negocios/clientes allowlisted usen pantallas modernas.
- No imprimir ni commitear `.p12`, JSON de service account, passwords SMTP,
  connection strings ni tokens.
- Activar `LegacyWalletSync` solo cuando quieras que cambios hechos desde Web
  Forms disparen updates de Wallet.
- Antes de usuarios reales, rotar credenciales usadas durante pruebas.

## Validacion

```powershell
Get-Content "$env:USERPROFILE\.digitalcards\appsettings.Local.json" -Raw | ConvertFrom-Json | Out-Null
Invoke-WebRequest https://app.puntelio.com/health -UseBasicParsing
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'; dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```

En prueba iPhone, despues de agregar sello, buscar logs seguros:

```text
Apple Wallet update push accepted
Apple Wallet update check ... returned 1 updated passes
Apple Wallet updated pass request returned Ready
```
