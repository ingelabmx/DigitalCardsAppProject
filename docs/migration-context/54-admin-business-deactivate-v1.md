# 54 Admin Business Deactivate v1

Esta fase agrega desactivacion operacional de negocio sin delete fisico y sin
SQL nuevo. Usa `ModernPilotBusiness.ActivationStatus`, agregado en PR 38.

## Alcance

- Nuevo estado `Inactive` en `BusinessActivationStatus`.
- `/Admin/BusinessProfile/{businessId}` permite seleccionar `Inactivo`.
- `Inactive` guarda `IsEnabled=false` en `ModernPilotBusiness`.
- Login moderno de negocio queda bloqueado cuando el negocio esta inactivo.
- Los negocios no allowlisted siguen pudiendo iniciar sesion y ver el bloqueo de
  piloto, como antes.
- Web Forms no se toca.

## Seguridad

- No se elimina `Business`.
- No se modifica `Business.BusinessPassword`.
- No se muestran credenciales ni secretos.
- El bloqueo se evalua por `BusinessID` en la tabla moderna.

## Uso Operativo

1. Entrar a `/Admin/Login`.
2. Abrir `/Admin/Businesses`.
3. Administrar el negocio.
4. Cambiar `Estado de activacion` a `Inactivo`.
5. Guardar.
6. Confirmar que `/Business/Login` muestra bloqueo y no emite cookie moderna.

## Reactivacion

Desde el mismo perfil admin, cambiar el estado a:

- `PilotModern`, para piloto controlado;
- `ModernPrimary`, cuando el negocio opere principalmente en moderno;
- `LegacyOnly`, para volver a legacy sin permitir pantallas modernas.

## Rollback

No hay schema nuevo. Para revertir una desactivacion, cambiar el estado desde el
admin o actualizar `ModernPilotBusiness.ActivationStatus` manualmente a
`PilotModern`/`LegacyOnly` segun el caso.
