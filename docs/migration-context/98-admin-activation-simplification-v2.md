# 98 Admin Activation Simplification v2

Esta fase simplifica el lenguaje operativo de administracion: para el admin, un
negocio esta `Activo` o `Inactivo`. Los valores historicos internos siguen
existiendo para compatibilidad tecnica, pero ya no se exponen como opciones en
formularios admin.

## Cambios

- `/Admin/BusinessProfile/{businessId}` muestra solo `Activo` / `Inactivo`.
- `/Admin/Cutover` filtra y cambia estado con el mismo lenguaje simplificado.
- Crear negocio mantiene `Enviar invitacion por correo` marcado por default.
- Al subir un nuevo logo desde admin o desde self-service de negocio, el archivo
  anterior se elimina si pertenece al directorio controlado de uploads.
- Se corrigio la materializacion MySQL de `LegacyWalletSync` para evitar que el
  worker detenga la app cuando MySQL devuelve `HasRegisteredAppleDevices`.

## Sin SQL Nuevo

No hay cambios de esquema. La app sigue usando las tablas existentes.

## Validacion

- Formularios admin no necesitan exponer nombres tecnicos como `ModernPrimary`.
- Negocio inactivo sigue bloqueado para login moderno.
- Logos anteriores subidos desde el flujo seguro se limpian al reemplazarlos.
- `LegacyWalletSync` puede materializar filas de HostGator sin tumbar el host.
