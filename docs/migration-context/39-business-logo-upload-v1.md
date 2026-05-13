# 39 Business Logo Upload v1

## Objetivo

Permitir que el admin suba el logo publico de un negocio desde
`/Admin/BusinessProfile/{businessId}` y usarlo en correo, UI moderna, Google
Wallet y Apple Wallet.

## Configuracion

Los archivos se guardan fuera del repo:

```json
{
  "DigitalCards": {
    "Branding": {
      "LogoUploads": {
        "Path": "C:\\Users\\eguillen\\.digitalcards\\uploads\\business-logos",
        "RequestPath": "/uploads/business-logos",
        "MaxBytes": 2097152
      }
    }
  }
}
```

Si `Path` no se configura, la app usa:

```text
%USERPROFILE%\.digitalcards\uploads\business-logos
```

La ruta publica queda bajo:

```text
https://app.puntelio.com/uploads/business-logos/...
```

## Comportamiento

- El admin puede subir `png`, `jpg`, `jpeg` o `webp`.
- El maximo default es 2 MB.
- La app valida extension y firma basica del archivo.
- No se acepta SVG en v1.
- El nombre final es aleatorio; no se usa el filename original.
- `ModernBusinessBranding.LogoPath` guarda una ruta relativa publica.
- `Business.BusinessLogo` legacy no se modifica.

## Wallets

Google Wallet usa el logo del negocio como URL HTTPS publica cuando existe:

```text
https://app.puntelio.com/uploads/business-logos/...
```

Si no hay logo publico, cae al `GoogleWallet:LogoImageUri` global.

Apple Wallet embebe `logo.png` y `logo@2x.png` dentro del `.pkpass` cuando el
logo subido es PNG. Para JPG/JPEG/WebP, el logo queda disponible para UI,
correo y Google Wallet; Apple Wallet cae a los assets base hasta tener un PR de
conversion/redimensionamiento real.

Los passes Apple ya instalados necesitan update APNs o reinstalacion para ver el
logo nuevo.

## Operacion

1. Entrar a `/Admin/Businesses`.
2. Abrir `Administrar`.
3. En `Branding Wallet`, seleccionar `Subir logo`.
4. Guardar branding.
5. Confirmar que el campo `Logo publico` queda como `/uploads/business-logos/...`.
6. Reenviar link Wallet o agregar sello para probar update.

## Validacion

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```

## SQL

No hay SQL nuevo. Requiere que ya exista `ModernBusinessBranding`:

```text
docs/migration-context/31-business-branding-v1-hostgator.sql
```

