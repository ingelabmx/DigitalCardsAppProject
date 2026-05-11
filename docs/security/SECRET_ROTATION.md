# Secret Rotation Notes

## Alcance

Se detectaron credenciales productivas o sensibles en configuracion y codigo del
proyecto legado. Los valores reales no se documentan aqui.

## Secretos Que Deben Rotarse

- Credenciales SMTP usadas para enviar correos.
- Connection string de base de datos legacy.
- Service account de Google Wallet.
- Cualquier certificado o llave que haya sido usado para Wallets.

## Cambios Aplicados En Repo

- `Web.config` y `bin/DigitalCardsApp.dll.config` usan placeholders.
- Los code-behind de correo leen `SmtpFrom`, `SmtpHost`, `SmtpPort`,
  `SmtpUserName` y `SmtpPassword` desde variables de entorno o `appSettings`.
- Google Wallet legacy exige una credencial externa y ya no usa un fallback a
  `GW-K/*.json`; la app moderna usa
  `%USERPROFILE%\.digitalcards\appsettings.Local.json` con
  `DigitalCards:GoogleWallet:CredentialsFilePath`.
- Se retiro el JSON de service account del proyecto y se agregaron reglas de
  ignore para secretos locales.
- Se retiraron binarios propios generados del legacy (`DigitalCardsApp.dll` y
  `.pdb` en `bin/` y `obj/`) para evitar conservar literales compilados.

## Acciones Requeridas Fuera Del Repo

1. Rotar la contrasena del usuario de base de datos.
2. Rotar usuario/contrasena o token SMTP.
3. Revocar y recrear la service account de Google Wallet.
4. Confirmar que ningun ambiente productivo use las credenciales antiguas.
5. Si este repo fue compartido, purgar secretos del historial Git con una
   herramienta como `git filter-repo` y forzar rotacion completa.
6. Mantener `bin/` y `obj/` fuera del control de versiones; reconstruirlos en
   ambientes locales o CI.
