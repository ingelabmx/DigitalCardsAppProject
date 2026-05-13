# 44 Business Only Pilot Access v1

## Objetivo

Retirar la allowlist de clientes del flujo operativo moderno. El piloto se
controla solo por negocio: el admin habilita negocios desde `/Admin/Businesses`
y el negocio habilitado asocia clientes desde `/Business/Enroll` o
`/Business/Cards`.

## Cambios

- `PilotAccessService` ya no bloquea clientes por `ModernPilotClient`,
  `AllowedClientEmails` ni `AllowedClientEmailDomains`.
- La configuracion de ejemplo conserva solo:
  - `DigitalCards:Pilot:Enabled`;
  - `AllowedBusinessIds`;
  - `AllowedBusinessEmails`.
- El dashboard admin y el sidebar ya no muestran `Clientes piloto`.
- `/Admin/Clients` queda como consulta legacy segura si alguien entra por URL
  directa, pero no permite habilitar/deshabilitar clientes.
- La tabla `ModernPilotClient` y su repositorio no se borran para evitar una
  migracion destructiva; quedan como historial/compatibilidad.

## Flujo Real

1. Admin entra a `/Admin/Login`.
2. Admin habilita el negocio desde `/Admin/Businesses`.
3. Negocio entra a `/Business/Login`.
4. Negocio asocia cliente desde `/Business/Enroll` o busca tarjeta desde
   `/Business/Cards`.
5. El sistema envia o reenvia el link Wallet con token opaco.
6. Cliente instala Apple Wallet o guarda Google Wallet.
7. Negocio agrega sello; Wallets y `StampLedger` se actualizan.

## Seguridad

- Un negocio no habilitado sigue bloqueado cuando `Pilot.Enabled=true`.
- Wallet landing, Google, Apple y Apple Web Service siguen publicos por token o
  autorizacion propia, no por cookie de negocio.
- La consulta legacy de clientes no muestra passwords, hashes, tokens, JWTs,
  push tokens ni connection strings.
- No hay SQL nuevo.

## Rollback

Rollback operativo rapido:

```json
{
  "DigitalCards": {
    "Pilot": {
      "Enabled": false
    }
  }
}
```

Si se necesitara recuperar la vieja allowlist de clientes, usar historial git.
No se recomienda reactivarla porque contradice el flujo real del negocio.

## Validacion

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
