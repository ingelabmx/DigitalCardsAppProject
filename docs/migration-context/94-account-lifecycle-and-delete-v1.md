# 94 Account Lifecycle and Delete v1

## Resumen

Esta fase agrega lifecycle operacional para negocios y tarjetas cliente-negocio.
Admin puede activar/desactivar o eliminar permanentemente un negocio. El negocio
puede desactivar/reactivar o eliminar la tarjeta de un cliente para su negocio
sin borrar la cuenta global del cliente.

## SQL

Aplicar manualmente en HostGator:

```text
docs/migration-context/94-account-lifecycle-and-delete-hostgator.sql
```

## Cambios

- Estado visible de negocio en Admin: `Activo` / `Inactivo`.
- Crear negocio mantiene `Enviar invitacion por correo` marcado por default.
- Admin puede eliminar permanentemente un negocio desde su perfil.
- La eliminacion de negocio borra `Business`, tarjetas `ClientCard` de ese
  negocio, tokens Wallet, Apple pass registrations, ledger y datos modernos
  asociados.
- Las cuentas globales `UserClient` no se eliminan.
- El negocio puede desactivar/reactivar una tarjeta propia.
- El negocio puede eliminar permanentemente una tarjeta/asociacion propia.
- Los logos subidos se eliminan solo si pertenecen al directorio controlado de
  uploads.

## Validacion

- Negocio inactivo no puede iniciar sesion moderna.
- Admin elimina negocio y sus tarjetas dejan de existir.
- Cliente global sobrevive a la eliminacion del negocio.
- Links Wallet de tarjetas eliminadas dejan de resolver.
- Negocio no puede operar tarjetas de otro negocio.
