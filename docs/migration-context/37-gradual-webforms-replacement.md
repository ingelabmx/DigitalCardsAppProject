# 37 Gradual Web Forms Replacement

## Objetivo

Definir como pasar negocios de Web Forms al flujo moderno sin apagar el fallback
global. El reemplazo debe hacerse por negocio, con criterios observables y
rollback rapido.

Este PR no cambia codigo funcional ni tablas. Es el contrato operativo para las
siguientes activaciones.

## Principio De Migracion

HostGator sigue siendo el source of truth mientras Web Forms y ASP.NET Core
conviven. La app moderna reemplaza flujos operativos por negocio cuando:

- el negocio esta habilitado en piloto;
- el flujo moderno tiene paridad suficiente para ese negocio;
- `StampLedger`, Wallet updates y soporte admin permiten diagnosticar fallas;
- Web Forms queda disponible como fallback.

## Estados Por Negocio

### 1. Legacy Only

Estado inicial.

- El negocio opera en Web Forms.
- App moderna puede estar apagada para ese negocio.
- `LegacyWalletSync` puede observar sellos legacy si se activa para pruebas.

### 2. Pilot Modern

Estado de pruebas controladas.

- Admin habilita negocio desde `/Admin/Businesses`.
- Negocio usa `/Business/Login`, `/Business/Dashboard` y `/Business/Cards`.
- Clientes se asocian desde `/Business/Enroll` o se buscan desde
  `/Business/Cards`.
- Correos Wallet salen con links opacos.
- Apple/Google Wallet se emiten desde `app.puntelio.com`.
- Web Forms sigue permitido como fallback.

### 3. Modern Primary

Estado recomendado para negocios validados.

- Operacion diaria ocurre en `/Business/Cards`.
- Web Forms queda como respaldo manual.
- Admin revisa problemas desde `/Admin/Support`.
- `LegacyWalletSync` permanece activo solo si todavia hay sellos hechos desde
  Web Forms.

### 4. Legacy Retired For Business

Estado futuro, negocio por negocio.

- El negocio ya no usa pantallas legacy para sellos.
- Rutas legacy se bloquean o se ocultan para ese negocio.
- Web Forms puede seguir activo para otros negocios.
- Se mantiene rollback documentado hasta completar una ventana operativa.

## Gates Antes De Pasar A Modern Primary

Para cada negocio:

- login negocio moderno validado;
- busqueda de cliente validada;
- asociacion/enroll validado;
- reenvio de link Wallet validado;
- Apple Wallet instala y registra dispositivo;
- Google Wallet guarda tarjeta;
- sello moderno actualiza `ClientCard`;
- Google patch exitoso;
- Apple APNs/update exitoso;
- `StampLedger` registra sello;
- `/Admin/Support` muestra diagnostico correcto;
- smoke real completado en iPhone y Android;
- rollback probado: negocio puede volver a Web Forms.

## Gates Antes De Retirar Legacy Para Un Negocio

No retirar Web Forms para un negocio hasta que:

- el negocio complete al menos una ventana operativa real usando moderno como
  flujo principal;
- no existan errores recurrentes de SMTP, Google Wallet, Apple Wallet/APNs o
  MySQL;
- soporte pueda resolver una tarjeta usando `/Admin/Support`;
- el admin pueda actualizar datos del negocio desde
  `/Admin/BusinessProfile/{businessId}`;
- el admin pueda crear/resetear admins;
- el negocio pueda resetear password;
- el cliente pueda resetear password;
- el cliente pueda ver sus tarjetas desde `/Client/Cards`;
- exista decision explicita de rollback si falla la operacion.

## Flujos Que Ya Tienen Base Moderna

- Admin login, admins y negocios.
- Alta/edicion/reset de negocio.
- Piloto negocio y cliente.
- Login negocio con cookie.
- Dashboard negocio.
- Busqueda de tarjetas y sello moderno.
- Reenvio Wallet link con token opaco.
- Google Wallet real.
- Apple Wallet real, Web Service y APNs.
- SMTP real.
- Cliente login, dashboard, cards y cambio/reset de password.
- Branding negocio.
- Plantillas de correo.
- Soporte admin.
- Auditoria `StampLedger`.

## Flujos Que Deben Revisarse Antes De Retirar Legacy

- Reportes historicos usados por negocios.
- Pantallas admin legacy que tengan funciones no migradas.
- Upload real de logos/branding.
- Alta masiva o importacion de clientes, si existe en legacy.
- Reglas comerciales especiales por negocio.
- Backfill historico de auditoria, si se decide necesario.
- Manejo formal de negocios inactivos o eliminados.

## Rollback Por Negocio

Rollback rapido:

1. Deshabilitar negocio en `/Admin/Businesses`.
2. Confirmar que `Pilot.Enabled=true` bloquea sus pantallas modernas.
3. Operar sellos desde Web Forms.
4. Si se requiere mantener updates Wallet desde sellos legacy, dejar
   `LegacyWalletSync.Enabled=true`.
5. Revisar `/Admin/Support` para confirmar estado de tarjeta y Wallet.

Rollback global:

```json
{
  "DigitalCards": {
    "Pilot": {
      "Enabled": false
    },
    "LegacyWalletSync": {
      "Enabled": false
    }
  }
}
```

Luego reiniciar la app moderna.

## Activacion Recomendada

1. Elegir un negocio controlado.
2. Habilitarlo desde `/Admin/Businesses`.
3. Confirmar o crear branding basico.
4. Crear o asociar un cliente controlado.
5. Enviar link Wallet.
6. Instalar Apple y Google.
7. Agregar 2 sellos desde moderno.
8. Agregar 1 sello desde Web Forms si se quiere validar sync.
9. Revisar `StampLedger` y `/Admin/Support`.
10. Mantener el negocio en piloto hasta completar una ventana operativa.

## Indicadores Operativos

Medir manualmente al inicio:

- correos enviados;
- links abiertos;
- Apple passes instalados;
- Google Wallet saves;
- sellos modernos;
- sellos legacy detectados;
- updates Apple exitosos;
- patches Google exitosos;
- errores de readiness;
- errores visibles en `/Admin/Support`.

## Siguiente PR Recomendado

El siguiente PR deberia mover una pieza concreta de paridad faltante, no otro
documento general. Opciones recomendadas:

- `feature/business-logo-upload-v1`: upload seguro de logo/branding.
- `feature/admin-business-activation-status`: estado formal por negocio
  (`LegacyOnly`, `PilotModern`, `ModernPrimary`, `LegacyRetired`).
- `feature/support-export-v1`: export seguro de diagnostico para soporte.

