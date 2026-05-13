# 65 Security Rate Limits v1

## Objetivo

Reducir riesgo publico en `app.puntelio.com` antes de ampliar operacion real.

## Cambios

- Rate limiting nativo de ASP.NET Core por IP para:
  - login admin, negocio y cliente;
  - forgot/reset password;
  - `/Register`;
  - `/Enroll/{businessToken}`;
  - rutas publicas Wallet con limite mas permisivo.
- Headers seguros basicos:
  - `X-Frame-Options: DENY`;
  - `X-Content-Type-Options: nosniff`;
  - `Referrer-Policy: strict-origin-when-cross-origin`.
- No-cache para superficies admin, negocio, cliente y dev.

## Configuracion

Valores default:

```json
{
  "DigitalCards": {
    "Security": {
      "RateLimiting": {
        "AuthPermitLimit": 20,
        "AuthWindowSeconds": 60,
        "PublicWritePermitLimit": 30,
        "PublicWriteWindowSeconds": 60,
        "WalletPermitLimit": 300,
        "WalletWindowSeconds": 60
      }
    }
  }
}
```

## SQL

No requiere SQL.

## Validacion

- Rutas auth devuelven `429 Too Many Requests` cuando exceden el limite.
- Wallet landing sigue respondiendo con limite separado.
- Paginas auth tienen `Cache-Control: no-store`.
- No cambia autenticacion, Wallets ni Web Forms.
