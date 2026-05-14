# 101 Legacy Removal Phase 1 v1

Esta fase retira Legacy de la operacion visible sin borrar todavia la
compatibilidad tecnica con tablas HostGator existentes.

## Cambios

- Admin muestra `Activacion` en lugar de `Cutover` en navegacion y dashboard.
- `/Admin/Support` deja de mostrar estado operativo de `LegacyWalletSync`.
- `/Admin/Cutover` deja de mostrar estado del worker de sincronizacion.
- Los exports de soporte ya no incluyen el bloque de estado LegacyWalletSync ni
  columnas de sincronizacion legacy.
- Eventos de sellos detectados desde fuentes externas se muestran como
  `Operacion externa` en UI/export.

## Sin SQL Nuevo

No hay cambios de esquema. `LegacyWalletSync` sigue disponible tecnicamente, pero
permanece apagado por default y fuera de la UI operativa.

## Validacion

- La app moderna opera sin depender de `LegacyWalletSync` visible.
- Soporte y activacion no muestran texto LegacySync/LegacyWalletSync.
- Las pruebas siguen usando fakes y los smokes manuales siguen separados por
  flags `RUN_*`.
