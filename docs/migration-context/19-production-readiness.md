# 19 Production Readiness

## Objetivo

Dejar `https://app.puntelio.com` operable con piloto controlado: health checks,
readiness de dependencias, Cloudflare forwarded headers, Data Protection
persistente y logs seguros.

## Configuracion

La configuracion real sigue fuera del repo:

```text
C:\Users\eguillen\.digitalcards\appsettings.Local.json
```

Bloque operativo recomendado:

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

Notas:

- `EnableForwardedHeaders=true` permite respetar `X-Forwarded-Proto` y
  `X-Forwarded-Host` enviados por Cloudflare/reverse proxy.
- `TrustAllForwardedHeaders=false` es el default recomendado. Solo usar `true`
  si Kestrel no esta expuesto directamente y el unico caller es un tunnel/proxy
  local controlado.
- `DataProtectionKeysPath` debe apuntar a una carpeta fuera del repo para que
  cookies de negocio sigan siendo validas despues de reiniciar la app.

## Health Checks

- `GET /health`: liveness, no depende de MySQL ni integraciones externas.
- `GET /health/ready`: readiness, valida configuracion critica y ejecuta
  `SELECT 1` cuando `DigitalCards:PersistenceProvider=MySql`.

La respuesta JSON es segura: no incluye passwords, connection strings,
certificados, service account JSON, JWTs, auth tokens ni push tokens.

## Logs Seguros

Flujos con logs operativos:

- login negocio correcto/fallido;
- enroll moderno completado/bloqueado/fallido;
- stamp moderno completado/bloqueado/fallido;
- SMTP enviado con correo enmascarado;
- Google Wallet issue/patch;
- Apple Wallet register/update/pass request/APNs;
- LegacyWalletSync run summary.

No loggear secretos ni tokens. Los correos completos tampoco deben aparecer en
logs nuevos; usar valores enmascarados.

## Runbook De Encendido

Validar JSON local:

```powershell
Get-Content "$env:USERPROFILE\.digitalcards\appsettings.Local.json" -Raw | ConvertFrom-Json | Out-Null
```

Levantar tunnel y app:

```powershell
cloudflared tunnel --config "$env:USERPROFILE\.cloudflared\config.yml" run puntelio-app
dotnet run --project src\DigitalCards.Web\DigitalCards.Web.csproj --launch-profile http
```

Validar:

```powershell
Invoke-WebRequest https://app.puntelio.com/health -UseBasicParsing
Invoke-WebRequest https://app.puntelio.com/health/ready -UseBasicParsing
```

Smoke real:

1. Login negocio habilitado por admin.
2. Enroll/asociacion de cliente desde negocio habilitado.
3. Confirmar correo real con link `https://app.puntelio.com/Wallet/Select/...`.
4. Instalar Apple Wallet en iPhone.
5. Guardar Google Wallet.
6. Agregar sello desde app moderna.
7. Confirmar update Apple/Google.
8. Si se prueba Web Forms, activar `LegacyWalletSync`, agregar sello legacy y
   confirmar patch Google + push Apple.

## Rollback

Para volver a operar solo con Web Forms:

```json
{
  "DigitalCards": {
    "Pilot": {
      "Enabled": false
    },
    "Diagnostics": {
      "EnableWalletDiagnostics": false
    },
    "LegacyWalletSync": {
      "Enabled": false
    }
  }
}
```

Reiniciar la app moderna. Las Wallets ya emitidas quedan instaladas; este
rollback solo apaga pantallas modernas/polling/diagnostico.

## Validacion Automatizada

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'; dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
