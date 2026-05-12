# 18 Pilot Readiness

## Objetivo

Permitir pruebas reales en `https://app.puntelio.com` sin abrir las pantallas
modernas de negocio a todos los negocios/clientes. Web Forms sigue siendo el
fallback operativo.

## Configuracion

La configuracion vive solo fuera del repo:

```text
C:\Users\eguillen\.digitalcards\appsettings.Local.json
```

Activar piloto:

```json
{
  "DigitalCards": {
    "Pilot": {
      "Enabled": true,
      "AllowedBusinessIds": [],
      "AllowedBusinessEmails": ["NEGOCIO_TEST_EMAIL"],
      "AllowedClientEmails": ["CLIENTE_TEST_EMAIL"],
      "AllowedClientEmailDomains": ["example.test"]
    }
  }
}
```

Comportamiento:

- `Enabled=false`: no se bloquea ningun negocio por piloto.
- `Enabled=true`: `/Business/Dashboard`, `/Business/Enroll` y
  `/Business/Stamp` requieren que el negocio este allowlisted por ID o email.
- Enroll/stamp tambien validan cliente por email exacto o dominio.
- Wallet landing, Google Wallet, Apple `.pkpass` y Apple Wallet Web Service no
  dependen de cookie de negocio ni del piloto.

Si el piloto esta activo y no hay allowlist de clientes, los clientes existentes
quedan bloqueados para acciones modernas. Esto evita enrolar usuarios reales por
accidente.

## Diagnostico seguro

Activar solo durante troubleshooting controlado:

```json
{
  "DigitalCards": {
    "Diagnostics": {
      "EnableWalletDiagnostics": true
    }
  }
}
```

Endpoint:

```text
GET /internal/wallet-diagnostics/{CardID-or-enrollment-token}
```

Devuelve estado operativo: negocio, cliente, sellos, Google emitido, Apple
tracked, cantidad de dispositivos Apple registrados y ultimo update Apple.

No devuelve push tokens, authentication tokens, JWTs, passwords, certificados,
service account JSON ni connection strings. Los correos se muestran
parcialmente enmascarados.

## Smoke real

1. Confirmar tunnel y health:

   ```powershell
   Invoke-WebRequest https://app.puntelio.com/health -UseBasicParsing
   ```

2. Confirmar JSON local sin imprimir secretos:

   ```powershell
   Get-Content "$env:USERPROFILE\.digitalcards\appsettings.Local.json" -Raw | ConvertFrom-Json | Out-Null
   ```

3. Con piloto activo, login con negocio allowlisted.
4. Registrar/asociar cliente allowlisted.
5. Confirmar correo con link `https://app.puntelio.com/Wallet/Select/...`.
6. Instalar Apple Wallet en iPhone.
7. Guardar Google Wallet.
8. Agregar sello desde app moderna y validar update Apple/Google.
9. Si se prueba Web Forms, activar `LegacyWalletSync`, agregar sello legacy y
   confirmar patch Google + push Apple en logs.

Logs esperados:

```text
Apple Wallet update push accepted
Apple Wallet update check ... returned 1 updated passes
Apple Wallet updated pass request returned Ready
```

## Rollback

Para volver a operar solo con Web Forms:

```json
{
  "DigitalCards": {
    "Pilot": {
      "Enabled": false
    },
    "LegacyWalletSync": {
      "Enabled": false
    }
  }
}
```

Luego reiniciar la app moderna. Las tarjetas Wallet ya emitidas siguen
existiendo; apagar el piloto solo libera o bloquea pantallas modernas de
negocio segun configuracion, no borra datos.

## Validacion automatizada

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'; dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
