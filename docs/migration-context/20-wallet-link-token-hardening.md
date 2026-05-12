# 20 Wallet Link Token Hardening

## Objetivo

Esta fase deja de usar `CardID` directo como token publico en correos y rutas Wallet. Los links nuevos usan tokens opacos de 256 bits y la base solo guarda `SHA-256`, un sufijo corto para soporte y metadata operativa.

## Cambios

- Nueva tabla `WalletLinkToken` en HostGator.
- Nuevo repositorio `IWalletLinkTokenRepository` con implementaciones InMemory/MySQL.
- Nuevo servicio `IWalletLinkTokenService` para crear, resolver y revocar tokens.
- `EnrollClientAsync` y `ResendWalletEmailAsync` generan links `/Wallet/Select/{opaqueToken}`.
- `/Wallet/Select`, `/Wallet/Apple`, `/Wallet/Apple/Download` y `/Wallet/Google` resuelven tokens opacos.
- Apple Wallet conserva `serialNumber` estable basado en `ClientCard.CardID`; esto evita romper passes ya instalados.

## Compatibilidad

`DigitalCards:WalletLinks:AllowLegacyCardIdTokens` queda en `true` por default en este PR. Si un token opaco no existe, la app intenta resolver el token legacy por `CardID`.

Para endurecer despues:

```json
{
  "DigitalCards": {
    "WalletLinks": {
      "AllowLegacyCardIdTokens": false
    }
  }
}
```

Al apagar compatibilidad, links viejos por `CardID` dejan de funcionar y se debe reenviar link desde `/Business/Cards`.

## Nota Operativa

Como el token plano no se guarda, no se puede reconstruir el mismo token para reenviarlo. Cada correo nuevo crea un token opaco nuevo. Los tokens no expiran automaticamente; quedan activos hasta revocacion futura.

## HostGator

Antes del smoke real, ejecutar:

```text
docs/migration-context/20-wallet-link-token-hardening-hostgator.sql
```

No correr este SQL sin backup. No modifica tablas legacy ni filas existentes.

## Smoke Real

1. Aplicar SQL en HostGator.
2. Confirmar `DigitalCards:WalletLinks:AllowLegacyCardIdTokens=true`.
3. Login negocio allowlisted.
4. Reenviar link desde `/Business/Cards`.
5. Confirmar que el correo contiene `https://app.puntelio.com/Wallet/Select/{opaqueToken}`.
6. Abrir link en iPhone/Android.
7. Instalar Apple Wallet y guardar Google Wallet.
8. Agregar sello desde `/Business/Cards`.
9. Confirmar update Apple/Google.

## Pruebas

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
