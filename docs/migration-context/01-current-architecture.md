# Current Architecture

## Tipo de proyecto actual

El proyecto actual es una aplicacion ASP.NET Web Forms clasica:

- Solucion: `DigitalCardsApp.sln`.
- Proyecto web: `DigitalCardsApp/DigitalCardsApp.csproj`.
- Framework: `.NET Framework 4.8`, indicado por `TargetFrameworkVersion` `v4.8`.
- Modelo de aplicacion: Web Forms con paginas `.aspx`, code-behind `.aspx.cs` y archivos `.designer.cs`.
- Hosting local configurado para IIS Express con URL HTTPS local en el archivo de proyecto.
- No es ASP.NET Core, no usa `Program.cs`, `Startup.cs`, MVC moderno ni Razor Pages.

Tambien existe un repositorio Git anidado dentro de `DigitalCardsApp`. El repo raiz lo marca como cambio de tipo subrepo/gitlink, pero no hay `.gitmodules` valido. No se modifico esa estructura.

## Estructura de carpetas importante

- `DigitalCardsApp/`: aplicacion Web Forms.
- `DigitalCardsApp/Connectors/`: acceso a datos con clases estaticas.
- `DigitalCardsApp/Models/`: DTOs/modelos simples para cliente, negocio, admin y tarjeta.
- `DigitalCardsApp/GW-Methods/`: implementacion actual de Google Wallet.
- `DigitalCardsApp/GW-K/`: contiene un JSON de service account. Es material sensible y debe salir del repo.
- `DigitalCardsApp/Resources/GoogleButton/`: asset del boton de Google Wallet.
- `DigitalCardsApp/Logos/`: logos cargados para negocios.
- `DigitalCardsApp/assets/`: template visual, CSS, JS e imagenes.
- `DigitalCardsApp/DataTables/`: librerias frontend vendorizadas.
- `DigitalCardsApp/Tools/`: helpers como `TextFormats.cs` y una plantilla HTML de correo.
- `docs/db_dcards.sql`: script SQL encontrado en el repo raiz.

La ruta solicitada `C:\Users\eguillen\source\repos\DigitalCardsApp\docs\db_dcards.sql` no existe en este equipo. El SQL disponible esta en `C:\Users\eguillen\source\repos\DigitalCardsAppProject\docs\db_dcards.sql`.

## Paginas principales

- `Registry.aspx`: registro de clientes.
- `Login.aspx`: login de clientes y administradores.
- `ClientPage.aspx`: vista de cliente con QR y lista de tarjetas.
- `BusinessLogin.aspx`: login de negocio.
- `BusinessDashboardPage.aspx`: dashboard de negocio con ultimos sellos y grafica anual.
- `BusinessInsertionPage.aspx`: alta/asociacion de cliente a negocio y generacion de Google Wallet.
- `BusinessCheckPage.aspx`: agregado de sello a una tarjeta.
- `AdminInsertionPage.aspx`: alta de negocio y creacion de clase Google Wallet.
- `AdminDisplayPage.aspx`: listado de negocios.
- `AdminModPage.aspx`: modificacion/eliminacion de negocio.
- `RequestPasswordResetPage.aspx` y `ResetPasswordPage.aspx`: recuperacion de contrasena de cliente.
- `Logout.aspx` y `NotAuthorized.aspx`: cierre de sesion y pagina de acceso no autorizado.

## Dependencias relevantes

NuGet usa `packages.config`. Las dependencias mas relevantes son:

- `MySql.Data`: conexion MySQL.
- `MailKit` y `MimeKit`: envio de correo.
- `Google.Apis.Walletobjects.v1`, `Google.Apis.Auth`, `Google.Apis.Core`: Google Wallet.
- `System.IdentityModel.Tokens.Jwt` y `Microsoft.IdentityModel.*`: JWT para Google Wallet.
- `Newtonsoft.Json`: serializacion JSON.
- `QRCoder`: QR para cliente.
- `BouncyCastle.Cryptography`: presente, pero no se encontro uso productivo claro para Apple Wallet.

Frontend:

- Bootstrap/template vendorizado en `assets`.
- jQuery, ApexCharts, SimpleBar.
- DataTables vendorizado en `DataTables`.

## Conexion a base de datos

La conexion se lee desde `ConfigurationManager.ConnectionStrings["DCConnectionString"]` en:

- `Connectors/AdminConnector.cs`.
- `Connectors/BusinessConnector.cs`.
- `Connectors/ClientConnector.cs`.

El proveedor real usado es `MySql.Data.MySqlClient`. Hay un metodo `BusinessConnector.GetClientData` que usa `SqlConnection` de SQL Server con el connection string de MySQL; parece codigo muerto o defectuoso.

Se detecto una connection string hardcodeada en `DigitalCardsApp/Web.config` y en `DigitalCardsApp/bin/DigitalCardsApp.dll.config`. No se copio el valor. Debe rotarse y moverse a un secret store.

## Base de datos

Tablas detectadas en `docs/db_dcards.sql`:

- `UserClient`: clientes y admin, con `RoleID`.
- `Role`: roles `ADMIN` y `CLIENT`.
- `Business`: negocios, credenciales y logo.
- `ClientCard`: relacion cliente-negocio-tarjeta con contadores de sellos y `CardIDGoogle`.
- `ApplePass`: datos iniciales para Apple Wallet, sin llaves foraneas definidas.
- `PasswordResetToken`: tokens de recuperacion de contrasena.

Stored procedures principales:

- Cliente: `spInsertUserClientData`, `spSelectUserClientData`, `spCheckIfEmailExist`, `spUpdateClientPassword`.
- Negocio/admin: `spInsertBusinessData`, `spGetBusinessData`, `spGetBusinessDetails`, `spModifyBusinessData`, `spDeleteBusinessData`, `spSelectBusinessData`.
- Tarjetas/sellos: `spInsertCardData`, `spGetCardDataBusiness`, `spGetClientCards`, `spGetCardCreatedTime`, `spIncreaseCheckQTY`, `spGetCardDataChecks`, `spGetLast5Checks`, `spGetYearData`.
- Password reset: `spStorePasswordResetToken`, `spValidateResetToken`, `spGetEmailByToken`, `spDeletePasswordResetToken`.
- Apple parcial: `spInsertPassData`.

El SQL dump incluye datos insertados con correos, tokens y hashes. Debe tratarse como sensible y limpiarse antes de compartir o versionar.

## Correo

El envio de correo se hace con `MailKit.Net.Smtp` y `MimeKit` directamente desde code-behind:

- `BusinessInsertionPage.aspx.cs`: envia correo con link de Google Wallet.
- `RequestPasswordResetPage.aspx.cs`: envia correo de recuperacion de contrasena.

Hay configuracion SMTP en `Web.config`, pero el codigo tambien contiene credenciales SMTP hardcodeadas. No se copiaron valores. Deben rotarse y moverse a configuracion segura.

La plantilla `Tools/EmailFormat.html` contiene placeholders para Google y Apple Wallet, pero el flujo real de `BusinessInsertionPage.aspx.cs` usa una plantilla inline y solo manda Google Wallet.

## Flujo actual

1. Cliente se registra en `Registry.aspx`.
2. `ClientConnector.InsertClientData` llama `spInsertUserClientData`.
3. Negocio inicia sesion en `BusinessLogin.aspx`.
4. Negocio agrega un cliente por nombre/usuario en `BusinessInsertionPage.aspx`.
5. `BusinessConnector.GetUserInfo` obtiene datos del cliente.
6. `BusinessConnector.InsertClientData` llama `spInsertCardData`, creando `ClientCard` si no existe.
7. Se crea objeto Google Wallet con `Loyalty.CreateObject`.
8. Se genera JWT con `Loyalty.CreateJWTExistingObjects`.
9. Se envia correo con boton de Google Wallet.
10. Negocio agrega sello en `BusinessCheckPage.aspx`.
11. `spIncreaseCheckQTY` actualiza `CheckQTY`, `HistoricCheckQTY` y `LastCheck`.
12. `Loyalty.PatchObject` actualiza el objeto Google Wallet.

## Logica de tarjetas y sellos

`ClientCard` es el agregado practico de lealtad. Tiene:

- `CardID`.
- `CardIDGoogle`.
- `CreationDate`.
- `CheckQTY`.
- `LastCheck`.
- `UserID`.
- `BusinessID`.
- `HistoricCheckQTY`.

`spInsertCardData` crea una relacion solo si no existe la combinacion cliente-negocio. Al crearla asigna un sello inicial. `spIncreaseCheckQTY` incrementa sellos y reinicia `CheckQTY` cuando supera cierto limite, mientras `HistoricCheckQTY` sigue aumentando.

## Riesgos tecnicos

- Secretos hardcodeados en config, codigo y JSON de service account.
- Artefactos `bin` versionados con configuracion sensible.
- Password hashing inseguro: SHA-256 sin sal, sin KDF y con columnas `varchar(25)` que no alcanzan para hash SHA-256 hex completo.
- Autenticacion basada en `Session` y roles numericos manuales.
- Bloqueo por intentos fallidos guardado en sesion, facil de evadir.
- HTML generado con `StringBuilder` sin encoding claro, con riesgo XSS.
- Datos sensibles en SQL dump.
- Inconsistencias de nombres: tabla `Business`, consulta antigua a `Bussiness`, columnas `IDUser` vs `UserID`.
- Stored procedures con parametros que no coinciden con tipos reales, por ejemplo `spCheckIfEmailExist` usa `INT` para email.
- Flujo Wallet acoplado al code-behind, sin abstraccion ni reintentos.
- No hay pruebas automatizadas detectadas.
- No hay endpoints Apple Wallet ni contratos publicos claros para actualizacion de tarjetas.

