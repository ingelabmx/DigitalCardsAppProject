# Business Operations UX v2

## Summary

El flujo diario del negocio queda mas claro para operacion en mostrador: buscar/escaneo, validar tarjeta y agregar sello.

## Cambios

- `/Business/Dashboard` agrega una franja de 3 pasos para el flujo diario.
- `/Business/Cards` mejora estados vacios y guia hacia asociar cliente cuando no hay resultados.
- `/Business/CheckIn` muestra pasos visibles de mostrador antes del scanner QR.
- Se agregan estilos compartidos para herramientas operativas del negocio.

## Seguridad

- No cambia `BusinessId` autenticado ni permisos.
- No cambia Wallet, StampLedger ni persistencia.
- No requiere SQL nuevo.

## Validacion

- `dotnet test DigitalCardsApp.Modern.sln`
- `RUN_PLAYWRIGHT=1 dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj`
