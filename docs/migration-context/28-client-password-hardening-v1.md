# 28 Client Password Hardening v1

Esta fase mueve el login moderno de cliente a hashes modernos sin romper Web
Forms. `UserClient.UserPassword` se sigue escribiendo con el hash legacy de 25
caracteres para compatibilidad, pero la app ASP.NET Core usa
`ModernClientCredential` cuando existe.

## Cambios

- Nueva tabla `ModernClientCredential` keyed por `UserClient.UserID`.
- `/Client/Login` acepta solo usuarios `RoleID=2`.
- Si existe credential moderno, valida con `PasswordHasher<T>`.
- Si no existe credential moderno, valida legacy y crea el hash moderno.
- Registro moderno de cliente crea `UserClient.UserPassword` y
  `ModernClientCredential`.
- Nueva pagina protegida `/Client/ChangePassword`.
- Cambio de password actualiza legacy + moderno.

No se introduce ASP.NET Core Identity completo y no se elimina el password
legacy.

## SQL HostGator

Antes de usar este flujo contra MySQL real, ejecutar:

```text
docs/migration-context/28-client-password-hardening-hostgator.sql
```

La tabla nueva no modifica `Business`, `ClientCard`, Wallets ni datos de
negocio. `UserClient` sigue siendo la fuente legacy para clientes.

## Operacion

1. Cliente entra a `/Client/Login`.
2. Si viene de Web Forms/legacy y no tiene credential moderno, el primer login
   correcto crea `ModernClientCredential`.
3. Cliente puede abrir `/Client/ChangePassword`.
4. El password nuevo funciona en la app moderna y se mantiene compatible con
   Web Forms porque tambien actualiza `UserClient.UserPassword`.

La app no muestra passwords despues del submit y no registra passwords ni hashes
en logs.

## Pruebas

- Registro crea hash legacy y credential moderno.
- Login legacy crea credential moderno.
- Login moderno funciona aunque cambie el valor legacy.
- Password invalido no crea credential.
- Cambio de password invalida el password anterior.
- Web y Playwright cubren `/Client/ChangePassword`.
