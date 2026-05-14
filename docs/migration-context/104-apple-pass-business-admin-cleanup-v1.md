# 104 Apple Pass + Business/Admin Cleanup v1

Esta fase ajusta el pase Apple real y limpia superficies operativas de negocio y
admin sin cambiar base de datos. `ModernBusinessBranding.ProgramDescription`
queda tratado en UI como **Recompensa**.

## Cambios

- Apple Pass muestra `Programa` y `Recompensa`, elimina campos visibles
  `Negocio`, `Historico` y `Alta`, y quita `altText` del QR/barcode.
- Apple Pass sigue embebiendo `logo.png` y `logo@2x.png` cuando el negocio tiene
  logo PNG subido; si no existe, usa los assets configurados.
- `/Business/Branding` refresca todas las tarjetas digitales emitidas del
  negocio, sin input de limite visible.
- `WalletBrandingRefreshService` usa listado completo por negocio cuando el
  limite es `0`.
- `/Business/Stamp` limpia el input renderizado despues de agregar sello.
- Admin ya no muestra campos de notas para negocios y mantiene estados simples
  `Activo` / `Inactivo`.
- `/Admin/Clients` conserva borrado permanente y aclara que sirve para limpieza
  segura de clientes de prueba.

## Sin SQL Nuevo

No hay cambios de esquema ni pasos manuales en HostGator.

## Validacion

- `git diff --check`
- `dotnet test DigitalCardsApp.Modern.sln`
- `RUN_PLAYWRIGHT=1 dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj`
