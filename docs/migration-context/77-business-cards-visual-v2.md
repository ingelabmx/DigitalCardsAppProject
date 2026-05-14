# 77 Business Cards Visual v2

## Objetivo

Mejorar la pantalla `/Business/Cards`, que es el centro operativo diario del
negocio para buscar clientes, reenviar Wallet y agregar sellos.

## Cambios

- Agrega resumen rapido de resultados, tarjeta seleccionada y estado Wallet.
- Mejora cada resultado con indicadores compactos de Google y Apple.
- Agrega una franja de accion principal en el detalle de tarjeta.
- Muestra cuantos sellos faltan para completar el ciclo actual.
- Mantiene el lector QR y los formularios existentes.

## Alcance

- No cambia reglas de negocio.
- No cambia Google Wallet, Apple Wallet, SMTP ni MySQL.
- No requiere SQL.
- Web Forms sigue vivo como fallback.

## Validacion

- Web tests verifican resumen, estado visual y acciones.
- Playwright valida que el negocio puede buscar tarjeta, abrir detalle,
  reenviar Wallet y agregar sello con fakes.
