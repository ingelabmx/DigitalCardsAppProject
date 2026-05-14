# Dashboard Empty States v1

## Summary

Los dashboards principales ahora ofrecen acciones claras cuando aun no hay datos suficientes para operar.

## Cambios

- Admin muestra una guia para crear negocio, habilitar piloto o revisar cutover.
- Negocio muestra acciones para asociar el primer cliente y abrir tarjetas cuando no hay actividad.
- Cliente muestra una guia para registrarse con un negocio cuando no tiene tarjetas activas.

## Seguridad

- No cambia permisos, cookies ni consultas.
- No se muestran tokens Wallet, passwords, hashes ni secretos.

## Validacion

- `dotnet test DigitalCardsApp.Modern.sln`
- `RUN_PLAYWRIGHT=1 dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj`
