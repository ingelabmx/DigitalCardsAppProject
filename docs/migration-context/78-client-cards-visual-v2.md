# 78 Client Cards Visual v2

## Objetivo

Mejorar `/Client/Cards` para que el cliente entienda rapidamente cuantas
tarjetas tiene, cuantos sellos acumula y que Wallets estan disponibles.

## Cambios

- Agrega resumen visual de tarjetas, sellos actuales, sellos historicos y
  Wallets emitidas.
- Agrega panel de progreso por tarjeta con sellos faltantes para completar el
  ciclo.
- Mantiene QR real del cliente y links Wallet opacos.
- No cambia reglas de negocio ni endpoints publicos.

## Alcance

- Sin SQL.
- Sin cambios a Apple Wallet, Google Wallet, SMTP ni HostGator.
- Cliente sigue viendo solo sus propias tarjetas.

## Validacion

- Web tests verifican resumen y progreso.
- Playwright valida dashboard cliente, lista de tarjetas, QR real y estados
  Wallet con fakes.
