# Client Real QR v1

Este PR reemplaza el identificador visual fake del dashboard cliente por un QR real generado desde el `UserName`.

## Alcance

- `/Client/Dashboard` muestra un SVG QR real.
- `/Client/Cards` muestra el mismo QR para uso en mostrador.
- El payload v1 es `UserName`, consistente con la busqueda de negocio por username/email/QR.
- No se agregan tablas ni tokens nuevos.

## Seguridad

El QR no contiene password, token Wallet, CardID, JWT ni datos secretos. Sólo codifica el username legacy del cliente.

## Uso operativo

El negocio puede escanear el QR desde el telefono del cliente y pegar/capturar el resultado en la busqueda de `/Business/Cards` o en el modo check-in futuro.
