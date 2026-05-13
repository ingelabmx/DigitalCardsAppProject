# Puntelio Windows Operations

Scripts de ayuda para operar `app.puntelio.com` localmente sin copiar secrets al
repo.

## Scripts

- `start-puntelio-app.ps1`: valida configuracion local y levanta ASP.NET Core en
  `http://localhost:5031`. Usa `-Background` para dejar PID y logs.
- `start-puntelio-tunnel.ps1`: levanta Cloudflare Tunnel `puntelio-app`. Usa
  `-Background` para dejar PID y logs.
- `start-puntelio-stack.ps1`: levanta app y tunnel en background.
- `stop-puntelio-stack.ps1`: detiene app y tunnel usando los PID files.
- `restart-puntelio-stack.ps1`: reinicio controlado de app y tunnel.
- `get-puntelio-status.ps1`: muestra procesos y health checks.
- `check-puntelio-health.ps1`: valida `/health` y `/health/ready`.
- `show-puntelio-logs.ps1`: muestra las ultimas lineas de logs locales.

Los scripts no instalan servicios, no modifican DNS y no imprimen secrets.
Los logs se guardan en `%USERPROFILE%\.digitalcards\logs` y los PID files en
`%USERPROFILE%\.digitalcards\run`.

## Uso Basico

En terminal 1:

```powershell
.\ops\windows\start-puntelio-app.ps1
```

En terminal 2:

```powershell
.\ops\windows\start-puntelio-tunnel.ps1
```

En terminal 3:

```powershell
.\ops\windows\check-puntelio-health.ps1
```

## Uso Background

Para una sesion operativa local:

```powershell
.\ops\windows\start-puntelio-stack.ps1
.\ops\windows\get-puntelio-status.ps1
.\ops\windows\show-puntelio-logs.ps1
```

Reinicio:

```powershell
.\ops\windows\restart-puntelio-stack.ps1
```

Paro:

```powershell
.\ops\windows\stop-puntelio-stack.ps1
```

## Checklist Diario

1. `get-puntelio-status.ps1` debe mostrar app y tunnel corriendo.
2. `/health` y `/health/ready` deben responder 200.
3. Revisar logs sin exponer passwords, tokens, JWTs, push tokens ni connection
   strings.
4. Hacer smoke corto: login admin, login negocio, abrir `/Business/Cards`.

