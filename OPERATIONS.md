# OPERATIONS.md — DigitalCardsAppProject en Ubuntu Server

Guía operativa para instalar, mantener, respaldar, actualizar y diagnosticar `DigitalCardsAppProject` en el servidor Ubuntu local.

---

## 1. Resumen ejecutivo

Este servidor hospeda la aplicación web `DigitalCardsAppProject` usando la siguiente arquitectura:

```text
Internet / usuarios
        ↓
Cloudflare Tunnel
        ↓
Apache 2.4 :80
        ↓
ASP.NET Core / Kestrel 127.0.0.1:5000
        ↓
MariaDB local 10.11
```

La aplicación corre como servicio `systemd` llamado:

```text
digitalcards.service
```

El túnel de Cloudflare corre como:

```text
cloudflared.service
```

Los dominios esperados son:

```text
puntelio.com
www.puntelio.com
app.puntelio.com
```

---

## 2. Contexto del servidor

### 2.1 Datos generales

```text
Sistema operativo: Ubuntu Server 24.04 LTS / 24.04.3 LTS
Hostname: ubuntu-server
Usuario principal: dang
IP local fija: 192.168.1.98
Arquitectura: x86_64
SSH: habilitado
```

### 2.2 Servicios principales

```text
ssh
apache2
mariadb
docker
cloudflared
digitalcards
```

### 2.3 Software base

```text
Apache 2.4
PHP 8.3
MariaDB 10.11
Docker 28+
Docker Compose plugin
.NET SDK / Runtime 8
Cloudflared
Git
Certbot
```

---

## 3. Contexto del proyecto

Repositorio:

```text
https://github.com/ingelabmx/DigitalCardsAppProject
```

Ruta local del código fuente:

```text
/home/dang/src/DigitalCardsAppProject
```

Proyecto web principal:

```text
/home/dang/src/DigitalCardsAppProject/src/DigitalCards.Web/DigitalCards.Web.csproj
```

Framework:

```text
.NET 8 / ASP.NET Core
```

Ruta de publicación en producción:

```text
/var/www/digitalcards/publish
```

Ruta base de archivos persistentes:

```text
/var/www/digitalcards/shared
```

Ruta de logs personalizados:

```text
/var/log/digitalcards
```

---

## 4. Arquitectura final de producción

### 4.1 Flujo de tráfico

```text
Usuario abre https://puntelio.com
        ↓
Cloudflare resuelve el dominio
        ↓
Cloudflare Tunnel envía tráfico al servidor local
        ↓
cloudflared entrega a http://localhost:80
        ↓
Apache recibe en puerto 80
        ↓
Apache hace proxy a http://127.0.0.1:5000
        ↓
Kestrel sirve DigitalCards.Web
        ↓
La app consulta MariaDB local
```

### 4.2 Por qué se usa Apache y no Nginx

El servidor ya tiene Apache activo y está preparado para hospedar múltiples sitios mediante VirtualHosts. Por eso se decidió usar Apache como reverse proxy local en vez de instalar Nginx. Esto evita duplicar servidores web y reduce conflictos de puertos.

---

## 5. Rutas importantes

### 5.1 Código fuente

```bash
/home/dang/src/DigitalCardsAppProject
```

### 5.2 Publicación de la app

```bash
/var/www/digitalcards/publish
```

### 5.3 Archivos persistentes compartidos

```bash
/var/www/digitalcards/shared/uploads/business-logos
/var/www/digitalcards/shared/data-protection-keys
/var/www/digitalcards/shared/secrets
```

### 5.4 Configuración local del usuario `dang`

Usada para pruebas manuales con `dotnet run`:

```bash
/home/dang/.digitalcards/appsettings.Local.json
```

### 5.5 Configuración local del servicio `digitalcards`

Usada en producción por `systemd`:

```bash
/home/digitalcards/.digitalcards/appsettings.Local.json
```

### 5.6 Cloudflared

Configuración usada por el servicio:

```bash
/etc/cloudflared/config.yml
```

Credencial del túnel:

```bash
/etc/cloudflared/6f3cdec4-cbca-41ce-a6dd-eb2c982215df.json
```

### 5.7 Apache VirtualHost

```bash
/etc/apache2/sites-available/digitalcards.conf
```

### 5.8 Servicio systemd

```bash
/etc/systemd/system/digitalcards.service
```

---

## 6. Servicios del sistema

### 6.1 Ver estado de la aplicación

```bash
sudo systemctl status digitalcards
```

### 6.2 Reiniciar aplicación

```bash
sudo systemctl restart digitalcards
```

### 6.3 Detener aplicación

```bash
sudo systemctl stop digitalcards
```

### 6.4 Iniciar aplicación

```bash
sudo systemctl start digitalcards
```

### 6.5 Ver logs de aplicación

Últimos logs:

```bash
journalctl -u digitalcards -n 100 --no-pager
```

Logs en vivo:

```bash
journalctl -u digitalcards -f
```

### 6.6 Validar que arranca automáticamente

```bash
sudo systemctl is-enabled digitalcards
```

Debe responder:

```text
enabled
```

---

## 7. Servicio `digitalcards.service`

Archivo:

```bash
/etc/systemd/system/digitalcards.service
```

Contenido esperado:

```ini
[Unit]
Description=DigitalCards Web
After=network.target mariadb.service

[Service]
WorkingDirectory=/var/www/digitalcards/publish
ExecStart=/usr/bin/dotnet /var/www/digitalcards/publish/DigitalCards.Web.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=digitalcards
User=digitalcards

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=HOME=/home/digitalcards
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

Después de editar este archivo, ejecutar:

```bash
sudo systemctl daemon-reload
sudo systemctl restart digitalcards
sudo systemctl status digitalcards
```

---

## 8. Apache reverse proxy

Archivo:

```bash
/etc/apache2/sites-available/digitalcards.conf
```

Configuración esperada:

```apache
<VirtualHost *:80>
    ServerName puntelio.com
    ServerAlias www.puntelio.com app.puntelio.com

    ProxyPreserveHost On
    ProxyRequests Off

    RequestHeader set X-Forwarded-Proto "https"
    RequestHeader set X-Forwarded-Port "443"

    ProxyPass / http://127.0.0.1:5000/
    ProxyPassReverse / http://127.0.0.1:5000/

    ErrorLog ${APACHE_LOG_DIR}/digitalcards_error.log
    CustomLog ${APACHE_LOG_DIR}/digitalcards_access.log combined
</VirtualHost>
```

Módulos necesarios:

```bash
sudo a2enmod proxy
sudo a2enmod proxy_http
sudo a2enmod headers
sudo a2enmod rewrite
sudo systemctl restart apache2
```

Activar sitio:

```bash
sudo a2ensite digitalcards.conf
sudo apachectl configtest
sudo systemctl reload apache2
```

Validar Apache:

```bash
sudo systemctl status apache2
curl -I http://127.0.0.1
```

### 8.1 Warning de ServerName

Si aparece:

```text
AH00558: apache2: Could not reliably determine the server's fully qualified domain name
```

No es crítico si `Syntax OK` aparece. Para quitarlo:

```bash
echo "ServerName ubuntu-server" | sudo tee /etc/apache2/conf-available/servername.conf
sudo a2enconf servername
sudo apachectl configtest
sudo systemctl reload apache2
```

---

## 9. Cloudflare Tunnel

### 9.1 Servicio

Ver estado:

```bash
sudo systemctl status cloudflared
```

Reiniciar:

```bash
sudo systemctl restart cloudflared
```

Logs:

```bash
journalctl -u cloudflared -n 100 --no-pager
journalctl -u cloudflared -f
```

Validar arranque automático:

```bash
sudo systemctl is-enabled cloudflared
```

Debe responder:

```text
enabled
```

### 9.2 Configuración esperada

Archivo:

```bash
/etc/cloudflared/config.yml
```

Contenido esperado:

```yaml
tunnel: 6f3cdec4-cbca-41ce-a6dd-eb2c982215df
credentials-file: /etc/cloudflared/6f3cdec4-cbca-41ce-a6dd-eb2c982215df.json

ingress:
  - hostname: puntelio.com
    service: http://localhost:80
  - hostname: www.puntelio.com
    service: http://localhost:80
  - hostname: app.puntelio.com
    service: http://localhost:80
  - service: http_status:404
```

### 9.3 Error común: origin service refused

Error:

```text
Unable to reach the origin service
connect: connection refused
```

Significa que `cloudflared` sí funciona, pero no puede llegar al servicio configurado en `service:`.

Validaciones:

```bash
sudo systemctl status apache2
curl -I http://127.0.0.1
sudo ss -tulpn | grep -E ':80|:5000|:5031'
```

En producción, `cloudflared` debe apuntar a Apache:

```yaml
service: http://localhost:80
```

No debe apuntar a `5031`, porque `5031` era solo para `dotnet run --launch-profile http`.

---

## 10. Base de datos

### 10.1 Estado actual recomendado

La base de datos debe residir localmente en MariaDB del servidor Ubuntu.

Base de datos:

```text
alltrac1_dcards
```

Usuario de aplicación:

```text
digitalcards_user
```

Host local:

```text
127.0.0.1
```

Puerto:

```text
3306
```

### 10.2 Connection string local esperado

En producción:

```bash
sudo nano /home/digitalcards/.digitalcards/appsettings.Local.json
```

Connection string:

```json
"ConnectionStrings": {
  "DigitalCards": "Server=127.0.0.1;Port=3306;Database=alltrac1_dcards;User ID=digitalcards_user;Password=PASSWORD_LOCAL;CharSet=utf8mb4;SslMode=None;"
}
```

Para pruebas manuales:

```bash
nano /home/dang/.digitalcards/appsettings.Local.json
```

Debe tener el mismo connection string local.

### 10.3 Connection string anterior de HostGator

La conexión anterior apuntaba a:

```text
Server=162.241.2.108
Database=alltrac1_dcards
User ID=alltrac1_dcard_admin
```

Una vez migrada la base local, este host remoto ya no debe aparecer en los archivos de configuración.

Validar que ya no existe referencia a HostGator:

```bash
sudo grep -RIn "162.241.2.108\|alltrac1_dcard_admin" \
  /home/digitalcards/.digitalcards/appsettings.Local.json \
  /home/dang/.digitalcards/appsettings.Local.json
```

Si no muestra salida, la app ya no apunta a HostGator.

---

## 11. Migración de base de datos desde HostGator/phpMyAdmin a MariaDB local

### 11.1 Objetivo

Migrar completamente la base `alltrac1_dcards` conservando:

```text
Tablas
Registros
Stored procedures
Functions
Triggers
Events
AUTO_INCREMENT
Vistas
```

### 11.2 Exportación correcta desde phpMyAdmin

En phpMyAdmin seleccionar:

```text
Export method: Custom
Format: SQL
Database: alltrac1_dcards
Output: Save output to a file
Character set: utf-8
Structure and data
```

Marcar:

```text
Display comments
Enclose export in a transaction
Disable foreign key checks
Add DROP DATABASE IF EXISTS statement, si está disponible
Add DROP TABLE / VIEW / PROCEDURE / FUNCTION / EVENT / TRIGGER statement
Add CREATE TABLE statement
AUTO_INCREMENT value
Add CREATE VIEW statement
Add CREATE PROCEDURE / FUNCTION / EVENT statement
Add CREATE TRIGGER statement
Enclose table and column names with backquotes
Dump binary columns in hexadecimal notation
Dump TIMESTAMP columns in UTC
```

En inserts seleccionar:

```text
Function: INSERT
Syntax: both of the above
```

Esto genera inserts con columnas y múltiples filas.

### 11.3 Subir SQL desde Windows al servidor

Desde PowerShell en Windows:

```powershell
scp "C:\Users\eguillen\Downloads\alltrac1_dcards.sql" dang@192.168.1.98:/home/dang/
```

Validar en Ubuntu:

```bash
ls -lh /home/dang/alltrac1_dcards.sql
```

### 11.4 Detener la aplicación antes de restaurar

```bash
sudo systemctl stop digitalcards
```

### 11.5 Crear copia limpia sin DEFINER

Algunos exports de HostGator/phpMyAdmin pueden traer `DEFINER`, por ejemplo:

```sql
CREATE DEFINER=`root`@`localhost` PROCEDURE ...
```

Para evitar errores de permisos al importar:

```bash
cp /home/dang/alltrac1_dcards.sql /home/dang/alltrac1_dcards.original.sql

sed -E 's/DEFINER=`[^`]+`@`[^`]+`//g' \
  /home/dang/alltrac1_dcards.original.sql \
  > /home/dang/alltrac1_dcards.clean.sql
```

### 11.6 Crear base local si el SQL no trae `CREATE DATABASE` ni `USE`

Si al importar sale:

```text
ERROR 1046 (3D000): No database selected
```

Crear la base:

```bash
sudo mariadb -e "
CREATE DATABASE IF NOT EXISTS alltrac1_dcards
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;
"
```

Importar indicando la base:

```bash
sudo mariadb alltrac1_dcards < /home/dang/alltrac1_dcards.clean.sql
```

### 11.7 Validar importación

```bash
sudo mariadb
```

Dentro:

```sql
USE alltrac1_dcards;

SHOW TABLES;

SELECT COUNT(*) AS routines
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'alltrac1_dcards';

SELECT ROUTINE_TYPE, ROUTINE_NAME
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'alltrac1_dcards';

SELECT COUNT(*) AS triggers_count
FROM information_schema.TRIGGERS
WHERE TRIGGER_SCHEMA = 'alltrac1_dcards';

SELECT COUNT(*) AS events_count
FROM information_schema.EVENTS
WHERE EVENT_SCHEMA = 'alltrac1_dcards';

EXIT;
```

Si `routines` sale en `0`, el export no incluyó stored procedures/functions correctamente.

### 11.8 Crear o corregir usuario local de aplicación

Entrar como root:

```bash
sudo mariadb
```

Dentro, reemplazar `PASSWORD_LOCAL` por una contraseña segura:

```sql
CREATE USER IF NOT EXISTS 'digitalcards_user'@'localhost'
IDENTIFIED BY 'PASSWORD_LOCAL';

CREATE USER IF NOT EXISTS 'digitalcards_user'@'127.0.0.1'
IDENTIFIED BY 'PASSWORD_LOCAL';

ALTER USER 'digitalcards_user'@'localhost'
IDENTIFIED BY 'PASSWORD_LOCAL';

ALTER USER 'digitalcards_user'@'127.0.0.1'
IDENTIFIED BY 'PASSWORD_LOCAL';

GRANT ALL PRIVILEGES ON alltrac1_dcards.* TO 'digitalcards_user'@'localhost';
GRANT ALL PRIVILEGES ON alltrac1_dcards.* TO 'digitalcards_user'@'127.0.0.1';

FLUSH PRIVILEGES;
EXIT;
```

### 11.9 Probar conexión local con usuario de app

```bash
mariadb \
  -h 127.0.0.1 \
  -P 3306 \
  -u digitalcards_user \
  -p \
  alltrac1_dcards
```

Dentro:

```sql
SHOW TABLES;

SELECT ROUTINE_TYPE, ROUTINE_NAME
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'alltrac1_dcards';

EXIT;
```

### 11.10 Error común: Access denied para digitalcards_user

Error:

```text
ERROR 1045 (28000): Access denied for user 'digitalcards_user'@'localhost'
```

Corrección:

```bash
sudo mariadb
```

Dentro:

```sql
SELECT User, Host, plugin
FROM mysql.user
WHERE User = 'digitalcards_user';

ALTER USER 'digitalcards_user'@'localhost'
IDENTIFIED BY 'PASSWORD_LOCAL';

ALTER USER 'digitalcards_user'@'127.0.0.1'
IDENTIFIED BY 'PASSWORD_LOCAL';

GRANT ALL PRIVILEGES ON alltrac1_dcards.* TO 'digitalcards_user'@'localhost';
GRANT ALL PRIVILEGES ON alltrac1_dcards.* TO 'digitalcards_user'@'127.0.0.1';

FLUSH PRIVILEGES;
EXIT;
```

Volver a probar la conexión.

---

## 12. Cambiar connection string a MariaDB local

### 12.1 Archivo de producción

```bash
sudo nano /home/digitalcards/.digitalcards/appsettings.Local.json
```

Cambiar de HostGator:

```json
"DigitalCards": "Server=162.241.2.108;Port=3306;Database=alltrac1_dcards;User ID=alltrac1_dcard_admin;Password=PASSWORD_HOSTGATOR;CharSet=utf8mb4;SslMode=Preferred;"
```

A local:

```json
"DigitalCards": "Server=127.0.0.1;Port=3306;Database=alltrac1_dcards;User ID=digitalcards_user;Password=PASSWORD_LOCAL;CharSet=utf8mb4;SslMode=None;"
```

### 12.2 Archivo para pruebas manuales

```bash
nano /home/dang/.digitalcards/appsettings.Local.json
```

Usar el mismo connection string local.

### 12.3 Reiniciar la app

```bash
sudo systemctl restart digitalcards
sudo systemctl status digitalcards
journalctl -u digitalcards -n 100 --no-pager
```

---

## 13. Secretos y archivos requeridos

La app usa configuración y secretos fuera del repositorio.

### 13.1 Archivos importantes

```bash
/home/digitalcards/.digitalcards/appsettings.Local.json
/home/digitalcards/.digitalcards/google-wallet-service-account.json
/home/digitalcards/.digitalcards/apple-wallet/certs/digitalcards-pass-type.p12
/home/digitalcards/.digitalcards/apple-wallet/certs/AppleWWDRCAG4.cer
/home/digitalcards/.digitalcards/apple-wallet/assets
/home/digitalcards/.digitalcards/uploads/business-logos
/home/digitalcards/.digitalcards/data-protection-keys
```

### 13.2 Permisos recomendados

```bash
sudo chown -R digitalcards:digitalcards /home/digitalcards
sudo chmod 700 /home/digitalcards/.digitalcards
sudo find /home/digitalcards/.digitalcards -type f -exec chmod 600 {} \;
sudo find /home/digitalcards/.digitalcards -type d -exec chmod 700 {} \;
```

### 13.3 Error común: Google Wallet credentials file was not found

Error:

```text
The configured Google Wallet credentials file was not found.
```

Revisar ruta configurada:

```bash
sudo grep -RIn "CredentialsFilePath\|google-wallet\|GoogleWallet" \
  /home/digitalcards/.digitalcards/appsettings.Local.json
```

Buscar archivo:

```bash
sudo find /home/digitalcards/.digitalcards -iname "*google*" -o -iname "*.json"
```

Si el archivo está en `/home/dang/.digitalcards`, copiarlo:

```bash
sudo cp /home/dang/.digitalcards/google-wallet-service-account.json \
  /home/digitalcards/.digitalcards/

sudo chown digitalcards:digitalcards \
  /home/digitalcards/.digitalcards/google-wallet-service-account.json

sudo chmod 600 \
  /home/digitalcards/.digitalcards/google-wallet-service-account.json
```

En `appsettings.Local.json` debe apuntar a la ruta que el servicio puede leer, por ejemplo:

```json
"CredentialsFilePath": "/home/digitalcards/.digitalcards/google-wallet-service-account.json"
```

---

## 14. Rutas Windows que rompen MSBuild en Ubuntu

### 14.1 Problema observado

Error:

```text
MSB3552: Resource file "**/*.resx" cannot be found
```

Causa encontrada:

Se habían creado carpetas con rutas Windows dentro del proyecto, por ejemplo:

```text
./src/DigitalCards.Web/C:\Users\eguillen\.digitalcards\uploads\business-logos
./src/DigitalCards.Web/C:\Users\eguillen\.digitalcards\data-protection-keys
```

Esto rompe la expansión de globs de MSBuild en Linux.

### 14.2 Detectar rutas problemáticas

```bash
cd ~/src/DigitalCardsAppProject
find . \( -name '*\\*' -o -name '*:*' \) -print
```

### 14.3 Borrar rutas problemáticas

```bash
cd ~/src/DigitalCardsAppProject
find . \( -name '*\\*' -o -name '*:*' \) -exec rm -rf {} +
```

### 14.4 Buscar rutas Windows en configuración

```bash
grep -RIn "C:\\\\Users\|C:\\|eguillen" \
  /home/dang/.digitalcards \
  /home/digitalcards/.digitalcards \
  ~/src/DigitalCardsAppProject
```

Corregir en `appsettings.Local.json` cualquier ruta como:

```json
"C:\\Users\\eguillen\\.digitalcards\\uploads\\business-logos"
```

Por ruta Linux:

```json
"/home/digitalcards/.digitalcards/uploads/business-logos"
```

O en pruebas manuales:

```json
"/home/dang/.digitalcards/uploads/business-logos"
```

---

## 15. Probar la app manualmente

Esto solo es para desarrollo o diagnóstico, no para producción.

```bash
cd ~/src/DigitalCardsAppProject

dotnet run --project src/DigitalCards.Web/DigitalCards.Web.csproj --launch-profile http
```

Normalmente escucha en:

```text
http://localhost:5031
```

Esto solo es accesible desde el propio servidor. Para producción se usa el servicio en:

```text
http://127.0.0.1:5000
```

Para detener la app manual:

```text
Ctrl + C
```

---

## 16. Publicar actualización desde GitHub

### 16.1 Flujo recomendado de actualización

```bash
cd ~/src/DigitalCardsAppProject

git pull

find . \( -name '*\\*' -o -name '*:*' \) -exec rm -rf {} +
find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +

rm -rf /tmp/digitalcards-publish

dotnet publish src/DigitalCards.Web/DigitalCards.Web.csproj \
  -c Release \
  -o /tmp/digitalcards-publish

sudo systemctl stop digitalcards

sudo rsync -av --delete /tmp/digitalcards-publish/ /var/www/digitalcards/publish/

sudo chown -R digitalcards:digitalcards /var/www/digitalcards
sudo chown -R digitalcards:digitalcards /var/log/digitalcards

sudo systemctl start digitalcards
sudo systemctl status digitalcards
```

### 16.2 Ver logs después del deploy

```bash
journalctl -u digitalcards -n 100 --no-pager
```

### 16.3 Validar HTTP local

```bash
curl -I http://127.0.0.1:5000
curl -I http://127.0.0.1
```

### 16.4 Validar dominio

Abrir en navegador:

```text
https://puntelio.com
https://www.puntelio.com
https://app.puntelio.com
```

---

## 17. Script sugerido de actualización

Crear archivo:

```bash
nano /home/dang/deploy-digitalcards.sh
```

Contenido:

```bash
#!/usr/bin/env bash
set -euo pipefail

APP_REPO="/home/dang/src/DigitalCardsAppProject"
PUBLISH_TMP="/tmp/digitalcards-publish"
PUBLISH_DIR="/var/www/digitalcards/publish"
PROJECT="src/DigitalCards.Web/DigitalCards.Web.csproj"

cd "$APP_REPO"

echo "==> Pull latest changes"
git pull

echo "==> Remove problematic Windows-style paths if any"
find . \( -name '*\\*' -o -name '*:*' \) -exec rm -rf {} + || true

echo "==> Clean bin/obj"
find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} + || true

echo "==> Publish Release"
rm -rf "$PUBLISH_TMP"
dotnet publish "$PROJECT" -c Release -o "$PUBLISH_TMP"

echo "==> Stop service"
sudo systemctl stop digitalcards

echo "==> Sync publish output"
sudo rsync -av --delete "$PUBLISH_TMP/" "$PUBLISH_DIR/"

echo "==> Fix permissions"
sudo chown -R digitalcards:digitalcards /var/www/digitalcards
sudo chown -R digitalcards:digitalcards /var/log/digitalcards

echo "==> Start service"
sudo systemctl start digitalcards
sudo systemctl status digitalcards --no-pager

echo "==> Local checks"
curl -I http://127.0.0.1:5000 || true
curl -I http://127.0.0.1 || true

echo "==> Done"
```

Dar permisos:

```bash
chmod +x /home/dang/deploy-digitalcards.sh
```

Ejecutar:

```bash
/home/dang/deploy-digitalcards.sh
```

---

## 18. Backups

### 18.1 Qué respaldar

Respaldar siempre:

```text
Base de datos MariaDB alltrac1_dcards
/home/digitalcards/.digitalcards/appsettings.Local.json
/home/digitalcards/.digitalcards/google-wallet-service-account.json
/home/digitalcards/.digitalcards/apple-wallet
/home/digitalcards/.digitalcards/uploads
/home/digitalcards/.digitalcards/data-protection-keys
/etc/cloudflared/config.yml
/etc/cloudflared/*.json
/etc/apache2/sites-available/digitalcards.conf
/etc/systemd/system/digitalcards.service
```

### 18.2 Backup manual de base local

```bash
sudo mkdir -p /backups/digitalcards
sudo chown -R dang:dang /backups/digitalcards

mariadb-dump \
  -u digitalcards_user \
  -p \
  --databases alltrac1_dcards \
  --routines \
  --triggers \
  --events \
  --single-transaction \
  --quick \
  --add-drop-database \
  --add-drop-table \
  --default-character-set=utf8mb4 \
  --no-tablespaces \
  > /backups/digitalcards/alltrac1_dcards_$(date +%F_%H%M).sql
```

### 18.3 Backup manual de archivos críticos

```bash
sudo tar -czf /backups/digitalcards/digitalcards_files_$(date +%F_%H%M).tar.gz \
  /home/digitalcards/.digitalcards \
  /etc/cloudflared \
  /etc/apache2/sites-available/digitalcards.conf \
  /etc/systemd/system/digitalcards.service
```

### 18.4 Restaurar base desde backup

Detener app:

```bash
sudo systemctl stop digitalcards
```

Restaurar:

```bash
sudo mariadb < /backups/digitalcards/alltrac1_dcards_YYYY-MM-DD_HHMM.sql
```

Reiniciar app:

```bash
sudo systemctl start digitalcards
sudo systemctl status digitalcards
```

---

## 19. Diagnóstico rápido

### 19.1 Estado de servicios

```bash
sudo systemctl status digitalcards
sudo systemctl status apache2
sudo systemctl status cloudflared
sudo systemctl status mariadb
```

### 19.2 Puertos

```bash
sudo ss -tulpn | grep -E ':80|:5000|:5031|:3306'
```

Esperado:

```text
Apache escuchando en :80
DigitalCards/Kestrel escuchando en 127.0.0.1:5000
MariaDB escuchando en :3306
```

### 19.3 HTTP local

```bash
curl -I http://127.0.0.1:5000
curl -I http://127.0.0.1
```

### 19.4 Logs de aplicación

```bash
journalctl -u digitalcards -n 100 --no-pager
```

### 19.5 Logs de Cloudflared

```bash
journalctl -u cloudflared -n 100 --no-pager
```

### 19.6 Logs de Apache

```bash
sudo tail -n 100 /var/log/apache2/digitalcards_error.log
sudo tail -n 100 /var/log/apache2/digitalcards_access.log
```

---

## 20. Errores comunes y correcciones

### 20.1 `MSB1011: Specify which project or solution file to use`

Causa: el repo tiene más de un proyecto/solución.

Usar siempre ruta explícita:

```bash
dotnet restore src/DigitalCards.Web/DigitalCards.Web.csproj
dotnet build src/DigitalCards.Web/DigitalCards.Web.csproj -c Release
dotnet publish src/DigitalCards.Web/DigitalCards.Web.csproj -c Release -o /tmp/digitalcards-publish
```

### 20.2 `MSB3552: Resource file "**/*.resx" cannot be found`

Causa: rutas Windows creadas dentro del proyecto.

Corrección:

```bash
cd ~/src/DigitalCardsAppProject
find . \( -name '*\\*' -o -name '*:*' \) -print
find . \( -name '*\\*' -o -name '*:*' \) -exec rm -rf {} +
find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
```

Luego corregir rutas en `appsettings.Local.json`.

### 20.3 `Access denied for user ... HostGator`

Causa: HostGator bloquea IP externa o credenciales incorrectas.

Solución usada durante migración: agregar IP pública del servidor en cPanel → Remote MySQL.

Después de migrar a MariaDB local, este problema ya no debe ocurrir.

### 20.4 `Unable to connect to any of the specified MySQL hosts`

Causa: host/puerto de MySQL no accesible.

Validar:

```bash
timeout 8 bash -c '</dev/tcp/HOST/3306' \
  && echo "PUERTO 3306 ABIERTO" \
  || echo "NO CONECTA AL PUERTO 3306"
```

### 20.5 `Access to the path /var/www/digitalcards/publish/... is denied`

Causa: publicar directamente en carpeta propiedad del usuario `digitalcards`.

Solución recomendada:

```bash
rm -rf /tmp/digitalcards-publish

dotnet publish src/DigitalCards.Web/DigitalCards.Web.csproj \
  -c Release \
  -o /tmp/digitalcards-publish

sudo systemctl stop digitalcards
sudo rsync -av --delete /tmp/digitalcards-publish/ /var/www/digitalcards/publish/
sudo chown -R digitalcards:digitalcards /var/www/digitalcards
sudo systemctl start digitalcards
```

### 20.6 `No database selected` al importar SQL

Error:

```text
ERROR 1046 (3D000): No database selected
```

Corrección:

```bash
sudo mariadb -e "CREATE DATABASE IF NOT EXISTS alltrac1_dcards CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
sudo mariadb alltrac1_dcards < /home/dang/alltrac1_dcards.clean.sql
```

### 20.7 `Access denied for user 'digitalcards_user'@'localhost'`

Corregir password y permisos:

```bash
sudo mariadb
```

```sql
ALTER USER 'digitalcards_user'@'localhost' IDENTIFIED BY 'PASSWORD_LOCAL';
ALTER USER 'digitalcards_user'@'127.0.0.1' IDENTIFIED BY 'PASSWORD_LOCAL';
GRANT ALL PRIVILEGES ON alltrac1_dcards.* TO 'digitalcards_user'@'localhost';
GRANT ALL PRIVILEGES ON alltrac1_dcards.* TO 'digitalcards_user'@'127.0.0.1';
FLUSH PRIVILEGES;
EXIT;
```

### 20.8 `The configured Google Wallet credentials file was not found`

Causa: el servicio no encuentra el JSON de credenciales.

Validar y corregir:

```bash
sudo grep -RIn "CredentialsFilePath" /home/digitalcards/.digitalcards/appsettings.Local.json
sudo find /home/digitalcards/.digitalcards -iname "*google*" -o -iname "*.json"
```

Copiar desde `dang` si es necesario:

```bash
sudo cp /home/dang/.digitalcards/google-wallet-service-account.json /home/digitalcards/.digitalcards/
sudo chown digitalcards:digitalcards /home/digitalcards/.digitalcards/google-wallet-service-account.json
sudo chmod 600 /home/digitalcards/.digitalcards/google-wallet-service-account.json
sudo systemctl restart digitalcards
```

---

## 21. Checklist después de reiniciar el servidor

Después de un `sudo reboot`, validar:

```bash
sudo systemctl status digitalcards
sudo systemctl status apache2
sudo systemctl status cloudflared
sudo systemctl status mariadb
```

Validar URLs locales:

```bash
curl -I http://127.0.0.1:5000
curl -I http://127.0.0.1
```

Validar dominios:

```text
https://puntelio.com
https://www.puntelio.com
https://app.puntelio.com
```

---

## 22. Checklist de actualización segura

Antes de actualizar:

```bash
sudo systemctl status digitalcards
sudo systemctl status mariadb
```

Backup DB:

```bash
mariadb-dump \
  -u digitalcards_user \
  -p \
  --databases alltrac1_dcards \
  --routines \
  --triggers \
  --events \
  --single-transaction \
  --quick \
  --no-tablespaces \
  > /backups/digitalcards/pre_deploy_$(date +%F_%H%M).sql
```

Actualizar:

```bash
/home/dang/deploy-digitalcards.sh
```

Validar:

```bash
sudo systemctl status digitalcards
journalctl -u digitalcards -n 100 --no-pager
curl -I http://127.0.0.1:5000
curl -I http://127.0.0.1
```

---

## 23. Notas operativas importantes

- No subir `appsettings.Local.json` al repositorio.
- No subir certificados Apple Wallet al repositorio.
- No subir JSON de Google Wallet al repositorio.
- No ejecutar la app productiva como `root`.
- No publicar directamente a `/var/www/digitalcards/publish` con usuario `dang` si la carpeta pertenece a `digitalcards`; usar `/tmp/digitalcards-publish` + `rsync`.
- No dejar rutas `C:\Users\eguillen\...` en configuración Linux.
- En producción, Apache debe apuntar a `127.0.0.1:5000`.
- En producción, Cloudflared debe apuntar a `localhost:80`.
- `5031` es solo para `dotnet run --launch-profile http`.
- Después de cambiar `appsettings.Local.json`, reiniciar `digitalcards`.
- Después de cambiar `/etc/systemd/system/digitalcards.service`, ejecutar `daemon-reload`.
- Después de cambiar Apache, ejecutar `apachectl configtest` y recargar Apache.
- Después de cambiar Cloudflared, reiniciar `cloudflared`.

---

## 24. Comandos rápidos

Estado general:

```bash
sudo systemctl status digitalcards apache2 cloudflared mariadb
```

Reiniciar app:

```bash
sudo systemctl restart digitalcards
```

Logs app:

```bash
journalctl -u digitalcards -n 100 --no-pager
```

Logs tunnel:

```bash
journalctl -u cloudflared -n 100 --no-pager
```

Ver puertos:

```bash
sudo ss -tulpn | grep -E ':80|:5000|:5031|:3306'
```

Probar app directa:

```bash
curl -I http://127.0.0.1:5000
```

Probar Apache:

```bash
curl -I http://127.0.0.1
```

Actualizar desde GitHub:

```bash
/home/dang/deploy-digitalcards.sh
```

---

## 25. Estado final esperado

```text
digitalcards.service: active (running), enabled
apache2.service: active (running), enabled
cloudflared.service: active (running), enabled
mariadb.service: active (running), enabled
```

La aplicación debe responder en:

```text
http://127.0.0.1:5000
http://127.0.0.1
https://puntelio.com
https://www.puntelio.com
https://app.puntelio.com
```

La base debe ser local:

```text
Server=127.0.0.1
Database=alltrac1_dcards
User ID=digitalcards_user
```
