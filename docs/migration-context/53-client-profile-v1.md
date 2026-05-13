# 53 Client Profile v1

Esta fase agrega `/Client/Profile` para que el cliente moderno pueda editar sus
datos basicos sin tocar Wallet tokens ni `CardID`.

## Alcance

- Nueva pagina protegida por cookie `DigitalCards.Client`.
- El cliente puede editar:
  - nombre;
  - apellido;
  - correo.
- El username queda solo lectura y estable.
- El update escribe sobre `UserClient` legacy para mantener una sola fuente de
  verdad.
- Despues del update se refresca la cookie cliente con los claims nuevos.

## Validaciones

Se respetan limites legacy:

- `FirstName <= 30`;
- `Lastname <= 30`;
- `UserEmail <= 30`;
- correo requerido y con formato basico;
- correo duplicado bloqueado con mensaje seguro.

## Seguridad

- El `ClientId` sale de la cookie cliente, no de query string.
- Un cliente no puede editar otro usuario.
- No se muestran passwords, hashes, tokens Wallet, JWTs, push tokens ni
  connection strings.
- No se cambia `UserName`, porque puede estar referenciado por flujos legacy y
  soporte operativo.

## Smoke

1. Login cliente en `/Client/Login`.
2. Abrir `/Client/Profile`.
3. Cambiar nombre/apellido/email.
4. Confirmar que `/Client/Dashboard` muestra los datos nuevos.
5. Confirmar que Wallet links y tarjetas siguen disponibles.

## Rollback

No hay SQL nuevo. Si falla, ocultar el link de perfil o volver al commit
anterior. Web Forms y Wallets no dependen de esta pagina.
