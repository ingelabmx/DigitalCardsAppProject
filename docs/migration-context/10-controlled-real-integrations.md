# Controlled Real Integrations Runbook

## Alcance

Esta fase deja la app ASP.NET Core moderna lista para pruebas operativas
controladas con integraciones reales, sin reemplazar Web Forms y sin agregar
tablas nuevas.

Defaults seguros:

```json
{
  "DigitalCards": {
    "PersistenceProvider": "InMemory",
    "GoogleWallet": {
      "Provider": "Fake"
    },
    "Email": {
      "Provider": "Fake"
    }
  }
}
```

Playwright y CI deben seguir usando fakes. Las integraciones reales solo se
activan desde configuracion local fuera del repo.

## Configuracion Local

Archivo esperado en cada maquina:

```text
%USERPROFILE%\.digitalcards\appsettings.Local.json
```

Usar `src/DigitalCards.Web/appsettings.Local.example.json` como forma base.
No commitear connection strings, passwords SMTP, service account JSON,
certificados, llaves privadas ni URLs firmadas de Wallet.

Keys operativas:

- `DigitalCards:PersistenceProvider`: `InMemory` o `MySql`.
- `DigitalCards:PublicBaseUrl`: origen publico para links de correo.
- `DigitalCards:GoogleWallet:Provider`: `Fake` o `Google`.
- `DigitalCards:Email:Provider`: `Fake` o `Smtp`.
- `DigitalCards:Smoke:*`: credenciales del negocio test y correo destino para
  smokes manuales.

Para cambiar de maquina, copiar solo la carpeta local segura:

```text
%USERPROFILE%\.digitalcards
```

En la maquina destino, revisar que las rutas internas apunten al nuevo perfil
de usuario, especialmente:

```text
DigitalCards:GoogleWallet:CredentialsFilePath
```

## Validacion de Configuracion

`DigitalCards.Infrastructure` falla temprano con mensajes de configuracion
claros y sin imprimir secretos.

Validaciones actuales:

- `DigitalCards:PersistenceProvider` debe ser `InMemory` o `MySql`.
- `ConnectionStrings:DigitalCards` es obligatorio y parseable cuando se usa
  `MySql`.
- `DigitalCards:GoogleWallet:IssuerId`, `Origins` y `CredentialsFilePath` son
  obligatorios cuando `GoogleWallet:Provider=Google`.
- Cada `DigitalCards:GoogleWallet:Origins` debe ser URL absoluta `http/https`.
- `DigitalCards:Email:Host`, `FromAddress`, `UserName`, `Password`, `Port` y
  `SecureSocket` son obligatorios/validos cuando `Email:Provider=Smtp`.
- `DigitalCards:PublicBaseUrl` debe ser URL absoluta `http/https` cuando SMTP
  real esta activo.

## Smoke 1: MySQL + Google Real + Email Fake

Este smoke escribe filas de prueba en HostGator y emite/actualiza un Google
Wallet real. No envia correo SMTP.

```powershell
$env:RUN_MYSQL_GOOGLE_SMOKE = '1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter MySqlGoogleSmoke
```

Si necesitas usar un negocio test sin editar el JSON local:

```powershell
$env:DigitalCards__Smoke__BusinessEmail = 'business@example.test'
$env:DigitalCards__Smoke__BusinessPassword = 'business-password'
$env:RUN_MYSQL_GOOGLE_SMOKE = '1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter MySqlGoogleSmoke
```

Filas esperadas:

- `UserClient`: cliente nuevo con username prefijo `gwsql`.
- `ClientCard`: relacion cliente-negocio con `CardIDGoogle` poblado.

Las filas no se eliminan automaticamente.

## Smoke 2: SMTP Real Aislado

Este smoke usa persistencia in-memory, Google fake y SMTP real. Sirve para
probar credenciales de correo sin tocar HostGator ni Google Wallet real.

```powershell
$env:RUN_SMTP_SMOKE = '1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter SmtpSmoke
```

El correo se manda a `DigitalCards:Email:SmokeRecipient`. El contenido debe
apuntar a `/Wallet/Select/{token}`; no debe mandar un JWT de Google directo.

## Smoke 3: Full Real

Este smoke usa MySQL HostGator, Google Wallet real y SMTP real. Debe correrse
solo cuando el flujo 1 y 2 ya pasaron.

```powershell
$env:RUN_FULL_REAL_SMOKE = '1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter FullRealSmoke
```

Filas esperadas:

- `UserClient`: cliente nuevo con username prefijo `full`, o reutilizacion del
  cliente asociado a `DigitalCards:Email:SmokeRecipient` si ese correo ya
  existe en el schema legacy.
- `ClientCard`: relacion cliente-negocio con `CardIDGoogle` poblado o
  actualizado.

Tambien envia un correo real a `DigitalCards:Email:SmokeRecipient`.

## Apagar Flags

Los flags `RUN_*` viven solo en la sesion actual de PowerShell. Para apagarlos:

```powershell
Remove-Item Env:\RUN_MYSQL_GOOGLE_SMOKE -ErrorAction SilentlyContinue
Remove-Item Env:\RUN_SMTP_SMOKE -ErrorAction SilentlyContinue
Remove-Item Env:\RUN_FULL_REAL_SMOKE -ErrorAction SilentlyContinue
Remove-Item Env:\DigitalCards__Smoke__BusinessEmail -ErrorAction SilentlyContinue
Remove-Item Env:\DigitalCards__Smoke__BusinessPassword -ErrorAction SilentlyContinue
```

## Pruebas Regulares

Estas pruebas deben pasar siempre sin llamar HostGator, Google Wallet ni SMTP:

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT = '1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```

## Rollback Operativo

Para volver a modo completamente fake:

```json
{
  "DigitalCards": {
    "PersistenceProvider": "InMemory",
    "GoogleWallet": {
      "Provider": "Fake"
    },
    "Email": {
      "Provider": "Fake"
    }
  }
}
```

Tambien puedes quitar temporalmente `%USERPROFILE%\.digitalcards\appsettings.Local.json`
o renombrarlo fuera del flujo de arranque. No borres el archivo si contiene
credenciales que aun no estan respaldadas en un vault seguro.
