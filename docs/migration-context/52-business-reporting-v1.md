# 52 Business Reporting v1

Esta fase agrega reportes read-only para negocios en `/Business/Reports`.
No requiere SQL nuevo y no toca Web Forms.

## Alcance

El negocio autenticado puede ver solamente sus datos:

- tarjetas totales y tarjetas recientes;
- clientes recientes;
- sellos actuales e historicos;
- sellos por periodo;
- Google Wallet emitidas y pendientes;
- Apple Wallet activas y pendientes;
- dispositivos Apple registrados;
- alertas Wallet recientes.

La informacion se calcula desde tablas existentes y desde `StampLedger`.

## Seguridad

- El `BusinessId` siempre sale de la cookie de negocio.
- No se aceptan `businessId` por query string.
- La pantalla no muestra enrollment tokens, JWTs, push tokens, auth tokens,
  passwords, hashes, certificados ni connection strings.
- Es una vista solo lectura.

## Uso Operativo

1. Login en `/Business/Login`.
2. Abrir `/Business/Reports` desde sidebar o dashboard.
3. Revisar metricas de tarjetas, sellos, Wallets y alertas.
4. Si aparece una alerta Wallet, usar `/Business/Cards` para revisar la tarjeta
   y `/Admin/Support` para diagnostico profundo.

## Smoke

- Crear/asociar cliente desde negocio.
- Emitir Google Wallet o usar fake en CI.
- Agregar sello desde `/Business/Cards`.
- Abrir `/Business/Reports`.
- Confirmar que el cliente aparece en recientes, el periodo tiene sello y no
  aparece ningun secreto.

## Rollback

No hay cambios de esquema. Si la pantalla falla, se puede ocultar el link o
volver al commit anterior sin afectar Wallets, correo, sellos ni Web Forms.
