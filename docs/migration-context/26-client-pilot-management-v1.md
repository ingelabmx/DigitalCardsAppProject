# 26 Client Pilot Management v1

Esta fase mueve el control operativo de clientes piloto desde
`appsettings.Local.json` hacia la UI admin moderna. El admin puede buscar
clientes legacy de `UserClient` y habilitar/deshabilitar cuales pueden usar
enroll, reenvio de link Wallet y sellos dentro del flujo moderno.

## Cambios

- Nueva tabla `ModernPilotClient` keyed por `UserClient.UserID`.
- Nuevo repositorio `IPilotClientRepository` con implementaciones InMemory y
  MySQL.
- `AdminAppService` puede buscar clientes legacy y actualizar su estado piloto.
- Nueva pagina protegida `/Admin/Clients`.
- `PilotAccessService` permite cliente cuando:
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

Con `DigitalCards:Pilot:Enabled=true`, un cliente deshabilitado queda bloqueado
para acciones modernas aunque el negocio este habilitado. Wallet landing,
Google Wallet, Apple Wallet y Apple Wallet Web Service siguen publicos por sus
tokens/autorizacion propia.

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
- Web: `/Admin/Clients` protegido, habilitar/deshabilitar cliente y bloqueo de
  enroll cuando el cliente no esta habilitado.
- Playwright: login admin, habilitar negocio, habilitar cliente, login negocio,
  enroll, Wallet fake y sello desde `/Business/Cards`.
