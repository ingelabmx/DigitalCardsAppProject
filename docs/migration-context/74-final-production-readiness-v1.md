# 74 Final Production Readiness v1

## Objetivo

Consolidar el checklist final para operar `app.puntelio.com` con negocios reales
de forma controlada, manteniendo Web Forms como fallback hasta retirar cada
negocio por separado.

Este PR no agrega features nuevas ni SQL. La meta es que el corte a
`ModernPrimary` sea repetible, auditable y reversible.

## Checklist Global

Antes de activar negocios reales:

1. `app.puntelio.com` responde por Cloudflare Tunnel o hosting final.
2. `/health` responde 200.
3. `/health/ready` responde 200.
4. `C:\Users\eguillen\.digitalcards\appsettings.Local.json` valida como JSON.
5. Data Protection keys estan fuera del repo y persisten reinicios.
6. MySQL HostGator esta configurado con `SslMode=Preferred`.
7. SMTP real envia correo sin exponer passwords.
8. Google Wallet tiene `https://app.puntelio.com` como origin.
9. Apple Wallet usa `webServiceURL=https://app.puntelio.com/apple-wallet`.
10. Certificados Apple, service account JSON, `.p12` y secrets estan fuera del
    repo.
11. `/Dev/Outbox` esta apagado en ambiente real salvo soporte admin temporal.
12. `AllowLegacyCardIdTokens=false` salvo emergencia controlada.

## SQL Aplicado

Confirmar que las migraciones manuales requeridas estan aplicadas en HostGator:

- `13-apple-wallet-updates-hostgator.sql`
- `16-business-password-hardening-hostgator.sql`
- `20-wallet-link-token-hardening-hostgator.sql`
- `21-stamp-ledger-v1-hostgator.sql`
- `22-admin-pilot-management-hostgator.sql`
- `25-admin-access-management-hostgator.sql`
- `28-client-password-hardening-hostgator.sql`
- `31-business-branding-v1-hostgator.sql`
- `33-password-reset-flows-v1-hostgator.sql`
- `38-admin-business-activation-status-hostgator.sql`
- `48-business-self-service-v1-hostgator.sql`
- `61-public-business-enrollment-v1-hostgator.sql`
- `66-operational-audit-log-hostgator.sql`
- `69-public-enrollment-consent-hostgator.sql`
- `73-cutover-smoke-evidence-hostgator.sql`

## Checklist Por Negocio

Para cada negocio:

1. Admin habilita el negocio desde `/Admin/Businesses` o `/Admin/Cutover`.
2. Branding esta completo: nombre publico, logo, colores, programa.
3. Negocio entra a `/Business/Login`.
4. Negocio confirma `/Business/Dashboard`, `/Business/Cards`,
   `/Business/Reports` y `/Business/Branding`.
5. Cliente controlado se registra/asocia desde flujo moderno.
6. Correo real contiene links de `https://app.puntelio.com`.
7. Cliente instala Apple Wallet en iPhone.
8. Cliente guarda Google Wallet.
9. Negocio agrega sello desde moderno.
10. Apple y Google muestran sellos actualizados.
11. `/Admin/Support` muestra estado seguro de la tarjeta.
12. `StampLedger` registra el sello.
13. `/Admin/Cutover` guarda evidencia de smoke completa.
14. Admin cambia estado a `ModernPrimary`.

## Rollback Por Negocio

Si algo falla:

1. Cambiar negocio a `PilotModern` o `LegacyOnly` desde `/Admin/Cutover`.
2. Confirmar que Web Forms sigue disponible para el negocio.
3. No borrar tokens, passes, tarjetas ni registros Wallet.
4. Revisar `/Admin/Support` y `StampLedger`.
5. Si el fallo es por `LegacyWalletSync`, apagarlo temporalmente en
   `appsettings.Local.json`.
6. Reintentar Wallet desde soporte solo cuando el estado de tarjeta sea claro.

## Retiro De Web Forms

Un negocio solo pasa a `LegacyRetired` cuando:

- lleva una ventana operativa estable en `ModernPrimary`;
- sellos, correo, Apple, Google y soporte ya pasaron smoke;
- existe evidencia registrada en `/Admin/Cutover`;
- Web Forms guard bloquea ese negocio o existe instruccion manual equivalente;
- rollback fue probado y documentado.

## Validacion Antes De Release

```powershell
git diff --check
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```

Si se modifica Web Forms:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' DigitalCardsApp.sln /restore /p:Configuration=Debug /p:Platform='Any CPU'
```
