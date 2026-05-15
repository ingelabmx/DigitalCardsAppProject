# PR 118: Reward Redemption Cycles v1

## Summary

Las tarjetas completas ya no se reinician automaticamente al escanear o agregar sello. El negocio debe confirmar el canje de recompensa. Al confirmar, se conserva el mismo cliente, la misma tarjeta y los mismos links Wallet, pero el ciclo activo vuelve a 0 sellos.

## Database

Aplicar manualmente en HostGator:

```text
docs/migration-context/118-reward-redemption-cycles-hostgator.sql
```

La tabla nueva `RewardRedemption` guarda historial operativo de ciclos canjeados por tarjeta, negocio y cliente.

## Behavior

- `AddStamp` incrementa hasta la meta configurada del negocio.
- Si la tarjeta ya esta completa, `AddStamp` no reinicia ni agrega sellos.
- `/Business/Cards` muestra `Canjear recompensa` cuando la tarjeta esta completa.
- `/Business/Stamp` muestra el flujo de canje cuando el QR o usuario pertenece a una tarjeta completa.
- El canje registra `RewardRedemption` y `StampLedger` con `Source=RewardRedeemed`.
- Apple/Google Wallet se actualizan con la tarjeta en `0 de meta`.

## Rollout

1. Mergear el PR.
2. Aplicar el SQL manual.
3. Probar con un negocio controlado:
   - completar tarjeta hasta la meta;
   - confirmar que no se reinicia sola;
   - canjear recompensa;
   - confirmar `0 de meta` en UI y Wallet;
   - revisar historial de canjes en tarjeta y soporte.
