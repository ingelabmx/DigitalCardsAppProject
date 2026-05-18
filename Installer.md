# Installer

Guia de instalacion y migracion de `DigitalCardsAppProject` a Ubuntu Server.

Proyecto detectado:
- ASP.NET Core `net8.0`
- App web en `src/DigitalCards.Web`
- Landing `puntelio.com`
- App `app.puntelio.com`
- Cloudflare Tunnel con `cloudflared`
- MySQL
- Archivos locales para uploads, certificados y Data Protection keys

## 1. Objetivo

Dejar servidor Ubuntu listo para:
- bajar actualizaciones desde GitHub
- correr app ASP.NET Core en produccion
- conectar dominio con Cloudflare Tunnel
- usar MySQL local o remoto
- persistir uploads
- persistir Data Protection keys
- mantener app como servicio `systemd`

## 2. Recomendacion base

Usa:
- Ubuntu Server 24.04 LTS
- usuario normal con `sudo`
- despliegue en `/var/www/digitalcards`
- secretos fuera del repo
- `systemd` para app
- `cloudflared` como servicio
- `nginx` como reverse proxy local

## 3. Paquetes a instalar

Instala base:

```bash
sudo apt update
sudo apt install -y git openssh-server curl wget unzip nginx ufw fail2ban
```

Utilidad de revision:

```bash
sudo apt install -y jq tree
```

## 4. GitHub

Necesitas:
- `git`
- llave SSH
- acceso repo

Genera llave SSH:

```bash
ssh-keygen -t ed25519 -C "ubuntu-server-digitalcards"
```

Carga agente SSH:

```bash
eval "$(ssh-agent -s)"
ssh-add ~/.ssh/id_ed25519
```

Muestra llave publica:

```bash
cat ~/.ssh/id_ed25519.pub
```

Agrega esa llave a GitHub.

Prueba conexion:

```bash
ssh -T git@github.com
```

Clona repo:

```bash
mkdir -p ~/src
cd ~/src
git clone git@github.com:TU_ORG_O_USUARIO/TU_REPO.git DigitalCardsAppProject
```

Si ya existe repo:

```bash
cd ~/src/DigitalCardsAppProject
git pull
```

Opcional `gh` CLI:
- util para PRs, releases, auth CLI
- no obligatorio para correr app

## 5. .NET 8

Proyecto usa `net8.0`. Instala .NET 8.

Si solo correr app:

```bash
sudo apt update
sudo apt install -y aspnetcore-runtime-8.0
```

Si tambien compilar en server:

```bash
sudo apt update
sudo apt install -y dotnet-sdk-8.0
```

Valida:

```bash
dotnet --info
dotnet --list-runtimes
```

## 6. MySQL

Decision:
- si seguiras usando DB remota actual -> no instales MySQL local
- si moveras DB al server -> instala MySQL

Instalacion local:

```bash
sudo apt update
sudo apt install -y mysql-server
```

Asegura instalacion:

```bash
sudo mysql_secure_installation
```

Crea DB y usuario ejemplo:

```sql
CREATE DATABASE digitalcards CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'digitalcards_user'@'localhost' IDENTIFIED BY 'CAMBIA_PASSWORD';
GRANT ALL PRIVILEGES ON digitalcards.* TO 'digitalcards_user'@'localhost';
FLUSH PRIVILEGES;
```

Backup desde equipo viejo:

```bash
mysqldump -u USER -p --databases NOMBRE_DB > digitalcards.sql
```

Restore en Ubuntu:

```bash
mysql -u root -p < digitalcards.sql
```

## 7. Estructura recomendada

Rutas sugeridas:

```text
/var/www/digitalcards/app
/var/www/digitalcards/publish
/var/www/digitalcards/shared/uploads/business-logos
/var/www/digitalcards/shared/data-protection-keys
/var/www/digitalcards/shared/secrets
/var/log/digitalcards
```

Crea directorios:

```bash
sudo mkdir -p /var/www/digitalcards/app
sudo mkdir -p /var/www/digitalcards/publish
sudo mkdir -p /var/www/digitalcards/shared/uploads/business-logos
sudo mkdir -p /var/www/digitalcards/shared/data-protection-keys
sudo mkdir -p /var/www/digitalcards/shared/secrets
sudo mkdir -p /var/log/digitalcards
```

Asigna dueño:

```bash
sudo chown -R $USER:$USER /var/www/digitalcards
sudo chown -R $USER:$USER /var/log/digitalcards
```

## 8. Secretos y archivos que debes mover

Debes copiar desde PC actual:
- `C:\Users\eguillen\.digitalcards\appsettings.Local.json`
- credenciales Google Wallet
- certificados Apple Wallet
- assets Apple Wallet si aplican
- uploads de logos
- Data Protection keys
- archivos de `cloudflared`

No subas secretos al repo.

Ejemplo de destinos Linux:

```text
/var/www/digitalcards/shared/secrets/appsettings.Local.json
/var/www/digitalcards/shared/secrets/google-wallet-service-account.json
/var/www/digitalcards/shared/secrets/apple-wallet/certs/...
/var/www/digitalcards/shared/uploads/business-logos
/var/www/digitalcards/shared/data-protection-keys
~/.cloudflared/config.yml
~/.cloudflared/<tunnel-id>.json
```

## 9. Configuracion local de app

Tu app carga:
- `appsettings.json`
- `appsettings.Local.json`
- `appsettings.{Environment}.Local.json`
- `~/.digitalcards/appsettings.Local.json`
- variables de entorno

Para Ubuntu recomiendo usar archivo de usuario:

```text
/home/TU_USUARIO/.digitalcards/appsettings.Local.json
```

Crea carpeta:

```bash
mkdir -p ~/.digitalcards
chmod 700 ~/.digitalcards
```

Mueve archivo:

```bash
cp /ruta/origen/appsettings.Local.json ~/.digitalcards/appsettings.Local.json
chmod 600 ~/.digitalcards/appsettings.Local.json
```

### Ajustes importantes dentro de `appsettings.Local.json`

Revisa rutas Windows -> Linux.

Ejemplo:

```json
{
  "ConnectionStrings": {
    "DigitalCards": "Server=127.0.0.1;Port=3306;Database=digitalcards;User ID=digitalcards_user;Password=CAMBIA_PASSWORD;CharSet=utf8mb4;SslMode=Preferred;"
  },
  "DigitalCards": {
    "PersistenceProvider": "MySql",
    "PublicBaseUrl": "https://app.puntelio.com",
    "GoogleWallet": {
      "Provider": "Google",
      "IssuerId": "TU_ISSUER_ID",
      "Origins": [
        "https://app.puntelio.com"
      ],
      "CredentialsFilePath": "/var/www/digitalcards/shared/secrets/google-wallet-service-account.json"
    },
    "AppleWallet": {
      "Provider": "Apple",
      "AuthenticationTokenSecret": "TU_SECRET",
      "ApnsBaseUrl": "https://api.push.apple.com",
      "TeamIdentifier": "TU_TEAM_ID",
      "PassTypeIdentifier": "TU_PASS_TYPE_ID",
      "OrganizationName": "Puntelio",
      "CertificatePath": "/var/www/digitalcards/shared/secrets/apple-wallet/certs/digitalcards-pass-type.p12",
      "CertificatePassword": "TU_PASSWORD",
      "WwdrCertificatePath": "/var/www/digitalcards/shared/secrets/apple-wallet/certs/AppleWWDRCAG4.cer",
      "AssetsPath": "/var/www/digitalcards/shared/secrets/apple-wallet/assets"
    },
    "Email": {
      "Provider": "Smtp",
      "FromName": "Puntelio",
      "FromAddress": "tu-correo@dominio.com",
      "Host": "smtp.gmail.com",
      "Port": 587,
      "SecureSocket": "StartTls",
      "UserName": "tu-correo@dominio.com",
      "Password": "TU_PASSWORD_SMTP"
    },
    "Branding": {
      "LogoUploads": {
        "Path": "/var/www/digitalcards/shared/uploads/business-logos",
        "RequestPath": "/uploads/business-logos",
        "MaxBytes": 2097152
      }
    },
    "Operations": {
      "EnableForwardedHeaders": true,
      "TrustAllForwardedHeaders": false,
      "KnownProxies": [],
      "DataProtectionKeysPath": "/var/www/digitalcards/shared/data-protection-keys",
      "RequireDataProtectionKeysForReadiness": true
    }
  },
  "Puntelio": {
    "Landing": {
      "SiteUrl": "https://puntelio.com",
      "AppUrl": "https://app.puntelio.com",
      "ContactEmail": "ingelabmx@gmail.com",
      "WhatsAppUrl": "https://wa.me/526641972204",
      "PhoneDisplay": "664 197 2204",
      "InstagramUrl": "https://instagram.com/puntelio",
      "TikTokUrl": "https://www.tiktok.com/@puntelio"
    }
  },
  "AllowedHosts": "*"
}
```

## 10. Build y publish

Desde repo:

```bash
cd ~/src/DigitalCardsAppProject
dotnet restore
dotnet build
dotnet publish src/DigitalCards.Web/DigitalCards.Web.csproj -c Release -o /var/www/digitalcards/publish
```

Prueba manual:

```bash
cd /var/www/digitalcards/publish
ASPNETCORE_ENVIRONMENT=Production dotnet DigitalCards.Web.dll
```

Si app corre, detenla con `Ctrl+C`.

## 11. Servicio `systemd` para app

Crea usuario servicio opcional:

```bash
sudo useradd --system --shell /usr/sbin/nologin --home /var/www/digitalcards digitalcards
```

Da permisos:

```bash
sudo chown -R digitalcards:digitalcards /var/www/digitalcards
sudo chown -R digitalcards:digitalcards /var/log/digitalcards
sudo mkdir -p /home/digitalcards/.digitalcards
sudo cp ~/.digitalcards/appsettings.Local.json /home/digitalcards/.digitalcards/appsettings.Local.json
sudo chown -R digitalcards:digitalcards /home/digitalcards/.digitalcards
sudo chmod 700 /home/digitalcards/.digitalcards
sudo chmod 600 /home/digitalcards/.digitalcards/appsettings.Local.json
```

Crea archivo:

```bash
sudo nano /etc/systemd/system/digitalcards.service
```

Contenido:

```ini
[Unit]
Description=DigitalCards Web
After=network.target

[Service]
WorkingDirectory=/var/www/digitalcards/publish
ExecStart=/usr/bin/dotnet /var/www/digitalcards/publish/DigitalCards.Web.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=digitalcards
User=digitalcards
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=HOME=/home/digitalcards
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

Activa servicio:

```bash
sudo systemctl daemon-reload
sudo systemctl enable digitalcards
sudo systemctl start digitalcards
sudo systemctl status digitalcards
```

Logs:

```bash
journalctl -u digitalcards -f
```

## 12. Nginx

Aunque uses Cloudflare Tunnel, Nginx ayuda como reverse proxy local.

Instala:

```bash
sudo apt install -y nginx
```

Crea config:

```bash
sudo nano /etc/nginx/sites-available/digitalcards
```

Contenido:

```nginx
server {
    listen 80;
    server_name puntelio.com www.puntelio.com app.puntelio.com;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   X-Forwarded-Host $host;
    }
}
```

Nota:
- si app escuchara en `5000`, define URL en servicio o por env vars
- si prefieres Kestrel default distinto, ajusta `proxy_pass`

Agrega URL Kestrel al servicio:

```ini
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
```

Activa sitio:

```bash
sudo ln -s /etc/nginx/sites-available/digitalcards /etc/nginx/sites-enabled/digitalcards
sudo nginx -t
sudo systemctl restart nginx
```

## 13. Cloudflare Tunnel

Instala `cloudflared`. Luego copia:
- `config.yml`
- archivo credencial del tunnel

Config sugerida:

```yaml
tunnel: TU_TUNNEL_ID
credentials-file: /home/TU_USUARIO/.cloudflared/TU_TUNNEL_ID.json

ingress:
  - hostname: puntelio.com
    service: http://localhost:80
  - hostname: app.puntelio.com
    service: http://localhost:80
  - service: http_status:404
```

Si saltas Nginx, apunta directo a Kestrel:

```yaml
ingress:
  - hostname: puntelio.com
    service: http://localhost:5000
  - hostname: app.puntelio.com
    service: http://localhost:5000
  - service: http_status:404
```

Prueba manual:

```bash
cloudflared tunnel run TU_TUNNEL_ID
```

Instala servicio:

```bash
sudo cloudflared --config /home/TU_USUARIO/.cloudflared/config.yml service install
sudo systemctl enable cloudflared
sudo systemctl start cloudflared
sudo systemctl status cloudflared
```

Logs:

```bash
journalctl -u cloudflared -f
```

## 14. Firewall

Con Nginx:

```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
sudo ufw status
```

Si solo usaras Cloudflare Tunnel y no trafico publico directo:
- puedes dejar solo SSH abierto

## 15. Actualizar app despues

Flujo simple:

```bash
cd ~/src/DigitalCardsAppProject
git pull
dotnet publish src/DigitalCards.Web/DigitalCards.Web.csproj -c Release -o /var/www/digitalcards/publish
sudo systemctl restart digitalcards
sudo systemctl status digitalcards
```

## 16. Backup

Debes respaldar:
- base MySQL
- `/var/www/digitalcards/shared/uploads/business-logos`
- `/var/www/digitalcards/shared/data-protection-keys`
- `/home/digitalcards/.digitalcards/appsettings.Local.json`
- certificados Apple
- credenciales Google
- `~/.cloudflared`

Ejemplo backup DB:

```bash
mysqldump -u root -p --databases digitalcards > /backups/digitalcards_$(date +%F).sql
```

## 17. Checklist final

Verifica:
- `dotnet --info` funciona
- repo clonado
- `appsettings.Local.json` en Linux con rutas Linux
- publish generado en `/var/www/digitalcards/publish`
- `digitalcards.service` activo
- `nginx` activo
- `cloudflared` activo
- `puntelio.com` responde
- `app.puntelio.com` responde
- login negocio responde
- login admin responde
- uploads funcionan
- correo funciona
- Google Wallet funciona
- Apple Wallet funciona
- DB conecta

## 18. Comandos de diagnostico

App:

```bash
sudo systemctl status digitalcards
journalctl -u digitalcards -n 200 --no-pager
```

Tunnel:

```bash
sudo systemctl status cloudflared
journalctl -u cloudflared -n 200 --no-pager
```

Nginx:

```bash
sudo nginx -t
sudo systemctl status nginx
```

Puertos:

```bash
sudo ss -tulpn
```

HTTP local:

```bash
curl -I http://127.0.0.1:5000
curl -I http://127.0.0.1
```

## 19. Riesgos comunes

- rutas Windows dejadas en config Linux
- `Puntelio` mal anidado dentro de `DigitalCards`
- `AllowedHosts` mal anidado
- `cloudflared` sin ingress para `puntelio.com`
- no copiar uploads
- no copiar Data Protection keys -> sesiones invalidas
- no copiar certs Apple Wallet
- `PublicBaseUrl` incorrecto
- permisos malos en secretos
- app corriendo como root

## 20. Fuentes oficiales

- .NET Ubuntu: https://learn.microsoft.com/dotnet/core/install/linux-ubuntu
- ASP.NET Core en Linux con Nginx: https://learn.microsoft.com/aspnet/core/host-and-deploy/linux-nginx
- Cloudflare Tunnel service Linux: https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/do-more-with-tunnels/local-management/as-a-service/linux/
- GitHub SSH: https://docs.github.com/authentication/connecting-to-github-with-ssh
- MySQL APT repo: https://dev.mysql.com/doc/mysql-apt-repo-quick-guide/en/

