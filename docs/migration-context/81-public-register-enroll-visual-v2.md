# Public Register and Enroll Visual v2

## Summary

Las pantallas publicas de registro general y enrolamiento por negocio usan una experiencia mas guiada y visual, manteniendo las mismas reglas de seguridad y persistencia.

## Cambios

- `/Register` muestra una estructura de pasos para orientar al cliente.
- `/Enroll/{businessToken}` muestra nombre, logo, programa, descripcion y colores del negocio cuando existe branding.
- Los formularios conservan los mismos campos, consentimiento obligatorio y test IDs existentes.
- El resultado de enrolamiento mantiene el link Wallet por token opaco.

## Seguridad

- No cambia la resolucion de `businessToken`.
- No se agrega cookie al flujo publico.
- No se muestran tokens internos, passwords, hashes ni secretos.

## Validacion

- `dotnet test DigitalCardsApp.Modern.sln`
- `RUN_PLAYWRIGHT=1 dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj`
