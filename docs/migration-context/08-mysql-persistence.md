# MySQL Persistence Adapter

## Scope

This adapter adds real persistence for the ASP.NET Core migration shell against
the existing Web Forms tables. It uses Dapper and MySqlConnector behind the
existing application interfaces.

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

For the HostGator database, keep the connection string in ASP.NET Core User
Secrets or environment variables. The repo includes a `hostgator-mysql` launch
profile that enables the `MySql` provider, but it does not contain credentials.

```powershell
dotnet user-secrets set "ConnectionStrings:DigitalCards" "Server=HOST;Port=3306;Database=DATABASE;User ID=USER;Password=PASSWORD;CharSet=utf8mb4;SslMode=Preferred;" --project src\DigitalCards.Web\DigitalCards.Web.csproj
dotnet run --launch-profile hostgator-mysql --project src\DigitalCards.Web\DigitalCards.Web.csproj
```

## Legacy Table Mapping

No new tables are created by this adapter. It maps the modern domain to these
existing tables:

- `UserClient` -> clients.
- `Business` -> businesses.
- `ClientCard` -> loyalty cards and stamp state.

The legacy schema uses integer ids. The modern domain currently uses `Guid`, so
Infrastructure maps legacy integer ids to deterministic GUID values internally.
Wallet enrollment tokens are based on the mapped `ClientCard.CardID`.

## Intentional Limits

- No production connection strings are committed.
- Wallet and email integrations remain fake by default. Google Wallet can be
  enabled explicitly with `DigitalCards:UseFakeIntegrations=false` and the
  required Google Wallet secrets.
- This adapter writes rows to the existing HostGator tables when the `MySql`
  provider is enabled.
- No DDL is executed by the application.
- `CardIDGoogle` is limited by the existing legacy column length, so the app
  stores only the Google object suffix and regenerates signed save URLs on
  demand.
