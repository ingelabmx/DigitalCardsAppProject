# 91 Final Business Cutover Runbook v1

## Objetivo

Convertir el paso de un negocio a operacion moderna en un procedimiento repetible. El cutover sigue siendo por negocio; Web Forms no se apaga globalmente.

## Estados

| Estado | Uso |
| --- | --- |
| `LegacyOnly` | Negocio sigue operando en Web Forms. |
| `PilotModern` | Negocio prueba moderno con usuarios controlados. |
| `ModernPrimary` | Negocio opera principalmente en moderno, Web Forms queda como fallback. |
| `LegacyRetired` | Web Forms debe quedar bloqueado para ese negocio. |
| `Inactive` | Login moderno bloqueado. |

## Pre-Checklist

Antes de mover un negocio a `ModernPrimary`:

1. `/health` responde 200.
2. `/health/ready` responde healthy.
3. Admin puede entrar a `/Admin/Login`.
4. Negocio puede entrar a `/Business/Login`.
5. Negocio esta habilitado en `/Admin/Businesses`.
6. Branding/logo revisado.
7. `DigitalCards:PublicBaseUrl` apunta a `https://app.puntelio.com`.
8. SMTP real probado.
9. Google Wallet origins incluyen `https://app.puntelio.com`.
10. Apple Wallet instala pass con `webServiceURL` de `https://app.puntelio.com/apple-wallet`.

## Smoke Real Por Negocio

1. Admin abre `/Admin/Cutover`.
2. Busca el negocio.
3. Confirma estado inicial `PilotModern`.
4. Negocio abre `/Business/Dashboard`.
5. Negocio abre `/Business/Cards`.
6. Negocio registra o asocia cliente controlado.
7. Sistema envia correo real.
8. Cliente abre el link de `app.puntelio.com`.
9. Cliente instala Apple Wallet en iPhone.
10. Cliente guarda Google Wallet.
11. Negocio agrega sello desde `/Business/Cards` o `/Business/CheckIn`.
12. Confirmar:
    - Apple Wallet actualiza.
    - Google Wallet actualiza.
    - `StampLedger` registra evento.
    - `/Admin/Support` muestra estado correcto.
    - `/Admin/Cutover` muestra evidencia de smoke.

## Cambio A `ModernPrimary`

Despues del smoke:

1. En `/Admin/Cutover`, cambiar estado a `ModernPrimary`.
2. Confirmar que el negocio sigue entrando a moderno.
3. Confirmar que Web Forms muestra advertencia o redirect suave si aplica.
4. Documentar en notas internas:
   - fecha;
   - admin;
   - negocio;
   - cliente de prueba;
   - resultado Apple;
   - resultado Google;
   - resultado sello;
   - fallback probado.

## Cambio A `LegacyRetired`

Solo usar cuando:

1. El negocio opero estable en `ModernPrimary`.
2. Soporte puede diagnosticar tarjetas sin SQL manual.
3. El negocio entiende que Web Forms queda bloqueado.
4. Existe rollback documentado.

Accion:

1. Cambiar estado a `LegacyRetired`.
2. Validar que Web Forms bloquee o advierta para ese negocio.
3. Validar que moderno siga operando.

## Rollback Por Negocio

Si falla el flujo moderno:

1. Cambiar estado de `ModernPrimary` a `PilotModern` o `LegacyOnly`.
2. Informar al negocio que opere temporalmente en Web Forms.
3. Mantener activos los endpoints Wallet ya instalados.
4. Revisar:
   - `/Admin/Support`;
   - logs de SMTP;
   - Google patch;
   - Apple APNs;
   - `LegacyWalletSync`;
   - `StampLedger`.
5. Repetir smoke antes de volver a `ModernPrimary`.

## No Hacer Durante Cutover

- No cambiar `PublicBaseUrl`.
- No rotar certificados Apple durante una prueba activa.
- No borrar passes, tokens, credentials o filas legacy.
- No apagar Web Forms globalmente.
- No exponer tokens, JWTs, passwords, hashes, push tokens ni connection strings en notas.

## Criterio De Exito

Un negocio esta listo para operar moderno cuando:

- El negocio puede asociar clientes.
- Los clientes reciben correo real.
- Apple y Google se instalan/guardan desde `app.puntelio.com`.
- Los sellos modernos actualizan ambas Wallets.
- Soporte puede diagnosticar la tarjeta.
- Existe rollback claro por negocio.

## Validacion

Este PR es documental y no requiere SQL.

```powershell
git diff --check
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
