# 29 Client Dashboard v2

Esta fase mejora el dashboard moderno de cliente sin crear tablas nuevas ni
apagar Web Forms. El cliente sigue usando cookie `DigitalCards.Client`, y las
tarjetas se cargan por `ClientId` autenticado.

## Cambios

- `/Client/Dashboard` muestra resumen de tarjetas, sellos actuales, sellos
  historicos y conteo de Wallets emitidas/tracked.
- El dashboard incluye perfil basico de cliente: usuario y correo.
- `/Client/Cards` muestra por tarjeta:
  - negocio;
  - sellos actuales e historicos;
  - ultimo sello;
  - estado Google Wallet;
  - estado Apple Wallet;
  - dispositivos Apple registrados;
  - link Wallet con token opaco.
- No se exponen `CardID`, JWTs, push tokens, auth tokens ni hashes.

## Operacion

1. Cliente entra a `/Client/Login`.
2. Abre `/Client/Dashboard`.
3. Revisa resumen y perfil basico.
4. Abre `/Client/Cards` para ver tarjetas detalladas.
5. Usa `Abrir Wallet` para llegar a `/Wallet/Select/{opaqueToken}`.

El negocio sigue asociando clientes desde `/Business/Enroll` y operando sellos
desde `/Business/Cards`.

## Pruebas

- Application: dashboard devuelve perfil, resumen y estados Wallet.
- Web: dashboard muestra perfil, conteos y links Wallet opacos.
- Playwright: flujo negocio/cliente valida resumen, tarjetas y cambio de
  contrasena existente.

No requiere SQL nuevo.
