# 58 Wallet Link Legacy Compat Off v1

Esta fase cambia el default de links Wallet a modo seguro: los links publicos
nuevos y la landing Wallet deben usar tokens opacos persistidos por hash, no el
`CardID`/token legacy visible.

## Cambios

- `DigitalCards:WalletLinks:AllowLegacyCardIdTokens` queda en `false` por
  default.
- El override `true` sigue disponible solo para una ventana temporal de soporte
  si hay links antiguos pendientes.
- Correos, reenvios y botones Apple/Google siguen usando el token publico
  recibido.
- Los Apple passes instalados no cambian de serial; este PR solo endurece los
  links publicos.

## Configuracion

Default real:

```json
{
  "DigitalCards": {
    "WalletLinks": {
      "AllowLegacyCardIdTokens": false
    }
  }
}
```

Override temporal de emergencia:

```json
{
  "DigitalCards": {
    "WalletLinks": {
      "AllowLegacyCardIdTokens": true
    }
  }
}
```

## Validacion

- Token opaco valido abre `/Wallet/Select/{token}`.
- `CardID`/token legacy directo devuelve link no valido con compatibilidad
  apagada.
- Compatibilidad encendida permite links antiguos durante una ventana de
  soporte controlada.

No hay SQL nuevo.
