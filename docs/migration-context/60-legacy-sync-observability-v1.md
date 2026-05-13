# 60 Legacy Sync Observability v1

Esta fase agrega observabilidad operacional en memoria para
`LegacyWalletSync`, sin crear tablas nuevas.

## Cambios

- Nuevo estado in-memory de `LegacyWalletSync`:
  - ultimo inicio;
  - ultimo run completado;
  - ultimo fallo;
  - candidatos, sincronizados, saltados y fallidos;
  - error seguro por tipo de excepcion.
- El worker actualiza este estado en cada ejecucion.
- `/Admin/Support` muestra el estado del worker junto con la configuracion.
- `/Admin/Cutover` muestra el mismo resumen para tomar decisiones por negocio.

## Limites

El estado no sobrevive reinicios del proceso. Para auditoria persistente por
tarjeta se sigue usando `StampLedger`.

## Validacion

- Tests de estado in-memory.
- `/Admin/Support` y `/Admin/Cutover` muestran estado seguro.
- No se exponen tokens, passwords, connection strings ni detalles internos de
  excepciones.

No hay SQL nuevo.
