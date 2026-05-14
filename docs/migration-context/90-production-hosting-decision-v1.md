# 90 Production Hosting Decision v1

## Decision

La decision operativa inicial es mantener `app.puntelio.com` como dominio canonico y correr la app moderna en un host Windows controlado, con Cloudflare Tunnel como entrada publica HTTPS. Esta opcion conserva lo que ya funciona para Apple Wallet, Google Wallet, SMTP, MySQL HostGator y Web Forms fallback, sin mover todavia a una plataforma nueva.

## Opcion Elegida Para Produccion Inicial

- ASP.NET Core ejecutado como proceso/servicio Windows.
- Cloudflare Tunnel nombrado para `app.puntelio.com`.
- Configuracion externa unica:
  - `%USERPROFILE%\.digitalcards\appsettings.Local.json`
  - Data Protection keys fuera del repo.
  - Certificados, `.p12`, service account JSON, SMTP y connection strings fuera del repo.
- HostGator MySQL sigue como source of truth durante el cutover.
- Web Forms sigue vivo como fallback.

## Por Que Esta Opcion

- Apple Wallet queda ligado al `webServiceURL`; cambiar dominio rompe la continuidad de passes instalados.
- El flujo real ya fue probado con `app.puntelio.com`.
- Cloudflare simplifica TLS publico sin exponer directamente la maquina.
- Permite operar cutover por negocio antes de pagar la complejidad de una migracion de hosting.

## Requisitos Del Host

- Windows con .NET 8 Hosting Bundle o runtime compatible.
- Acceso estable a MySQL HostGator.
- `cloudflared` instalado y autenticado.
- Carpeta externa segura:
  - `%USERPROFILE%\.digitalcards\appsettings.Local.json`
  - `%USERPROFILE%\.digitalcards\secrets`
  - `%USERPROFILE%\.digitalcards\uploads`
  - `%USERPROFILE%\.digitalcards\data-protection-keys`
  - `%USERPROFILE%\.digitalcards\logs`
- Backups de config/secrets fuera de GitHub.

## Operacion Diaria

1. Verificar proceso app y tunnel.
2. Revisar:
   - `https://app.puntelio.com/health`
   - `https://app.puntelio.com/health/ready`
3. Confirmar logs sin errores recurrentes de:
   - SMTP
   - Google Wallet
   - Apple APNs
   - LegacyWalletSync
4. Usar `/Admin/Support` y `/Admin/Cutover` para diagnostico por negocio/tarjeta.

## Backups

- Base de datos HostGator: backup desde cPanel/phpMyAdmin o job externo.
- Uploads de logos: backup de `%USERPROFILE%\.digitalcards\uploads`.
- Data Protection keys: backup de `%USERPROFILE%\.digitalcards\data-protection-keys`.
- Config/secrets: backup cifrado, nunca en repo.

## Rollback

Rollback rapido por negocio:

1. Cambiar negocio a `PilotModern` o `LegacyOnly` desde `/Admin/Cutover`.
2. Apagar `LegacyWalletSync` si esta causando ruido.
3. Operar temporalmente desde Web Forms.
4. Mantener Wallet endpoints vivos si hay passes instalados.

Rollback de hosting:

1. Detener app moderna.
2. Mantener Cloudflare DNS/tunnel sin redirigir a otro dominio.
3. Revertir ultimo release o volver a binario anterior.
4. Validar `/health` y smoke minimo antes de reactivar negocio.

## Alternativas Futuras

### IIS En Windows Server

Buena opcion cuando se quiera un host Windows mas formal con proceso administrado por IIS, logs del sistema, reinicio automatico y separacion de usuario de servicio.

### Azure App Service O VPS

Buena opcion cuando se quiera escalar operacion, monitoreo externo, secretos administrados y despliegue CI/CD. Requiere revisar Apple certs, uploads persistentes, Data Protection y acceso MySQL.

## No Decidido Todavia

- Proveedor final de hosting administrado.
- Monitoreo externo con alertas.
- Estrategia CI/CD final.
- Separacion staging/produccion. Por ahora se opera en `app.puntelio.com` con guardrails por negocio.

## Validacion

Este PR es documental y no requiere SQL.

```powershell
git diff --check
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
