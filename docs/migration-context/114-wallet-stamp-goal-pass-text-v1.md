# 114 Wallet Stamp Goal + Pass Text Updates v1

## Summary

Agrega `StampGoal` al branding moderno para que cada negocio defina su
numero de sellos por ciclo. Las tarjetas digitales muestran el avance como
`actual de objetivo` y el siguiente sello reinicia el ciclo cuando la tarjeta
ya esta completa.

## Rollout

Aplicar manualmente en HostGator:

```text
docs/migration-context/114-wallet-stamp-goal-hostgator.sql
```

Despues actualizar el numero de sellos desde `/Business/Branding` o
`/Admin/BusinessProfile` y usar `Guardar y actualizar` para propagar el texto
en Wallets emitidas.
