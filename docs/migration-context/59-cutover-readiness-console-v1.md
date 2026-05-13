# 59 Cutover Readiness Console v1

Esta fase agrega una consola admin para mover negocios por etapas sin depender
de consultas manuales.

## Cambios

- Nueva pagina `/Admin/Cutover`.
- Lista negocios con estado de activacion:
  - `LegacyOnly`;
  - `PilotModern`;
  - `ModernPrimary`;
  - `LegacyRetired`;
  - `Inactive`.
- Muestra senales de readiness derivadas de datos existentes:
  - branding configurado;
  - tarjetas asociadas;
  - sellos recientes;
  - Google Wallet emitida;
  - Apple Wallet tracked;
  - errores Wallet recientes.
- Permite cambiar estado desde la consola con confirmacion visual.
- Enlaza a perfil del negocio y diagnostico de soporte.

## Operacion

Usar esta consola antes de mover un negocio a `ModernPrimary`:

1. Confirmar readiness visual.
2. Revisar soporte si hay errores Wallet.
3. Ejecutar smoke real de negocio.
4. Cambiar estado a `ModernPrimary`.
5. Mantener rollback cambiando a `PilotModern` o `LegacyOnly`.

No hay SQL nuevo; usa `ModernPilotBusiness`, reportes y soporte existentes.
