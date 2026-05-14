# Auth Pages Visual v2

## Summary

Los logins de admin, negocio y cliente usan una presentacion visual compartida para que el acceso inicial se sienta como producto final y no como formulario tecnico.

## Cambios

- `/Admin/Login`, `/Business/Login` y `/Client/Login` usan el mismo shell visual de autenticacion.
- Cada rol conserva su formulario, cookie y flujo existente.
- Se elimina texto de demo de la pantalla de negocio.
- Cada login incluye un enlace claro para volver a `/`.

## Seguridad

- No cambia la validacion de credenciales.
- No se agregan endpoints publicos nuevos.
- No se muestran passwords, hashes, tokens Wallet ni datos sensibles.

## Validacion

- `dotnet test DigitalCardsApp.Modern.sln`
- `RUN_PLAYWRIGHT=1 dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj`
