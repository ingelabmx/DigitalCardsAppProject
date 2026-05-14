# Form Validation UX v1

## Summary

Los formularios POST de Razor Pages reciben comportamiento compartido de envio y error para reducir doble-submit y hacer los errores mas visibles.

## Cambios

- Los formularios POST validos marcan `aria-busy=true`, deshabilitan botones submit y muestran `Procesando...`.
- Si la validacion cliente falla, el foco se mueve al primer campo/resumen con error.
- Los summaries de validacion usan un estilo visible y consistente.

## Seguridad

- No cambia reglas de validacion ni handlers.
- No se agregan endpoints ni persistencia.
- No se muestran secretos ni datos sensibles.

## Validacion

- `dotnet test DigitalCardsApp.Modern.sln`
- `RUN_PLAYWRIGHT=1 dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj`
