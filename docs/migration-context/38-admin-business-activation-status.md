# 38 Admin Business Activation Status

## Objetivo

Convertir el avance de migracion por negocio en un estado explicito
administrable desde la app moderna. Antes solo existia `IsEnabled` en
`ModernPilotBusiness`, que servia para bloquear o permitir pantallas modernas,
pero no expresaba si el negocio seguia en pruebas, ya operaba moderno como
principal o ya habia retirado Web Forms.

## Estados

- `LegacyOnly`: el negocio opera en Web Forms. El flujo moderno queda bloqueado
  cuando `DigitalCards:Pilot:Enabled=true`.
- `PilotModern`: el negocio esta en piloto controlado. Web Forms sigue como
  fallback activo.
- `ModernPrimary`: el negocio opera principalmente en ASP.NET Core. Web Forms
  queda como respaldo manual.
- `LegacyRetired`: el negocio ya no debe operar sellos desde Web Forms. El
  retiro real de rutas legacy sigue siendo un paso posterior.

## Cambios

- Se agrega `BusinessActivationStatus` al dominio.
- `PilotBusinessAccess` conserva `IsEnabled` para compatibilidad, pero ahora
  tambien guarda `ActivationStatus`.
- `/Admin/Businesses` muestra el estado formal de activacion.
- `/Admin/BusinessProfile/{businessId}` permite editar el estado.
- Los botones existentes de habilitar/deshabilitar piloto siguen funcionando:
  - habilitar traduce a `PilotModern`;
  - deshabilitar traduce a `LegacyOnly`.

## SQL HostGator

Antes de usarlo contra MySQL real, aplicar:

```text
docs/migration-context/38-admin-business-activation-status-hostgator.sql
```

El script agrega `ModernPilotBusiness.ActivationStatus` e indexa el campo. Los
negocios que ya estaban habilitados pasan a `PilotModern`; los no habilitados
quedan como `LegacyOnly`.

## Operacion

Para mover un negocio:

1. Entrar a `/Admin/Businesses`.
2. Abrir `Administrar`.
3. Cambiar `Estado de activacion`.
4. Guardar cambios.
5. Probar login negocio, `/Business/Cards`, Wallet links y sellos.

Recomendacion:

- usar `PilotModern` para pruebas;
- usar `ModernPrimary` cuando el negocio ya opera diario desde moderno;
- usar `LegacyRetired` solo cuando Web Forms ya no se use para ese negocio;
- volver a `LegacyOnly` si hace falta bloquear el flujo moderno.

## Seguridad

El estado no expone secrets ni credenciales. No muestra passwords, tokens
Wallet, JWTs, push tokens, certificados ni connection strings.

## Validacion

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```

