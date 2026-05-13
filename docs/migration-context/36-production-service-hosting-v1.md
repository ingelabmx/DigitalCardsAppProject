# 36 Production Service Hosting v1

## Objetivo

Dejar una guia operativa repetible para correr `app.puntelio.com` de forma
estable, sin depender de memoria manual: Cloudflare Tunnel, app ASP.NET Core,
Data Protection persistente, logs, health checks, reinicio y rollback.

Este PR no cambia tablas ni secretos. La configuracion real sigue en:

```text
C:\Users\eguillen\.digitalcards\appsettings.Local.json
```

## Layout Recomendado

```text
C:\Users\eguillen\.digitalcards\
  appsettings.Local.json
  data-protection-keys\
  logs\

C:\Users\eguillen\.cloudflared\
  config.yml
  <TUNNEL_ID>.json
```

`appsettings.Local.json` debe tener:

```json
{
  "DigitalCards": {
    "PublicBaseUrl": "https://app.puntelio.com",
    "Operations": {
      "EnableForwardedHeaders": true,
      "DataProtectionKeysPath": "C:\\Users\\eguillen\\.digitalcards\\data-protection-keys",
      "RequireDataProtectionKeysForReadiness": true
    }
  }
}
```

## Cloudflare Tunnel

Config esperada:

```yaml
tunnel: puntelio-app
credentials-file: C:\Users\eguillen\.cloudflared\<TUNNEL_ID>.json

ingress:
  - hostname: app.puntelio.com
    service: http://localhost:5031
  - service: http_status:404
```

Arranque manual:

```powershell
.\ops\windows\start-puntelio-tunnel.ps1
```

## App ASP.NET Core

Arranque manual:

```powershell
.\ops\windows\start-puntelio-app.ps1
```

El script valida que `appsettings.Local.json` sea JSON valido y crea la carpeta
de Data Protection si no existe. No imprime secrets.

## Health Checks

Validacion rapida:

```powershell
.\ops\windows\check-puntelio-health.ps1
```

Endpoints:

- `GET https://app.puntelio.com/health`: proceso vivo.
- `GET https://app.puntelio.com/health/ready`: configuracion critica y MySQL
  cuando el provider real esta activo.

`/health/ready` debe estar verde antes de hacer smoke real con clientes.

## Logs

Durante el piloto, usar salida de consola como primer destino operativo. Para
servicio estable, redirigir stdout/stderr a:

```text
C:\Users\eguillen\.digitalcards\logs\
```

Buscar eventos de:

- login admin, negocio y cliente;
- enroll/resend Wallet link;
- SMTP enviado;
- Google Wallet issue/patch;
- Apple pass download, device registration, APNs;
- `LegacyWalletSync`;
- errores de `StampLedger` o repositorios MySQL.

No deben aparecer passwords, connection strings, tokens Wallet, JWTs, push
tokens, certificados, service account JSON ni hashes.

## Ejecucion Como Servicio

Para produccion local estable en Windows, usar dos servicios separados:

- `puntelio-app`: ejecuta la app ASP.NET Core publicada.
- `puntelio-cloudflared`: ejecuta `cloudflared tunnel run puntelio-app`.

Orden recomendado:

1. publicar app:

   ```powershell
   dotnet publish src\DigitalCards.Web\DigitalCards.Web.csproj -c Release -o C:\Puntelio\DigitalCards.Web
   ```

2. configurar el servicio de la app para ejecutar:

   ```text
   dotnet C:\Puntelio\DigitalCards.Web\DigitalCards.Web.dll --urls http://localhost:5031
   ```

3. configurar el servicio del tunnel para ejecutar:

   ```text
   cloudflared tunnel --config C:\Users\eguillen\.cloudflared\config.yml run puntelio-app
   ```

4. reiniciar ambos servicios y validar health.

No guardar secrets en argumentos de servicio. Todos los secrets deben quedar en
`appsettings.Local.json` o archivos externos ya ignorados por git.

## Runbook De Encendido

```powershell
Get-Content "$env:USERPROFILE\.digitalcards\appsettings.Local.json" -Raw | ConvertFrom-Json | Out-Null
.\ops\windows\start-puntelio-app.ps1
.\ops\windows\start-puntelio-tunnel.ps1
.\ops\windows\check-puntelio-health.ps1
```

En uso real, app y tunnel se levantan en terminales separadas o como servicios.

## Smoke Real

1. `https://app.puntelio.com/health` responde.
2. `https://app.puntelio.com/health/ready` responde.
3. Login admin.
4. Login negocio habilitado.
5. Buscar/asociar cliente desde negocio.
6. Enviar o reenviar link Wallet.
7. Instalar Apple Wallet en iPhone.
8. Guardar Google Wallet.
9. Agregar sello desde `/Business/Cards`.
10. Confirmar update Apple/Google y evento en `StampLedger`.

## Rollback

Rollback operativo sin tocar Web Forms:

```json
{
  "DigitalCards": {
    "Pilot": {
      "Enabled": false
    },
    "LegacyWalletSync": {
      "Enabled": false
    },
    "Diagnostics": {
      "EnableWalletDiagnostics": false
    }
  }
}
```

Luego reiniciar la app. Las Wallets existentes siguen instaladas y apuntando a
`https://app.puntelio.com`; este rollback solo apaga uso moderno y sync.

## Criterio De Cierre

- Scripts operativos disponibles.
- README apunta al runbook.
- `/health` y `/health/ready` documentados.
- No hay SQL nuevo.
- Pruebas automatizadas y Playwright verdes.
