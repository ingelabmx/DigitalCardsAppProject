# 51 Admin Support Center v2

Esta fase mejora `/Admin/Support` para operacion real diaria. Sigue siendo una
vista solo lectura y no requiere SQL nuevo.

## Alcance

- Filtros por negocio, cliente, rango de fecha y tarjetas con alertas Wallet.
- Export seguro en JSON y CSV.
- Estado de `LegacyWalletSync` visible desde la pantalla.
- Por tarjeta:
  - cliente y negocio;
  - sellos actuales e historicos;
  - estado Google Wallet y Apple Wallet;
  - dispositivos Apple registrados;
  - eventos recientes de `StampLedger`;
  - conteo de eventos `LegacySync`;
  - ultimos errores seguros de Wallet.

## Seguridad

La pantalla y los exports no deben mostrar:

- passwords o hashes;
- connection strings;
- JWTs;
- push tokens;
- Apple authentication tokens;
- enrollment tokens completos;
- rutas fisicas locales;
- certificados o passwords de certificados.

Los errores se muestran como resumen operativo. Si un mensaje es largo, se
recorta antes de llegar a la UI/export.

## Uso Operativo

1. Entrar a `/Admin/Login`.
2. Abrir `/Admin/Support`.
3. Buscar por username/email de cliente, nombre/email de negocio, `CardID` o
   token Wallet.
4. Si hay muchos resultados, usar filtros:
   - negocio;
   - cliente;
   - rango de fecha;
   - solo alertas Wallet.
5. Revisar `StampLedger`, `LegacySync` y errores seguros.
6. Descargar JSON o CSV solo para soporte interno.

## Smoke Recomendado

- Buscar una tarjeta creada por un negocio piloto.
- Confirmar que aparece Google/Apple state.
- Agregar sello moderno y confirmar evento `ModernBusiness`.
- Agregar sello desde Web Forms con `LegacyWalletSync` activo y confirmar evento
  `LegacySync`.
- Exportar JSON/CSV y revisar que no incluya secretos.

## Rollback

No hay cambio de esquema ni flujo de negocio. Si se detecta un problema, el
rollback es volver al commit anterior o no usar `/Admin/Support` mientras se
corrige. Wallet, login, correos y Web Forms no dependen de esta vista.
