# 46 Wallet Public UI Polish v1

## Objetivo

Pulir la experiencia publica de Wallet para que `/Wallet/Select/{token}`,
Apple Wallet y Google Wallet se sientan parte de Puntelio/DigitalCards y usen
branding de negocio cuando exista.

## Cambios

- `WalletLandingDto` expone logo, color primario y color secundario del negocio.
- `GetWalletLandingAsync` aplica `ModernBusinessBranding` antes de renderizar la
  landing publica.
- `/Wallet/Select/{token}` muestra logo, nombre publico, metricas, sellos
  visuales y botones Apple/Google responsive.
- `/Wallet/Apple/{token}` y `/Wallet/Google/{token}` usan el mismo tratamiento
  visual cuando pueden resolver el token.
- Google y Apple siguen publicos por token; no se agrega cookie ni policy.
- No hay SQL nuevo.

## Seguridad

- No se muestran tokens internos, JWTs, push tokens, passwords, hashes,
  certificados, connection strings ni rutas locales.
- La landing sigue dependiendo de token opaco.
- Apple Wallet Web Service permanece separado bajo `/apple-wallet/v1` con
  `Authorization: ApplePass ...`.

## Validacion

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
