# 75 Interactive UI Polish v1

## Objetivo

Mejorar la experiencia visual del shell moderno autenticado sin cambiar reglas
de negocio ni flujos Wallet.

Este PR acerca la app moderna al dashboard Web Forms original con una
navegacion lateral mas usable en mobile y mejor feedback visual en tarjetas,
metricas y filas.

## Cambios

- Sidebar autenticado con apertura/cierre en mobile.
- Overlay para cerrar la navegacion al tocar fuera.
- Soporte de tecla `Escape` para cerrar el sidebar.
- Header con rol y nombre del usuario autenticado.
- Hover/focus mas claro en tarjetas, filas y metricas.
- Se mantiene la paleta legacy principal:
  - azul `#5d87ff`;
  - fondo `#f6f9fc`;
  - texto `#2a3547`;
  - bordes `#e5eaef`.

## Alcance

- Admin, negocio y cliente usan el mismo shell.
- Wallet publicas y login publicos quedan fuera de este PR.
- Sin SQL nuevo.
- Sin cambios en Apple Wallet, Google Wallet, SMTP, MySQL ni Web Forms.

## Validacion

- La pagina autenticada renderiza `data-testid="legacy-shell"`.
- El boton de menu mobile ya no esta deshabilitado.
- El sidebar conserva accesos por rol.
- Playwright sigue cubriendo flujos con fakes.
