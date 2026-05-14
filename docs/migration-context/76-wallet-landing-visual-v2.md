# 76 Wallet Landing Visual v2

## Objetivo

Hacer que la landing publica de Wallet sea mas clara y visual para clientes en
iPhone y Android.

## Cambios

- Agrega una vista tipo tarjeta con cliente, negocio y progreso de sellos.
- Muestra cuantos sellos faltan para completar el ciclo actual.
- Mantiene branding por negocio: logo, colores primario/secundario y nombre.
- Hace los botones de Apple Wallet y Google Wallet mas faciles de distinguir.
- Conserva endpoints publicos por token opaco; no agrega cookies ni SQL.

## Seguridad

La landing no muestra `CardID`, JWT, token Apple, push token, password, hash ni
connection string. El token publico sigue viajando solo en la ruta existente.

## Validacion

- Playwright verifica la landing en viewports iPhone/Android.
- Se valida que la tarjeta visual sea visible.
- Se valida que no haya overflow horizontal.
