# 22 Admin Pilot Management v1

## Objetivo

Esta fase mueve la habilitacion de negocios piloto desde `appsettings.Local.json` hacia una tabla operable desde la app moderna. El admin entra con un usuario legacy `UserClient.RoleID=1` y habilita o deshabilita negocios existentes.

No registra negocios nuevos y no toca Web Forms.

## Cambios

- Nuevo login admin en `/Admin/Login` con cookie separada `DigitalCards.Admin`.
- Nuevas paginas `/Admin/Dashboard` y `/Admin/Businesses`.
- Nueva tabla `ModernPilotBusiness`.
- Nuevo `AdminAppService`.
- Nuevos repositorios `IAdminUserRepository` e `IPilotBusinessRepository`.
- `PilotAccessService` permite un negocio cuando `Pilot.Enabled=false`, cuando el negocio esta habilitado en `ModernPilotBusiness`, o cuando sigue en el allowlist temporal de config.

## Auth Admin

El admin moderno usa la tabla legacy `UserClient`:

- `RoleID=1`;
- `UserName` o `UserEmail` para login;
- `UserPassword` validado con `LegacyPasswordVerifier`.

No se introduce ASP.NET Core Identity en este PR y no se migra password de admin todavia.

## HostGator

Antes de smoke real, ejecutar:

```text
docs/migration-context/22-admin-pilot-management-hostgator.sql
```

La tabla nueva no modifica `Business`, `UserClient`, `ClientCard` ni datos Wallet.

## Operacion

Con `DigitalCards:Pilot:Enabled=true`:

1. Entrar a `/Admin/Login`.
2. Abrir `/Admin/Businesses`.
3. Buscar negocio por nombre o correo.
4. Usar `Habilitar piloto` o `Deshabilitar piloto`.
5. El negocio habilitado puede usar `/Business/Dashboard`, `/Business/Cards`, `/Business/Enroll` y `/Business/Stamp`.

El allowlist de negocio en config queda como fallback temporal. Para operar desde admin, remover `AllowedBusinessEmails` o `AllowedBusinessIds` de `appsettings.Local.json` cuando ya exista la fila en `ModernPilotBusiness`.

El allowlist de clientes sigue en config en este PR.

## Seguridad

- Cookie admin separada de cookie negocio.
- Paginas admin requieren policy `AdminOnly`.
- No se muestran passwords, tokens Wallet, JWTs, push tokens, certificados ni connection strings.
- Los logs registran IDs operativos, no secretos.

## Smoke Real

1. Aplicar SQL en HostGator.
2. Levantar `app.puntelio.com`.
3. Confirmar `/health/ready`.
4. Entrar a `/Admin/Login` con admin legacy.
5. Habilitar negocio real en `/Admin/Businesses`.
6. Entrar como negocio.
7. Buscar tarjeta en `/Business/Cards`.
8. Reenviar link Wallet.
9. Agregar sello y confirmar updates Apple/Google.
10. Deshabilitar negocio y confirmar bloqueo del flujo moderno.

## Pruebas

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
