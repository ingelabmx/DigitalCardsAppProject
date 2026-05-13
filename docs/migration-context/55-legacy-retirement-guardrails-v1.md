# 55 Legacy Retirement Guardrails v1

Esta fase convierte `LegacyRetired` en una senal operativa visible dentro de la
app moderna. No toca Web Forms y no requiere SQL nuevo.

## Alcance

- `LegacyRetired` sigue permitiendo el flujo moderno.
- `/Admin/BusinessProfile/{businessId}` muestra advertencia clara cuando un
  negocio esta en `LegacyRetired`.
- `/Admin/Support` muestra el estado de activacion del negocio y marca los
  negocios `LegacyRetired`.
- La documentacion explica que Web Forms debe bloquearse manualmente por negocio
  mientras no exista automatizacion en legacy.

## No Cambia

- No se elimina negocio.
- No se bloquea login moderno.
- No se modifica Web Forms.
- No se agrega SQL.

## Uso Operativo

1. Admin cambia el negocio a `LegacyRetired`.
2. Admin confirma que el flujo moderno sigue operando.
3. Operacion bloquea manualmente el acceso Web Forms de ese negocio.
4. Soporte usa `/Admin/Support` para verificar el estado.

## Rollback

Cambiar el estado a `ModernPrimary` o `PilotModern` desde el perfil admin. Si
se quiere regresar a legacy, usar `LegacyOnly`.
