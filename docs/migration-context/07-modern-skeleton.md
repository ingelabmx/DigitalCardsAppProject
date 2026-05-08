# Modern ASP.NET Core Skeleton

## Objetivo

Se agrego una solucion paralela para iniciar la migracion sin reemplazar la
aplicacion Web Forms existente. El nuevo esqueleto usa ASP.NET Core Razor Pages
en `net8.0`, con dependencias falsas para correo y Wallets.

## Solucion

- `DigitalCardsApp.Modern.sln`
- `src/DigitalCards.Domain`: entidades y reglas base de cliente, negocio,
  tarjeta de lealtad, sellos y plataforma Wallet.
- `src/DigitalCards.Application`: casos de uso, contratos y modelos de salida.
- `src/DigitalCards.Infrastructure`: repositorios in-memory, outbox fake de
  correo, clock y Google Wallet fake.
- `src/DigitalCards.Web`: Razor Pages para el flujo vertical minimo.
- `tests/DigitalCards.Domain.Tests`: pruebas de reglas de dominio.
- `tests/DigitalCards.Application.Tests`: pruebas de casos de uso con
  integraciones fake.
- `tests/DigitalCards.Web.Tests`: smoke tests HTTP.
- `tests/DigitalCards.E2E.Tests`: pruebas Playwright listas para ejecutarse
  contra el Web nuevo.

## Flujo Cubierto

- Registro de cliente.
- Login de negocio demo con datos no productivos.
- Asociacion cliente-negocio-tarjeta.
- Captura fake de correo en `/Dev/Outbox`.
- Landing de seleccion Wallet.
- Google Wallet con stub.
- Agregado de sello.
- Visualizacion de tarjeta actualizada.
- Responsive basico para landing Wallet en viewports tipo iPhone y Android.

## Integraciones Fake

El proyecto nuevo no conecta a produccion. `DigitalCards.Infrastructure` registra
repositorios in-memory, `FakeWalletEmailOutbox` y `FakeGoogleWalletService`.
Esto permite probar el flujo completo sin SMTP real, base de datos real ni
credenciales reales de Wallet.

## Como Ejecutar

```powershell
dotnet build DigitalCardsApp.Modern.sln
dotnet test DigitalCardsApp.Modern.sln
dotnet run --no-launch-profile --project src/DigitalCards.Web/DigitalCards.Web.csproj --urls http://localhost:5088
```

Las pruebas Playwright se saltan por defecto para que CI no falle si el browser
runtime no esta instalado. Para activarlas:

```powershell
dotnet build DigitalCardsApp.Modern.sln
powershell -ExecutionPolicy Bypass -File tests/DigitalCards.E2E.Tests/bin/Debug/net8.0/playwright.ps1 install chromium
$env:RUN_PLAYWRIGHT = '1'
dotnet test tests/DigitalCards.E2E.Tests/DigitalCards.E2E.Tests.csproj
```

## Notas De Seguridad

El legado debe mantenerse sin credenciales productivas en repo. Para este pase:

- SMTP y connection string se movieron a placeholders/env vars.
- Google Wallet requiere `GOOGLE_APPLICATION_CREDENTIALS` apuntando a un archivo
  fuera de source control.
- El JSON de service account bajo `GW-K` se retiro del proyecto y se agrego
  `.gitignore` local.
- Aun se deben rotar credenciales reales fuera del repo y revisar historial si
  este repositorio se compartio.
