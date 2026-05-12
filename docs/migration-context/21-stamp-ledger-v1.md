# 21 Stamp Ledger v1

## Objetivo

Esta fase agrega auditoria operativa de sellos sin tocar Web Forms. A partir de este PR, los sellos agregados desde la app moderna y los cambios detectados por `LegacyWalletSync` dejan una huella de soporte en `StampLedger`.

No hay backfill historico. La auditoria empieza cuando se despliega la tabla y este codigo.

## Cambios

- Nueva tabla MySQL `StampLedger`.
- Nuevo contrato `IStampLedgerRepository`.
- Implementaciones InMemory y MySQL.
- `DigitalCardsAppService.AddStampAsync` y `AddStampToCardAsync` registran eventos `ModernBusiness`.
- `LegacyWalletSyncProcessor` registra eventos `LegacySync` solo cuando procesa un cambio no saltado por fingerprint.
- `/Business/Cards` muestra los ultimos eventos de auditoria de la tarjeta.

## Datos Registrados

Cada evento guarda:

- `CardID`, `BusinessID`, `UserID`.
- `Source`: `ModernBusiness` o `LegacySync`.
- `ActorBusinessID` cuando el sello viene de negocio moderno.
- Sellos visibles e historicos antes/despues.
- `ObservedLastCheck`.
- Si Google Wallet y Apple Wallet fueron intentados.
- Si los updates Wallet terminaron correctamente.
- `ErrorSummary` seguro cuando falla una integracion.

`ErrorSummary` guarda solo el tipo de excepcion. No guarda passwords, JWTs, auth tokens, push tokens, service account JSON, `.p12`, connection strings ni rutas de certificados.

## LegacySync

Cuando Web Forms modifica `ClientCard`, el worker moderno detecta cambios por `LastCheck`, `CheckQTY` y `HistoricCheckQTY`. En v1 el worker solo ve el snapshot actual, por eso para eventos `LegacySync` los valores previous/new quedan iguales. Esto indica que el cambio fue observado y procesado, no reconstruye el delta historico.

El worker no registra nada cuando salta un fingerprint repetido.

## HostGator

Antes de smoke real, ejecutar:

```text
docs/migration-context/21-stamp-ledger-v1-hostgator.sql
```

La tabla nueva no modifica tablas legacy. No borrar filas automaticamente; sirven para soporte operacional.

## UI

En `/Business/Cards`, el detalle de tarjeta muestra:

- fecha del evento;
- origen;
- sellos antes/despues;
- estado Google Wallet;
- estado Apple Wallet;
- error seguro si existio.

El negocio autenticado solo ve auditoria de tarjetas que pertenecen a su `BusinessID`.

## Smoke Real

1. Aplicar SQL en HostGator.
2. Levantar `app.puntelio.com`.
3. Login negocio allowlisted.
4. Buscar tarjeta en `/Business/Cards`.
5. Agregar sello moderno.
6. Confirmar update Apple/Google.
7. Confirmar evento `ModernBusiness` en el detalle.
8. Agregar sello desde Web Forms con `LegacyWalletSync` activo.
9. Confirmar evento `LegacySync`.

## Pruebas

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```

Cobertura nueva:

- sello moderno crea ledger;
- errores Wallet registran resumen seguro;
- `LegacyWalletSync` registra cambios procesados;
- fingerprints repetidos no generan ledger;
- `/Business/Cards` muestra eventos sin secretos.
