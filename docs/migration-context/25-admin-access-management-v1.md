# 25 Admin Access Management v1

Esta fase mueve la operacion diaria de acceso admin a la app moderna sin abrir
auto-registro publico. Solo un admin autenticado con cookie `DigitalCards.Admin`
puede listar admins, crear otro admin o resetear contrasenas.

## Cambios

- Nueva tabla `ModernAdminCredential` para hashes modernos por `UserClient.UserID`.
- `/Admin/Login` sigue aceptando solo usuarios legacy `UserClient.RoleID=1`.
- Si un admin entra con password legacy y no existe credential moderno, la app
  crea `ModernAdminCredential`.
- Nuevas paginas protegidas:
  - `/Admin/AdminUsers`;
  - `/Admin/CreateAdmin`.
- Crear admin inserta en `UserClient` con `RoleID=1`, guarda password legacy de
  25 caracteres y crea credential moderno.
- Reset admin actualiza `UserClient.UserPassword` y `ModernAdminCredential`.

No existe endpoint `/setup`, `/bootstrap` ni registro publico de admin. Si no
existe ningun admin usable, el bootstrap debe hacerse una sola vez por SQL.

## SQL HostGator

Antes de usar este flujo contra MySQL real, ejecutar:

```text
docs/migration-context/25-admin-access-management-hostgator.sql
```

La tabla nueva no modifica `Business`, `ClientCard`, Wallets ni datos de
negocio. `UserClient` sigue siendo la fuente legacy para admins.

## Operacion

1. Entrar a `/Admin/Login` con un admin legacy existente.
2. Abrir `/Admin/AdminUsers`.
3. Crear admins desde `/Admin/CreateAdmin`.
4. Resetear passwords desde la lista de administradores.
5. Probar logout/login con el admin nuevo o password nuevo.

Los passwords iniciales o reseteados se comunican fuera de la app. La app no los
muestra despues del submit y no los registra en logs.

## Seguridad

- Todas las paginas nuevas requieren policy `AdminOnly`.
- Los logs solo registran IDs de admin y target admin.
- No se registran passwords, hashes, connection strings, tokens ni secretos.
- Duplicados de `UserName` o `UserEmail` devuelven error seguro.
- No se implementa delete/desactivacion de admins en v1.
