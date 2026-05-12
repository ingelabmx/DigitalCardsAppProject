# 24 Admin Business Management v1

## Objetivo

Esta fase cierra el ciclo basico de administracion moderna de negocios. Despues
de crear negocios desde `/Admin/CreateBusiness`, el admin puede corregir datos,
ajustar piloto y resetear la contrasena sin usar Web Forms, phpMyAdmin ni SQL
manual.

No reemplaza Web Forms y no crea tablas nuevas.

## Cambios

- Nueva pagina `/Admin/BusinessProfile/{businessId}`.
- Link `Administrar` desde `/Admin/Businesses`.
- Edicion de datos legacy:
  - `BusinessName`;
  - `BusinessEmail`;
  - `BusinessLogo` como ruta manual.
- Edicion de piloto desde el perfil:
  - habilitado/deshabilitado;
  - notas.
- Reset de contrasena de negocio:
  - actualiza `Business.BusinessPassword` con SHA-256 lowercase truncado a 25
    caracteres;
  - actualiza `ModernBusinessCredential` con `PasswordHasher<T>`.

## HostGator

No hay SQL nuevo en este PR. Usa tablas existentes:

```text
Business
ModernBusinessCredential
ModernPilotBusiness
```

Se respetan los limites legacy:

- `BusinessName varchar(30)`;
- `BusinessEmail varchar(30)`;
- `BusinessLogo varchar(100)`.

## Operacion

1. Entrar a `/Admin/Login`.
2. Abrir `/Admin/Businesses`.
3. Buscar negocio por nombre o correo.
4. Abrir `Administrar`.
5. Editar datos o estado piloto.
6. Si hace falta, resetear contrasena.
7. Validar login del negocio con la nueva contrasena.

El upload real de logo queda fuera. En esta version el admin captura una ruta
legacy o URL corta compatible con `BusinessLogo`.

## Seguridad

- Las paginas requieren cookie `DigitalCards.Admin` y policy `AdminOnly`.
- La contrasena nueva nunca se muestra despues del submit.
- No se registran passwords, JWTs, tokens Wallet, push tokens, certificados ni
  connection strings.
- Duplicados de nombre/correo devuelven mensajes seguros.

## Smoke Real

1. Confirmar `/health/ready`.
2. Entrar como admin legacy.
3. Crear o buscar un negocio test.
4. Cambiar nombre/correo/logo.
5. Resetear contrasena.
6. Entrar como negocio con la nueva contrasena.
7. Confirmar `/Business/Cards`.
8. Enviar link Wallet y agregar sello moderno.

## Pruebas

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```

## Fuera De Alcance

- Eliminar negocios.
- Upload de logos.
- Email automatico al negocio.
- Cambio de password iniciado por el negocio.
- Migracion completa de Web Forms.
