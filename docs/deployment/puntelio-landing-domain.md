# Puntelio landing domain

## Dominios

- Landing publica: `https://puntelio.com`
- App principal: `https://app.puntelio.com`

El mismo deployment ASP.NET Core puede servir ambos dominios. La app detecta el host:

- `puntelio.com` muestra la landing, `/terminos`, `/privacidad`, `robots.txt` y `sitemap.xml`.
- rutas de app visitadas desde `puntelio.com` redirigen a `https://app.puntelio.com` preservando path y query.
- `app.puntelio.com` conserva la app actual.

## Configuracion appsettings

No se requieren variables locales para los dominios publicos. La configuracion base vive en `src/DigitalCards.Web/appsettings.json`:

```json
{
  "DigitalCards": {
    "PublicBaseUrl": "https://app.puntelio.com"
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
  }
}
```

El formulario de videollamada requiere SMTP real. En produccion, configurar estos valores en el appsettings usado por el hosting, sin subir secretos al repo:

```json
{
  "DigitalCards": {
    "Email": {
      "Provider": "Smtp",
      "FromName": "Puntelio",
      "FromAddress": "<correo-remitente>",
      "Host": "<smtp-host>",
      "Port": 587,
      "SecureSocket": "StartTls",
      "UserName": "<smtp-user>",
      "Password": "<smtp-password>"
    }
  }
}
```

## Cloudflare Tunnel

Usar el mismo tunnel para landing y app. Crear o reutilizar el tunnel `puntelio-app`:

```powershell
cloudflared tunnel login
cloudflared tunnel create puntelio-app
cloudflared tunnel route dns puntelio-app puntelio.com
cloudflared tunnel route dns puntelio-app www.puntelio.com
cloudflared tunnel route dns puntelio-app app.puntelio.com
```

Config sugerida de Cloudflare (`%USERPROFILE%\.cloudflared\config.yml`):

```yaml
tunnel: puntelio-app
credentials-file: C:\Users\eguillen\.cloudflared\<TUNNEL_ID>.json

ingress:
  - hostname: puntelio.com
    service: http://localhost:5031
  - hostname: www.puntelio.com
    service: http://localhost:5031
  - hostname: app.puntelio.com
    service: http://localhost:5031
  - service: http_status:404
```

Ejecutar:

```powershell
dotnet run --project src\DigitalCards.Web\DigitalCards.Web.csproj --launch-profile http
cloudflared tunnel run puntelio-app
```

En Cloudflare DNS deben quedar registros tipo `CNAME` administrados por el tunnel:

- `puntelio.com`
- `www.puntelio.com`
- `app.puntelio.com`

No usar registros `A` directos al IP publico si el acceso sera por tunnel.

SSL/TLS en Cloudflare:

- Modo recomendado: `Full`.
- Activar proxy naranja para los hostnames del tunnel.
- Confirmar que no exista Page Rule/Redirect Rule que mande `puntelio.com` a `app.puntelio.com`.

## DNS / hosting alterno

Si no se usa Cloudflare Tunnel, apuntar:

- `puntelio.com` al deployment web de Puntelio.
- `www.puntelio.com` al mismo deployment o redireccion a `puntelio.com`.
- `app.puntelio.com` al mismo deployment o al deployment de la app actual.

Verificar certificados TLS para los tres hosts.

## Prueba local

Probar la landing con Host header:

```powershell
curl.exe -H "Host: puntelio.com" http://localhost:5000/
```

Probar la app:

```powershell
curl.exe -H "Host: app.puntelio.com" http://localhost:5000/
```

Validar publico:

```powershell
curl.exe -I https://puntelio.com
curl.exe -I https://www.puntelio.com
curl.exe -I https://app.puntelio.com/health
curl.exe -I https://app.puntelio.com/health/ready
```
