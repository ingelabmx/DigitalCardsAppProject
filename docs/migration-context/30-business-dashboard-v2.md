# 30 Business Dashboard v2

Esta fase convierte `/Business/Dashboard` en una pantalla operativa mas util
sin crear tablas nuevas ni apagar Web Forms. El dashboard sigue protegido por
cookie `DigitalCards.Business` y por el piloto de negocio.

## Cambios

- `/Business/Dashboard` muestra resumen de actividad reciente:
  - tarjetas recientes;
  - sellos actuales e historicos;
  - Google Wallet emitidas;
  - Apple Wallet tracked;
  - alertas recientes de Wallet.
- Muestra accesos rapidos a `/Business/Cards`, `/Business/Enroll` y logout.
- Muestra tarjetas recientes con enlace validado a `/Business/Cards`.
- Muestra eventos recientes de `StampLedger` con estado Google/Apple.
- No muestra `businessId`, tokens Wallet, JWTs, push tokens, passwords ni
  connection strings.

## Operacion

1. Admin habilita negocio piloto.
2. Negocio entra a `/Business/Login`.
3. `/Business/Dashboard` muestra resumen seguro.
4. Negocio abre `Tarjetas y sellos` para operar tarjetas.
5. Los sellos agregados desde moderno aparecen en el resumen por `StampLedger`.

Los datos son una vista reciente de operacion, no un reporte contable final.

## Pruebas

- Application: dashboard devuelve tarjetas recientes, estado Wallet y ledger.
- Web: dashboard muestra resumen y no filtra datos sensibles.
- Playwright: flujo negocio/cliente agrega sello y valida resumen en dashboard.

No requiere SQL nuevo.
