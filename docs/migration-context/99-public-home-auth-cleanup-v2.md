# 99 Public Home Auth Cleanup v2

Esta fase limpia la entrada publica de Puntelio para que la pagina principal se
sienta como producto final y no exponga accesos internos.

## Cambios

- `/` conserva acceso publico a clientes y negocios, sin link visible de admin.
- El home usa lenguaje agregado: `tarjeta digital` y `Wallet`, sin separar Apple
  y Google fuera del flujo publico de instalacion.
- `/Business/Login` y `/Client/Login` eliminan las marcas laterales de rol y los
  badges visuales junto al titulo del formulario.
- `/Admin/Login` permanece accesible solo por URL directa.

## Sin SQL Nuevo

No hay cambios de esquema ni configuracion manual requerida.

## Validacion

- El home no muestra `/Admin/Login`, `Registro cliente`, Apple ni Google.
- Las paginas de login cliente/negocio conservan formularios y links de reset.
- El flujo de autenticacion existente no cambia.
