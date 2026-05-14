# 92 Product Frontend Cleanup v1

## Resumen

Esta fase limpia la superficie publica de Puntelio para que la app ya no se
vea como una herramienta de migracion o desarrollo.

## Cambios

- `/` mantiene una entrada publica simple para clientes y negocios.
- El header publico solo muestra `Clientes` y `Negocios`.
- El acceso a `/Admin/Login` sigue existiendo, pero ya no se publica en home ni
  en el header.
- Se reemplazan textos visibles de marca `DigitalCards` por `Puntelio`; los
  nombres internos de proyectos, namespaces y configuracion no cambian.
- Se elimina la pagina `/Dev/Outbox`.
- Los E2E ya no dependen de una pagina publica de Outbox; usan el link Wallet
  que devuelve el flujo de enrolamiento.

## Riesgos

- Las pruebas que antes abrían `/Dev/Outbox` deben usar links inline o servicios
  fake internos.
- `/Admin/Login` queda como ruta conocida solo por operacion/admin, no como
  navegación visible.

## Validacion

- Home/header no muestran Admin, Registro cliente ni Outbox.
- La UI publica usa Puntelio como marca visible.
- Los flujos Wallet fake siguen funcionando sin `/Dev/Outbox`.
