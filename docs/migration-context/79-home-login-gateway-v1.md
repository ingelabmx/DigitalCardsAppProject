# Home Login Gateway v1

## Summary

La raiz publica `/` deja de presentarse como shell de migracion y pasa a ser la entrada principal de Puntelio DigitalCards.

## Cambios

- La home muestra accesos claros para cliente, negocio y admin.
- Si existe una cookie valida de cliente, negocio o admin, la home muestra una accion para continuar al dashboard correspondiente.
- Los links publicos de registro, Wallet y enrolamiento por negocio permanecen sin cambios.
- El link de `/Dev/Outbox` sigue oculto salvo cuando la configuracion de diagnostico lo permite.

## Seguridad

- No cambia ninguna regla de autenticacion.
- No expone tokens Wallet, secrets, connection strings ni informacion sensible.
- La deteccion de sesiones usa los tres schemes existentes: `DigitalCards.Client`, `DigitalCards.Business` y `DigitalCards.Admin`.

## Validacion

- `dotnet test DigitalCardsApp.Modern.sln`
- `RUN_PLAYWRIGHT=1 dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj`
