# 15 Business Auth Hardening

## Objetivo

Este PR cierra el hueco principal del flujo moderno de negocio: las paginas
`/Business/Dashboard`, `/Business/Enroll` y `/Business/Stamp` ya no confian en
`businessId` enviado por query string o hidden inputs. El negocio autenticado
se resuelve desde una cookie de ASP.NET Core y claims emitidos despues del
login legacy.

## Alcance

- Se agrega autenticacion cookie en `DigitalCards.Web`.
- El esquema es `DigitalCards.Business`.
- La policy `BusinessOnly` requiere usuario autenticado, `Role=Business` y
  claim `BusinessId`.
- El login sigue validando con `DigitalCardsAppService.LoginBusinessAsync`.
- No se introduce ASP.NET Core Identity todavia.
- No se cambian tablas en HostGator.
- Web Forms no se toca.

## Claims Emitidos

- `BusinessId`: identificador legacy del negocio.
- `BusinessEmail`: correo del negocio.
- `BusinessName`: nombre visible del negocio.
- `Role=Business`: rol usado por la policy `BusinessOnly`.

## Cookie

- Nombre: `.DigitalCards.Business`.
- `HttpOnly`: activo.
- `SameSite`: `Lax`.
- `SecurePolicy`: `SameAsRequest`, para permitir desarrollo local y tuneles
  HTTPS sin exigir una configuracion distinta.
- Expiracion: 8 horas con sliding expiration.

## Paginas Protegidas

- `/Business/Dashboard`
- `/Business/Enroll`
- `/Business/Stamp`

Estas paginas obtienen el `BusinessId` desde `BusinessAuth.GetBusinessId(User)`.
Aunque un usuario mande `businessId` manipulado en la URL, los comandos
`EnrollClientCommand` y `AddStampCommand` se construyen con el negocio
autenticado.

## Rutas Publicas Que Se Mantienen Publicas

- `/Register`
- `/Wallet/Select/{token}`
- `/Wallet/Apple/...`
- `/Wallet/Google/...`
- `/apple-wallet/v1/...`

Apple Wallet Web Service sigue protegido por su propio header
`Authorization: ApplePass ...`, no por cookie de negocio.

## Logout

La ruta `/Business/Logout` limpia la cookie del esquema
`DigitalCards.Business` y redirige a `/Business/Login`.

## Pruebas Agregadas

- Dashboard sin cookie redirige a login.
- Login valido emite cookie y redirige al dashboard.
- Login invalido no emite cookie.
- Dashboard, Enroll y Stamp responden OK con cookie valida.
- Enroll y Stamp ignoran `businessId` manipulado y usan claims.
- Logout expira la cookie.
- Playwright verifica que los links modernos ya no lleven `businessId`.

## Nota De Configuracion Local En Tests

Los tests Web/E2E fuerzan fakes y pueden omitir
`%USERPROFILE%\.digitalcards\appsettings.Local.json` con:

```powershell
$env:DigitalCards__SkipUserLocalConfiguration='true'
```

Esto evita que CI o pruebas locales dependan de secretos reales o de un JSON
local temporalmente invalido. La ejecucion real de la app sigue leyendo ese
archivo salvo que se active el override anterior.

## Riesgos Restantes

- El password legacy todavia se valida con `LegacyPasswordVerifier`.
- La migracion a hashing moderno queda para un PR posterior.
- La cookie protege la app moderna, pero Web Forms sigue operando aparte.
- Si Web Forms agrega sellos directo, la sincronizacion de Wallets depende del
  worker legacy ya documentado.
