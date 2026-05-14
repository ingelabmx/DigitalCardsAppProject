# Business Check-in Mode v1

Este PR agrega `/Business/CheckIn` como modo mostrador para operar rapido con QR real del cliente.

## Alcance

- Nueva pagina `/Business/CheckIn`.
- Requiere cookie de negocio y respeta `PilotAccessService`.
- Permite escanear QR con `BarcodeDetector` del navegador o capturar username/email.
- Busca tarjetas del negocio autenticado.
- Permite agregar sello con un boton grande desde tarjeta validada.
- Reutiliza `AddStampToCardAsync`, por lo que mantiene updates Google/Apple y `StampLedger`.

## Seguridad

El negocio solo puede operar tarjetas de su propio `BusinessID`. No se muestran tokens Wallet, push tokens, JWTs, passwords ni hashes.

## Operacion

El cliente muestra su QR desde `/Client/Dashboard` o `/Client/Cards`. El negocio abre `/Business/CheckIn`, escanea el QR, valida la tarjeta y agrega el sello.
