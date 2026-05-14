# 71 Wallet Branding Refresh v1

## Objetivo

Permitir refrescar Wallets ya emitidas cuando cambia el branding de un negocio. El refresh aplica logo, colores, nombre publico y texto actual a tarjetas recientes sin modificar sellos, tokens ni datos legacy.

## Alcance

- Admin puede refrescar Wallets desde `/Admin/BusinessProfile/{businessId}`.
- Negocio puede refrescar Wallets desde `/Business/Branding`.
- La accion toma tarjetas recientes del negocio con limite seguro de 1 a 100.
- Google Wallet se actualiza con `PatchStampStateAsync`, que ya incluye branding.
- Apple Wallet se marca para update con `NotifyPassUpdatedAsync`; el pass actualizado se descarga despues desde Apple Wallet Web Service.
- Cada tarjeta con Wallet trackeada registra evento `StampLedger.Source=BrandingRefresh`.
- Si no hay Wallets trackeadas, se muestra alerta segura `NoTrackedWallets`.

## Seguridad

- No se muestran tokens Wallet, JWTs, push tokens, auth tokens ni paths locales.
- Los errores quedan reducidos a nombres de excepcion seguros.
- No hay SQL nuevo; `StampLedger.Source` es `varchar(32)` y acepta `BrandingRefresh`.
- El refresh no cambia `CheckQTY`, `HistoricCheckQTY`, `LastCheck` ni `Business.BusinessLogo`.

## Operacion

Smoke recomendado:

1. Actualizar branding/logo desde admin o negocio.
2. Ejecutar `Refrescar Wallets recientes`.
3. Revisar evento `BrandingRefresh` en detalle de tarjeta o `/Admin/Support`.
4. Validar Google Wallet en Android.
5. Validar Apple Wallet en iPhone despues de APNs/update.

## Rollback

Si el refresh marca alertas, no afecta sellos ni login. Revisa `StampLedger` y ejecuta reintento Wallet por tarjeta desde soporte si hace falta.
