# 23 Admin Business Registration v1

## Objetivo

Esta fase permite que el admin moderno registre negocios desde
`app.puntelio.com` usando la tabla legacy `Business`, sin tocar Web Forms y sin
crear tablas nuevas.

El alcance es intencionalmente basico: nombre, correo, password inicial,
habilitacion opcional de piloto y notas.

## Cambios

- Nueva pagina admin `/Admin/CreateBusiness`.
- Requiere cookie `DigitalCards.Admin` y policy `AdminOnly`.
- Inserta el negocio en la tabla legacy `Business`.
- Usa `/img/demo-coffee.svg` como logo default hasta tener carga de logo.
- Guarda `Business.BusinessPassword` como SHA-256 lowercase truncado a 25
  caracteres, compatible con el patron legacy existente.
- Crea inmediatamente `ModernBusinessCredential` con `PasswordHasher<T>` para
  que el login moderno use hash fuerte desde el primer acceso.
- Si el admin marca `Habilitar piloto`, crea/actualiza `ModernPilotBusiness`.

## HostGator

No hay SQL nuevo en este PR. Antes de usarlo contra MySQL real deben existir:

```text
docs/migration-context/16-business-password-hardening-hostgator.sql
docs/migration-context/22-admin-pilot-management-hostgator.sql
```

La tabla legacy `Business` sigue siendo la fuente de verdad para negocios. Las
limitaciones de esquema se respetan:

- `BusinessName varchar(30)`;
- `BusinessEmail varchar(30)`;
- `BusinessLogo varchar(100)`.

## Operacion

1. Entrar a `/Admin/Login`.
2. Abrir `/Admin/CreateBusiness`.
3. Capturar nombre, correo y password inicial.
4. Marcar `Habilitar piloto` si el negocio debe usar el flujo moderno.
5. Confirmar que el negocio aparece en `/Admin/Businesses`.
6. Probar login del negocio en `/Business/Login`.

No se envia correo automatico al negocio en esta version. El admin comunica la
credencial inicial manualmente.

## Seguridad

- El password inicial nunca se muestra despues del submit.
- El password inicial no se guarda en texto plano.
- Los logs registran `AdminUserId`, `BusinessId` y estado piloto; no registran
  passwords, tokens Wallet, JWTs, certificados ni connection strings.
- Duplicados de nombre/correo devuelven mensajes seguros.

## Smoke Real

1. Confirmar `/health/ready`.
2. Entrar como admin legacy.
3. Crear un negocio test con correo controlado.
4. Habilitar piloto.
5. Entrar como negocio.
6. Registrar/asociar un cliente controlado.
7. Reenviar link Wallet.
8. Confirmar Apple/Google y sello moderno.

## Pruebas

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```

## Fuera De Alcance

- Alta de logo.
- Edicion o eliminacion de negocios.
- Email automatico de bienvenida.
- Cambio moderno de password.
- Reemplazo de Web Forms.
