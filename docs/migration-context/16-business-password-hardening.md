# 16 Business Password Hardening

## Objetivo

Este PR agrega migracion progresiva de passwords de negocio para la app
ASP.NET Core moderna, sin tocar la columna legacy `Business.BusinessPassword`
que Web Forms todavia usa.

## Diseno

- Se agrega `ModernBusinessCredential`, keyed por `BusinessID`.
- `Business.BusinessPassword` queda intacto para compatibilidad con Web Forms.
- El login moderno busca primero una credencial moderna.
- Si existe, valida con `PasswordHasher<T>` de ASP.NET Core Identity.
- Si no existe, valida con `LegacyPasswordVerifier`; si el password legacy es
  correcto, crea la credencial moderna.
- Si el password es invalido, no crea ni actualiza credenciales.

## SQL HostGator

Antes de activar este cambio contra MySQL real, ejecutar:

```sql
source docs/migration-context/16-business-password-hardening-hostgator.sql
```

O copiar el contenido del archivo en cPanel/phpMyAdmin. El script solo crea la
tabla nueva si no existe; no modifica `Business`, `UserClient`, `ClientCard` ni
tablas de Wallet.

## Compatibilidad

- Web Forms sigue usando `Business.BusinessPassword`.
- La cookie auth moderna no cambia.
- Los flujos Wallet, SMTP, Google, Apple y Playwright siguen usando los mismos
  providers y fakes.
- Si Web Forms cambia el password legacy despues de que exista una credencial
  moderna, la app moderna seguira validando contra la credencial moderna. Un PR
  futuro debe definir un flujo moderno de cambio de password y/o sincronizacion.

## Pruebas

- Login con password legacy crea hash moderno.
- Segundo login usa hash moderno aunque el valor legacy cambie.
- Login invalido no crea hash moderno.
- DI registra repositorio MySQL/InMemory de credenciales.
- Regression completa y Playwright siguen verdes.

## Riesgos Restantes

- Este PR protege solo negocios en la app moderna.
- Clientes/admin legacy quedan fuera del alcance.
- La tabla nueva debe existir antes de ejecutar el flujo real con MySQL.
