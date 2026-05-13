# Puntelio Windows Operations

Scripts de ayuda para operar `app.puntelio.com` localmente sin copiar secrets al
repo.

## Scripts

- `start-puntelio-app.ps1`: valida configuracion local y levanta ASP.NET Core en
  `http://localhost:5031`.
- `start-puntelio-tunnel.ps1`: levanta Cloudflare Tunnel `puntelio-app`.
- `check-puntelio-health.ps1`: valida `/health` y `/health/ready`.

Los scripts no instalan servicios, no modifican DNS y no imprimen secrets.

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

