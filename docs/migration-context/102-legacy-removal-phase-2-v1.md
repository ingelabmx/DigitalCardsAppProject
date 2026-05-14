# 102 Legacy Removal Phase 2 v1

Esta fase agrega guardrails para que la UI moderna no vuelva a mostrar lenguaje
operativo legacy. El codigo aun conserva compatibilidad interna con las tablas
HostGator actuales hasta una migracion de esquema posterior.

## Cambios

- Agrega prueba de smoke para asegurar que Admin, Business y Client no rendericen
  `LegacyWalletSync`, `LegacySync`, `Web Forms` ni `fallback` en paginas
  operativas.
- Documenta que los archivos historicos de `docs/migration-context` son
  referencia tecnica, no runbooks de operacion diaria.
- Mantiene rutas y tablas existentes para no romper Wallets reales ni datos de
  HostGator.

## Sin SQL Nuevo

No hay cambios de esquema.

## Criterio

La app moderna queda como flujo principal visible. Cualquier referencia legacy
restante debe ser historica, interna o necesaria para leer tablas existentes.
