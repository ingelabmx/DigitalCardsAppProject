# 56 Pilot Business Cutover Smoke Tests v1

Esta fase agrega smokes manuales automatizados para validar un negocio piloto de
punta a punta. No se ejecutan en CI normal y no imprimen secretos.

## Flags

### Fake controlado

```powershell
$env:RUN_PILOT_BUSINESS_CUTOVER_SMOKE='1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter PilotBusinessCutoverSmoke
Remove-Item Env:\RUN_PILOT_BUSINESS_CUTOVER_SMOKE -ErrorAction SilentlyContinue
```

Fuerza:

- `PersistenceProvider=InMemory`;
- Google Wallet fake;
- Apple Wallet fake;
- email fake.

### Real controlado

```powershell
$env:RUN_PILOT_BUSINESS_CUTOVER_REAL_SMOKE='1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter PilotBusinessCutoverRealSmoke
Remove-Item Env:\RUN_PILOT_BUSINESS_CUTOVER_REAL_SMOKE -ErrorAction SilentlyContinue
```

Fuerza:

- `PersistenceProvider=MySql`;
- Google Wallet real;
- Apple Wallet real;
- SMTP real.

Requiere `C:\Users\eguillen\.digitalcards\appsettings.Local.json` completo.

## Flujo Validado

- login de negocio;
- registro o reutilizacion de cliente smoke;
- asociacion cliente-negocio;
- busqueda de tarjeta por negocio;
- reenvio de link Wallet;
- seleccion Google Wallet;
- seleccion Apple Wallet;
- descarga `.pkpass` en smoke real;
- agregado de sello;
- registro `StampLedger`.

## Seguridad

Los smokes validan estados e IDs internos de prueba, pero no escriben en salida:

- passwords;
- JWTs;
- push tokens;
- Apple auth tokens;
- connection strings;
- rutas de certificados.

## Uso En Cutover

Ejecutar primero el fake smoke. Si pasa, ejecutar el real con un negocio y correo
controlados antes de mover un negocio a `ModernPrimary` o `LegacyRetired`.
