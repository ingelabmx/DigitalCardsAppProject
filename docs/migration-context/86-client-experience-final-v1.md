# 86 Client Experience Final v1

## Objetivo

Cerrar la experiencia visual del cliente antes de seguir con pulidos publicos de Wallet. Este PR no cambia autenticacion, tokens, base de datos ni reglas de negocio; solo mejora la manera en que el cliente entiende su QR, sus tarjetas y el estado Wallet.

## Cambios

- `/Client/Dashboard` agrega una guia visual de tres pasos: mostrar QR, revisar sellos y abrir Wallet.
- `/Client/Cards` muestra badges claros para Apple Wallet y Google Wallet por tarjeta.
- `/Client/Cards` mejora el estado vacio con acciones directas a QR y perfil.
- `/Client/Profile` explica que el username queda fijo porque se usa para QR, busqueda de mostrador y compatibilidad Wallet.
- La UI mantiene el shell visual inspirado en Web Forms y sigue usando estilos responsive.

## Seguridad

- No se muestran passwords, hashes, tokens opacos, JWTs ni detalles internos.
- Los links Wallet siguen usando tokens opacos ya existentes.
- El cliente sigue viendo unicamente sus propias tarjetas.

## Validacion

```powershell
git diff --check
dotnet test tests\DigitalCards.Web.Tests\DigitalCards.Web.Tests.csproj --filter ClientPages_RenderFinalExperienceUx
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
