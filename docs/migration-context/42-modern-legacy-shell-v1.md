# 42 Modern Legacy Shell v1

## Objetivo

Agregar un shell autenticado en ASP.NET Core que se parezca al layout Web Forms:
sidebar izquierdo, header superior, cards sobre fondo claro y footer
`Propiedad de IngeLabs`.

## Comportamiento

- Las paginas autenticadas de admin, negocio y cliente usan shell legacy.
- Las paginas publicas siguen con layout publico:
  - home;
  - registro;
  - login;
  - forgot/reset password;
  - Wallet landing Apple/Google;
  - outbox de desarrollo.
- La navegacion se decide por claims:
  - admin: dashboard, crear negocio, negocios, clientes, admins, soporte;
  - negocio: dashboard, tarjetas, asociar cliente, checadas;
  - cliente: dashboard, mis tarjetas, contrasena.
- No cambia autorizacion, cookies, rutas publicas ni proveedores Wallet.

## Paridad Visual

El shell replica los elementos base detectados en Web Forms:

- sidebar de `270px`;
- header de `70px`;
- secciones por rol;
- cards blancas sobre fondo `#f6f9fc`;
- azul primario `#5d87ff`;
- footer operativo;
- mobile con sidebar en flujo vertical.

No copia el bundle completo legacy. La implementacion usa CSS moderno propio
sobre Bootstrap existente.

## Seguridad

- No muestra ids internos en navegacion.
- No agrega endpoints.
- No cambia Apple Wallet Web Service ni Wallet public pages.
- No muestra tokens, JWTs, push tokens, passwords ni connection strings.

## Validacion

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
