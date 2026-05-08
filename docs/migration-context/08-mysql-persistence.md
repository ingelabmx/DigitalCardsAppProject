# MySQL Persistence Adapter

## Scope

This adapter adds real persistence for the ASP.NET Core migration shell without
touching production or the Web Forms tables. It uses Dapper and MySqlConnector
behind the existing application interfaces.

## Provider Switch

The default remains in-memory:

```json
{
  "DigitalCards": {
    "UseFakeIntegrations": true,
    "PersistenceProvider": "InMemory"
  }
}
```

To use MySQL locally:

```powershell
$env:DigitalCards__PersistenceProvider = 'MySql'
$env:ConnectionStrings__DigitalCards = 'Server=localhost;Database=dcards_test;User ID=dcards_user;Password=LOCAL_PASSWORD;'
dotnet run --project src\DigitalCards.Web\DigitalCards.Web.csproj
```

## Schema

Run `docs/db-modern-mysql.sql` against a local or test database. The script uses
`modern_clients`, `modern_businesses`, and `modern_loyalty_cards` so it does not
collide with the legacy tables `UserClient`, `Business`, and `ClientCard`.

## Intentional Limits

- No production connection strings are committed.
- Wallet and email integrations remain fake.
- No legacy table writes are introduced in this step.
- The legacy mapping can be added later as a separate adapter once the migration
  contract is stable.
