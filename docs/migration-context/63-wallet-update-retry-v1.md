# 63 Wallet Update Retry v1

## Objetivo

Agregar un reintento manual seguro desde `/Admin/Support` para refrescar Wallets
por tarjeta sin tocar datos de sellos ni crear tablas nuevas.

## Alcance

- El admin puede buscar una tarjeta en `/Admin/Support` y ejecutar
  `Reintentar update Wallet`.
- El reintento usa el estado actual de `ClientCard`.
- Si la tarjeta tiene Google Wallet emitida, se intenta `PatchStampStateAsync`.
- Si la tarjeta tiene Apple Wallet registrada/tracked, se marca el pass como
  actualizado y se envian pushes APNs mediante el servicio Apple existente.
- El resultado se registra en `StampLedger` con `Source=AdminRetry`.

## Seguridad

- No se muestran ni se guardan tokens planos, JWTs, push tokens, auth tokens,
  certificados, passwords, hashes ni connection strings.
- Los errores registrados en `StampLedger.ErrorSummary` son tipos seguros de
  excepcion, por ejemplo `InvalidOperationException`.
- El boton vive en `/Admin/Support`, que ya requiere cookie admin.

## Operacion

Flujo recomendado:

1. Entrar a `/Admin/Support`.
2. Buscar por cliente, negocio, tarjeta o link Wallet.
3. Revisar estado Google/Apple y eventos recientes.
4. Ejecutar `Reintentar update Wallet`.
5. Confirmar evento `AdminRetry` en StampLedger.
6. Validar en el dispositivo del cliente si la Wallet ya refleja el cambio.

## SQL

No requiere SQL nuevo. Usa la tabla existente `StampLedger`; el campo `Source`
almacena el nuevo valor `AdminRetry`.

## Limitaciones

- No cambia sellos; solo reintenta updates de Wallet con el snapshot actual.
- Apple Wallet puede tardar en pedir el pass actualizado despues del push APNs.
- Si la tarjeta no tiene Wallet emitida/tracked, se registra `NoWalletsTracked`.
