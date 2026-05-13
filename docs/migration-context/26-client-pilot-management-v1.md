# 26 Client Pilot Management v1

Esta fase mueve el control operativo temporal de clientes piloto desde
`appsettings.Local.json` hacia la UI admin moderna. El admin puede buscar
clientes legacy de `UserClient` y habilitar/deshabilitar clientes para pruebas
controladas, soporte y rollback.

## Cambios

- Nueva tabla `ModernPilotClient` keyed por `UserClient.UserID`.
- Nuevo repositorio `IPilotClientRepository` con implementaciones InMemory y
  MySQL.
- `AdminAppService` puede buscar clientes legacy y actualizar su estado piloto.
- Nueva pagina protegida `/Admin/Clients`.
- `PilotAccessService` permite cliente cuando se llama en flujos que aun usan
  guardrail de cliente:
  - `DigitalCards:Pilot:Enabled=false`;
  - el cliente esta habilitado en `ModernPilotClient`;
  - el email/dominio sigue permitido por `AllowedClientEmails` o
    `AllowedClientEmailDomains` como fallback temporal.

No se migra dashboard/login de cliente en este PR. `UserClient` sigue siendo la
fuente de verdad para clientes.

## SQL HostGator

Antes de usar este flujo contra MySQL real, ejecutar:

```text
docs/migration-context/26-client-pilot-management-hostgator.sql
```

La tabla nueva no modifica `UserClient`, `Business`, `ClientCard`, Wallets ni
datos de Web Forms.

## Operacion

1. Entrar a `/Admin/Login`.
2. Abrir `/Admin/Clients`.
3. Buscar cliente por username, email, nombre o apellido.
4. Marcar `Habilitar piloto` o `Deshabilitar piloto`.
5. Probar desde negocio moderno:
   - `/Business/Enroll`;
   - `/Business/Cards`;
   - reenvio de link Wallet;
   - agregar sello.

Con `DigitalCards:Pilot:Enabled=true`, el admin sigue pudiendo usar esta tabla
como guardrail temporal. El flujo normal corregido es que el negocio habilitado
asocia al cliente mediante `/Business/Enroll` y opera la tarjeta desde
`/Business/Cards`; no requiere que admin habilite manualmente cada cliente.
Wallet landing, Google Wallet, Apple Wallet y Apple Wallet Web Service siguen
publicos por sus tokens/autorizacion propia.

## Rollback

Rollback rapido:

```json
{
  "DigitalCards": {
    "Pilot": {
      "Enabled": false
    }
  }
}
```

Rollback parcial: mantener `Pilot.Enabled=true` y agregar temporalmente el email
o dominio del cliente en `AllowedClientEmails` / `AllowedClientEmailDomains`.

## Seguridad

- `/Admin/Clients` requiere cookie `DigitalCards.Admin` y policy `AdminOnly`.
- La busqueda de clientes no selecciona ni muestra passwords o hashes.
- Los logs registran IDs de admin y cliente, no datos Wallet ni secretos.
- No se muestran tokens, JWTs, push tokens, passwords, hashes ni connection
  strings.

## Pruebas

- Unit/Application: upsert de `ModernPilotClient`, busqueda legacy y DI.
- Web: `/Admin/Clients` protegido, habilitar/deshabilitar cliente y negocio
  habilitado asociando cliente sin intervencion admin por cliente.
- Playwright: login admin, habilitar negocio, login negocio, enroll, Wallet fake
  y sello desde `/Business/Cards`.
