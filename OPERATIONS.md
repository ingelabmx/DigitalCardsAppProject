# DigitalCardsAppProject — Guía de instalación, mantenimiento, respaldo y actualización

> Servidor objetivo: Ubuntu Server local para hospedar `DigitalCardsAppProject` con Apache, ASP.NET Core, MariaDB/HostGator MySQL, Cloudflare Tunnel y servicio `systemd`.

---

## 1. Contexto general del servidor

### Sistema operativo

- Servidor: Ubuntu Server 24.04 LTS / 24.04.3 LTS
- Hostname: `ubuntu-server`
- Usuario principal: `dang`
- IP local fija: `192.168.1.98`
- Acceso SSH habilitado
- Arquitectura: `x86_64`

### Servicios principales

- `ssh`
- `apache2`
- `mariadb`
- `docker`
- `cloudflared`
- `digitalcards`

### Stack instalado

- Apache 2.4
- PHP 8.3
- MariaDB 10.11
- Docker + Docker Compose
- Certbot
- .NET SDK / Runtime 8
- Cloudflared
- Git

---

## 2. Contexto del proyecto

Proyecto:

```text
DigitalCardsAppProject
```

Repositorio:

```text
https://github.com/ingelabmx/DigitalCardsAppProject
```

Ruta del código fuente en Ubuntu:

```text
/home/dang/src/DigitalCardsAppProject
```

Proyecto web principal:

```text
src/DigitalCards.Web/DigitalCards.Web.csproj
```

Framework:

```text
.NET 8 / ASP.NET Core
```

Dominios esperados:

```text
puntelio.com
www.puntelio.com
app.puntelio.com
```

---

## 3. Arquitectura final recomendada

```text
Internet
   ↓
Cloudflare Tunnel
   ↓
Apache 2.4 en Ubuntu, puerto 80
   ↓
ASP.NET Core / Kestrel, 127.0.0.1:5000
   ↓
Base de datos MySQL / MariaDB
```

En producción, la app no debe correr con `dotnet run`. Debe correr como servicio `systemd`:

```text
digitalcards.service
```

---

## 4. Rutas importantes

### Código fuente

```text
/home/dang/src/DigitalCardsAppProject
```

### Publicación de producción

```text
/var/www/digitalcards/publish
```

### Archivos compartidos

```text
/var/www/digitalcards/shared
```

### Uploads de logos

```text
/var/www/digitalcards/shared/uploads/business-logos
```

También puede usarse esta ruta durante pruebas/manual:

```text
/home/dang/.digitalcards/uploads/business-logos
```

### Data Protection keys

Producción recomendada:

```text
/var/www/digitalcards/shared/data-protection-keys
```

Durante pruebas/manual:

```text
/home/dang/.digitalcards/data-protection-keys
```

### Configuración local del usuario `dang`

```text
/home/dang/.digitalcards/appsettings.Local.json
```

### Configuración local del usuario de servicio

```text
/home/digitalcards/.digitalcards/appsettings.Local.json
```

### Cloudflared

Configuración del servicio:

```text
/etc/cloudflared/config.yml
```

Credencial del túnel:

```text
/etc/cloudflared/6f3cdec4-cbca-41ce-a6dd-eb2c982215df.json
```

---

## 5. Archivos sensibles que NO deben subirse a GitHub

Nunca subir al repositorio:

```text
appsettings.Local.json
google-wallet-service-account.json
certificados Apple Wallet
archivos .p12
archivos .cer
credenciales de Cloudflare Tunnel
uploads privados
Data Protection keys
```

Ejemplos de archivos sensibles:

```text
/home/dang/.digitalcards/appsettings.Local.json
/home/digitalcards/.digitalcards/appsettings.Local.json
/home/digitalcards/.digitalcards/google-wallet-service-account.json
/etc/cloudflared/*.json
```

---

## 6. Revisión de configuración crítica

### Revisar configuración de usuario `dang`

```bash
nano /home/dang/.digitalcards/appsettings.Local.json
```

### Revisar configuración de usuario `digitalcards`

```bash
sudo nano /home/digitalcards/.digitalcards/appsettings.Local.json
```

### Buscar rutas Windows incorrectas

Este proyecto tuvo un problema porque se crearon carpetas con rutas tipo Windows dentro del proyecto, por ejemplo:

```text
C:\Users\eguillen\.digitalcards\uploads\business-logos
C:\Users\eguillen\.digitalcards\data-protection-keys
```

En Linux eso puede romper MSBuild y generar el error:

```text
MSB3552: Resource file "**/*.resx" cannot be found.
```

Para detectar esas rutas:

```bash
cd ~/src/DigitalCardsAppProject

find . \( -name '*\\*' -o -name '*:*' \) -print
```

Para borrarlas:

```bash
find . \( -name '*\\*' -o -name '*:*' \) -exec rm -rf {} +
```

Para buscar rutas Windows dentro de configuración:

```bash
grep -RIn "C:\\\\Users\\|C:\\|eguillen" /home/dang/.digitalcards ~/src/DigitalCardsAppProject
grep -RIn "C:\\\\Users\\|C:\\|eguillen" /home/digitalcards/.digitalcards
```

---

## 7. Configuración esperada de rutas Linux

Dentro de `appsettings.Local.json`, las rutas deben ser Linux, no Windows.

Ejemplo correcto:

```json
"Branding": {
  "LogoUploads": {
    "Path": "/home/digitalcards/.digitalcards/uploads/business-logos",
    "RequestPath": "/uploads/business-logos",
    "MaxBytes": 2097152
  }
}
```

Ejemplo correcto para Data Protection:

```json
"Operations": {
  "DataProtectionKeysPath": "/home/digitalcards/.digitalcards/data-protection-keys"
}
```

Ejemplo correcto para Google Wallet:

```json
"GoogleWallet": {
  "CredentialsFilePath": "/home/digitalcards/.digitalcards/google-wallet-service-account.json"
}
```

Ejemplo correcto para Apple Wallet:

```json
"AppleWallet": {
  "CertificatePath": "/home/digitalcards/.digitalcards/apple-wallet/certs/digitalcards-pass-type.p12",
  "WwdrCertificatePath": "/home/digitalcards/.digitalcards/apple-wallet/certs/AppleWWDRCAG4.cer",
  "AssetsPath": "/home/digitalcards/.digitalcards/apple-wallet/assets"
}
```

---

## 8. Servicio `systemd`

Archivo:

```text
/etc/systemd/system/digitalcards.service
```

Contenido recomendado:

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

### Comandos básicos del servicio

Ver estado:

```bash
sudo systemctl status digitalcards
```

Iniciar:

```bash
sudo systemctl start digitalcards
```

Detener:

```bash
sudo systemctl stop digitalcards
```

Reiniciar:

```bash
sudo systemctl restart digitalcards
```

Habilitar arranque automático:

```bash
sudo systemctl enable digitalcards
```

Validar si está habilitado:

```bash
sudo systemctl is-enabled digitalcards
```

Ver logs:

```bash
journalctl -u digitalcards -n 100 --no-pager
```

Ver logs en vivo:

```bash
journalctl -u digitalcards -f
```

---

## 9. Apache como reverse proxy

Archivo:

```text
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

### Módulos requeridos

```bash
sudo a2enmod proxy
sudo a2enmod proxy_http
sudo a2enmod headers
sudo a2enmod rewrite
sudo systemctl restart apache2
```

### Activar sitio

```bash
sudo a2ensite digitalcards.conf
sudo apachectl configtest
sudo systemctl reload apache2
```

### Quitar advertencia de ServerName

Si Apache muestra:

```text
Could not reliably determine the server's fully qualified domain name
```

Ejecutar:

```bash
echo "ServerName ubuntu-server" | sudo tee /etc/apache2/conf-available/servername.conf
sudo a2enconf servername
sudo apachectl configtest
sudo systemctl reload apache2
```

### Probar Apache

```bash
curl -I http://127.0.0.1
```

---

## 10. Cloudflare Tunnel

Archivo de configuración:

```text
/etc/cloudflared/config.yml
```

Configuración esperada:

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

### Comandos de servicio

```bash
sudo systemctl status cloudflared
sudo systemctl restart cloudflared
sudo systemctl is-enabled cloudflared
```

### Logs

```bash
journalctl -u cloudflared -n 100 --no-pager
journalctl -u cloudflared -f
```

### Error común: origin service connection refused

Si el log muestra:

```text
Unable to reach the origin service
dial tcp 127.0.0.1:5031: connect: connection refused
```

Significa que Cloudflared está apuntando al puerto incorrecto o la app no está corriendo.

En producción debe apuntar a Apache:

```yaml
service: http://localhost:80
```

No debe depender de `5031`, porque `5031` era solo para `dotnet run`.

---

## 11. Base de datos HostGator

Cadena detectada:

```text
Server=162.241.2.108;Port=3306;Database=alltrac1_dcards;User ID=alltrac1_dcard_admin;Password=***;CharSet=utf8mb4;SslMode=Preferred;
```

### Validar puerto MySQL desde Ubuntu

```bash
timeout 8 bash -c '</dev/tcp/162.241.2.108/3306' \
  && echo "PUERTO 3306 ABIERTO" \
  || echo "NO CONECTA AL PUERTO 3306"
```

### Probar login manual

```bash
mariadb \
  -h 162.241.2.108 \
  -P 3306 \
  -u alltrac1_dcard_admin \
  -p \
  alltrac1_dcards
```

Dentro de MariaDB:

```sql
SHOW TABLES;
exit;
```

### HostGator Remote MySQL

La IP pública del servidor Ubuntu fue detectada como:

```text
200.56.111.235
```

Esa IP debe estar autorizada en HostGator / cPanel / Remote MySQL.

---

## 12. Google Wallet y Apple Wallet

### Error común

```text
The configured Google Wallet credentials file was not found.
```

Significa que el servicio `digitalcards`, corriendo con usuario `digitalcards`, no encuentra el archivo de credenciales configurado.

### Revisar ruta configurada

```bash
sudo grep -RIn "CredentialsFilePath\|google-wallet\|GoogleWallet" /home/digitalcards/.digitalcards/appsettings.Local.json
```

### Buscar archivos Google

```bash
sudo find /home/digitalcards/.digitalcards -iname "*google*" -o -iname "*.json"
```

### Copiar credencial desde `dang` a `digitalcards`

```bash
sudo cp /home/dang/.digitalcards/google-wallet-service-account.json /home/digitalcards/.digitalcards/
sudo chown digitalcards:digitalcards /home/digitalcards/.digitalcards/google-wallet-service-account.json
sudo chmod 600 /home/digitalcards/.digitalcards/google-wallet-service-account.json
```

### Reiniciar servicio

```bash
sudo systemctl restart digitalcards
journalctl -u digitalcards -n 100 --no-pager
```

---

## 13. Validaciones rápidas

### Estado general

```bash
sudo systemctl status digitalcards
sudo systemctl status apache2
sudo systemctl status cloudflared
```

### Servicios habilitados al arranque

```bash
sudo systemctl is-enabled digitalcards
sudo systemctl is-enabled apache2
sudo systemctl is-enabled cloudflared
```

### Puertos activos

```bash
sudo ss -tulpn | grep -E ':80|:5000|:5031|:3306'
```

### App directa

```bash
curl -I http://127.0.0.1:5000
```

### Apache hacia app

```bash
curl -I http://127.0.0.1
```

### Dominio público

```text
https://puntelio.com
https://www.puntelio.com
https://app.puntelio.com
```

---

## 14. Cómo subir actualizaciones desde GitHub

Este es el flujo normal cada vez que haya cambios nuevos en GitHub.

### 1. Entrar al repo

```bash
cd ~/src/DigitalCardsAppProject
```

### 2. Ver estado local

```bash
git status
```

Si no tienes cambios locales, debe salir algo parecido a:

```text
nothing to commit, working tree clean
```

### 3. Descargar cambios

```bash
git pull
```

### 4. Limpiar rutas problemáticas y builds previos

```bash
find . \( -name '*\\*' -o -name '*:*' \) -exec rm -rf {} +
find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
```

### 5. Publicar en carpeta temporal

```bash
rm -rf /tmp/digitalcards-publish

dotnet publish src/DigitalCards.Web/DigitalCards.Web.csproj \
  -c Release \
  -o /tmp/digitalcards-publish
```

### 6. Detener servicio

```bash
sudo systemctl stop digitalcards
```

### 7. Copiar publicación a producción

```bash
sudo rsync -av --delete /tmp/digitalcards-publish/ /var/www/digitalcards/publish/
```

Si `rsync` no está instalado:

```bash
sudo apt install -y rsync
```

### 8. Corregir permisos

```bash
sudo chown -R digitalcards:digitalcards /var/www/digitalcards
sudo chown -R digitalcards:digitalcards /var/log/digitalcards
```

### 9. Reiniciar servicio

```bash
sudo systemctl start digitalcards
```

O también:

```bash
sudo systemctl restart digitalcards
```

### 10. Validar

```bash
sudo systemctl status digitalcards
curl -I http://127.0.0.1:5000
curl -I http://127.0.0.1
journalctl -u digitalcards -n 100 --no-pager
```

---

## 15. Script sugerido para actualizar desde GitHub

Puedes crear este script para automatizar el despliegue.

Archivo:

```text
/home/dang/deploy-digitalcards.sh
```

Crear:

```bash
nano /home/dang/deploy-digitalcards.sh
```

Contenido:

```bash
#!/usr/bin/env bash
set -euo pipefail

APP_NAME="digitalcards"
REPO_DIR="/home/dang/src/DigitalCardsAppProject"
PUBLISH_TMP="/tmp/digitalcards-publish"
PUBLISH_DIR="/var/www/digitalcards/publish"

echo "==> Entrando al repo"
cd "$REPO_DIR"

echo "==> Estado Git"
git status

echo "==> Descargando cambios"
git pull

echo "==> Limpiando rutas inválidas y compilaciones previas"
find . \( -name '*\\*' -o -name '*:*' \) -exec rm -rf {} +
find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +

echo "==> Publicando en carpeta temporal"
rm -rf "$PUBLISH_TMP"

dotnet publish src/DigitalCards.Web/DigitalCards.Web.csproj \
  -c Release \
  -o "$PUBLISH_TMP"

echo "==> Deteniendo servicio"
sudo systemctl stop "$APP_NAME" || true

echo "==> Copiando archivos a producción"
sudo rsync -av --delete "$PUBLISH_TMP"/ "$PUBLISH_DIR"/

echo "==> Ajustando permisos"
sudo chown -R digitalcards:digitalcards /var/www/digitalcards
sudo chown -R digitalcards:digitalcards /var/log/digitalcards

echo "==> Iniciando servicio"
sudo systemctl start "$APP_NAME"

echo "==> Estado final"
sudo systemctl status "$APP_NAME" --no-pager

echo "==> Prueba local"
curl -I http://127.0.0.1:5000 || true
curl -I http://127.0.0.1 || true

echo "==> Deploy terminado"
```

Dar permisos:

```bash
chmod +x /home/dang/deploy-digitalcards.sh
```

Uso:

```bash
/home/dang/deploy-digitalcards.sh
```

---

## 16. Respaldos recomendados

Debes respaldar al menos:

```text
/home/digitalcards/.digitalcards
/home/dang/.digitalcards
/var/www/digitalcards/shared
/etc/cloudflared
/etc/apache2/sites-available/digitalcards.conf
/etc/systemd/system/digitalcards.service
```

Si la base de datos sigue en HostGator, hacer respaldo desde phpMyAdmin o por `mysqldump` remoto.

---

## 17. Backup manual del servidor

Crear carpeta de backups:

```bash
sudo mkdir -p /backups/digitalcards
sudo chown -R dang:dang /backups/digitalcards
```

Backup de archivos sensibles y configuración:

```bash
tar -czf /backups/digitalcards/digitalcards_files_$(date +%F_%H%M).tar.gz \
  /home/digitalcards/.digitalcards \
  /home/dang/.digitalcards \
  /var/www/digitalcards/shared \
  /etc/cloudflared \
  /etc/apache2/sites-available/digitalcards.conf \
  /etc/systemd/system/digitalcards.service
```

---

## 18. Backup de base de datos HostGator desde Ubuntu

Si HostGator permite conexión remota:

```bash
mysqldump \
  -h 162.241.2.108 \
  -P 3306 \
  -u alltrac1_dcard_admin \
  -p \
  alltrac1_dcards \
  > /backups/digitalcards/alltrac1_dcards_$(date +%F_%H%M).sql
```

Comprimir:

```bash
gzip /backups/digitalcards/alltrac1_dcards_*.sql
```

---

## 19. Script sugerido de backup

Archivo:

```text
/home/dang/backup-digitalcards.sh
```

Crear:

```bash
nano /home/dang/backup-digitalcards.sh
```

Contenido:

```bash
#!/usr/bin/env bash
set -euo pipefail

BACKUP_DIR="/backups/digitalcards"
STAMP="$(date +%F_%H%M)"

mkdir -p "$BACKUP_DIR"

echo "==> Backup de archivos"
tar -czf "$BACKUP_DIR/digitalcards_files_$STAMP.tar.gz" \
  /home/digitalcards/.digitalcards \
  /home/dang/.digitalcards \
  /var/www/digitalcards/shared \
  /etc/cloudflared \
  /etc/apache2/sites-available/digitalcards.conf \
  /etc/systemd/system/digitalcards.service

echo "==> Backup de base de datos HostGator"
mysqldump \
  -h 162.241.2.108 \
  -P 3306 \
  -u alltrac1_dcard_admin \
  -p \
  alltrac1_dcards \
  > "$BACKUP_DIR/alltrac1_dcards_$STAMP.sql"

gzip "$BACKUP_DIR/alltrac1_dcards_$STAMP.sql"

echo "==> Backups generados en $BACKUP_DIR"
ls -lh "$BACKUP_DIR"
```

Dar permisos:

```bash
chmod +x /home/dang/backup-digitalcards.sh
```

Ejecutar:

```bash
/home/dang/backup-digitalcards.sh
```

---

## 20. Restauración básica

### Restaurar archivos

```bash
sudo tar -xzf /backups/digitalcards/digitalcards_files_FECHA.tar.gz -C /
```

Después corregir permisos:

```bash
sudo chown -R digitalcards:digitalcards /home/digitalcards
sudo chown -R digitalcards:digitalcards /var/www/digitalcards
sudo chown -R root:root /etc/cloudflared
sudo chmod 600 /etc/cloudflared/config.yml
sudo chmod 600 /etc/cloudflared/*.json
```

### Restaurar base de datos

Si el respaldo está comprimido:

```bash
gunzip /backups/digitalcards/alltrac1_dcards_FECHA.sql.gz
```

Restaurar:

```bash
mysql \
  -h 162.241.2.108 \
  -P 3306 \
  -u alltrac1_dcard_admin \
  -p \
  alltrac1_dcards \
  < /backups/digitalcards/alltrac1_dcards_FECHA.sql
```

---

## 21. Checklist después de reiniciar servidor

Después de un `sudo reboot`, validar:

```bash
sudo systemctl status digitalcards
sudo systemctl status apache2
sudo systemctl status cloudflared
```

Validar puertos:

```bash
sudo ss -tulpn | grep -E ':80|:5000'
```

Validar HTTP local:

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

## 22. Problemas comunes y solución rápida

### App no arranca

```bash
sudo systemctl status digitalcards
journalctl -u digitalcards -n 100 --no-pager
```

### Apache no responde

```bash
sudo apachectl configtest
sudo systemctl status apache2
journalctl -u apache2 -n 100 --no-pager
```

### Cloudflare muestra error 502 / origin unreachable

```bash
sudo systemctl status cloudflared
journalctl -u cloudflared -n 100 --no-pager
curl -I http://127.0.0.1
```

### Error `Resource file "**/*.resx" cannot be found`

Ejecutar:

```bash
cd ~/src/DigitalCardsAppProject
find . \( -name '*\\*' -o -name '*:*' \) -print
find . \( -name '*\\*' -o -name '*:*' \) -exec rm -rf {} +
find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
```

Después revisar que no haya rutas Windows en `appsettings.Local.json`.

### Error de Google Wallet credentials

Revisar:

```bash
sudo grep -RIn "CredentialsFilePath\|google-wallet\|GoogleWallet" /home/digitalcards/.digitalcards/appsettings.Local.json
sudo find /home/digitalcards/.digitalcards -iname "*google*" -o -iname "*.json"
```

### Error con HostGator MySQL

Probar puerto:

```bash
timeout 8 bash -c '</dev/tcp/162.241.2.108/3306' \
  && echo "PUERTO 3306 ABIERTO" \
  || echo "NO CONECTA AL PUERTO 3306"
```

Probar login:

```bash
mariadb -h 162.241.2.108 -P 3306 -u alltrac1_dcard_admin -p alltrac1_dcards
```

---

## 23. Comandos rápidos de uso diario

### Reiniciar app

```bash
sudo systemctl restart digitalcards
```

### Ver logs app

```bash
journalctl -u digitalcards -n 100 --no-pager
```

### Ver logs en vivo

```bash
journalctl -u digitalcards -f
```

### Actualizar desde GitHub

```bash
/home/dang/deploy-digitalcards.sh
```

### Hacer backup

```bash
/home/dang/backup-digitalcards.sh
```

### Reiniciar Cloudflared

```bash
sudo systemctl restart cloudflared
```

### Reiniciar Apache

```bash
sudo systemctl reload apache2
```

---

## 24. Notas importantes

- No usar `dotnet run` para producción.
- `dotnet run --launch-profile http` usa normalmente `localhost:5031` y entorno `Development`.
- Producción debe usar `digitalcards.service` en `127.0.0.1:5000`.
- Cloudflared debe apuntar a Apache en `localhost:80`.
- Apache debe apuntar a Kestrel en `127.0.0.1:5000`.
- El usuario del servicio es `digitalcards`; por eso los secretos deben existir en `/home/digitalcards/.digitalcards`.
- Si cambias `appsettings.Local.json` del servicio, reinicia con `sudo systemctl restart digitalcards`.
- Si cambias Apache, recarga con `sudo systemctl reload apache2`.
- Si cambias Cloudflared, reinicia con `sudo systemctl restart cloudflared`.

---

## 25. Resumen final del flujo operativo

### Para actualizar código

```bash
/home/dang/deploy-digitalcards.sh
```

### Para respaldar

```bash
/home/dang/backup-digitalcards.sh
```

### Para reiniciar app

```bash
sudo systemctl restart digitalcards
```

### Para revisar estado general

```bash
sudo systemctl status digitalcards
sudo systemctl status apache2
sudo systemctl status cloudflared
```

### Para revisar errores

```bash
journalctl -u digitalcards -n 100 --no-pager
journalctl -u cloudflared -n 100 --no-pager
journalctl -u apache2 -n 100 --no-pager
```
