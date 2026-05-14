# 87 Wallet Install Guidance v1

## Objetivo

Mejorar las pantallas publicas de Wallet para reducir errores de instalacion en iPhone y Android sin cambiar endpoints, tokens ni integraciones reales.

## Cambios

- `/Wallet/Select/{token}` agrega una guia visual para Apple Wallet e Google Wallet.
- La landing detecta iPhone/Android en el navegador y resalta el boton recomendado de forma cliente-side.
- `/Wallet/Apple/{token}` muestra ayuda segura para instalar el `.pkpass` desde Safari.
- `/Wallet/Google/{token}` muestra ayuda segura para guardar la tarjeta con la cuenta Google correcta.
- Los endpoints Wallet siguen publicos y protegidos por token opaco, no por cookies.

## Seguridad

- No se exponen JWTs, `ApplePass`, auth tokens, push tokens ni rutas locales.
- La deteccion de dispositivo no decide permisos; solo mejora la presentacion.
- Apple Wallet Web Service sigue usando `Authorization: ApplePass ...` en sus rutas propias.

## Validacion

```powershell
git diff --check
dotnet test tests\DigitalCards.Web.Tests\DigitalCards.Web.Tests.csproj --filter WalletInstallGuidance_RendersPlatformHintsAndTroubleshooting
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
