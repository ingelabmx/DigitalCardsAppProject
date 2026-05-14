# 100 Wallet Language Unification v1

Esta fase unifica el lenguaje visible de Wallet para admin, negocio y cliente:
la operacion diaria habla de `Tarjeta`, `Wallet` o `Tarjeta digital`, no de
plataformas separadas.

## Cambios

- Reportes de negocio y admin muestran tarjetas Wallet agregadas.
- Soporte admin muestra estado agregado de tarjeta digital.
- Cutover conserva los checks tecnicos internos, pero sus etiquetas visibles ya
  no exponen nombres de plataforma.
- Branding y refresh Wallet muestran total de actualizaciones completadas.
- Las pantallas publicas `/Wallet/Select`, `/Wallet/Apple` y `/Wallet/Google`
  conservan nombres de plataforma porque el cliente debe elegir donde guardar.

## Sin SQL Nuevo

No hay cambios de esquema. Se agregan campos DTO calculados desde datos
existentes.

## Validacion

- Business/Admin/Client usan lenguaje agregado de tarjeta digital.
- Las pantallas publicas Wallet siguen mostrando Apple y Google.
- Los reportes cuentan tarjetas listas por negocio, no una metrica visible por
  plataforma.
