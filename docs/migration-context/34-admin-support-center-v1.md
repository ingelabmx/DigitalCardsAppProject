# PR 34: Admin Support Center v1

## Summary

This PR adds `/Admin/Support`, a read-only operational view for support and pilot troubleshooting. It helps inspect a customer, business, or loyalty card without exposing Wallet tokens, password data, push tokens, JWTs, connection strings, or certificates.

No HostGator SQL is required.

## Scope

- Adds a protected admin page:
  - `/Admin/Support`
- Search accepts:
  - client username, email, first name, or last name;
  - business name or email;
  - card GUID / legacy CardID token;
  - a Wallet URL, using the last path segment as the lookup value.
- Shows safe support state:
  - matching clients and card counts;
  - matching businesses and pilot state;
  - card stamp counts and last stamp;
  - Google Wallet issued/pending state with suffix only;
  - Apple Wallet tracked/pending state, device count, update tag, serial suffix;
  - recent `StampLedger` events and Wallet update status;
  - `LegacyWalletSync` configuration state.

## Security Notes

- Requires `DigitalCards.Admin` cookie and `AdminOnly` policy.
- Does not show enrollment tokens, opaque Wallet tokens, auth tokens, push tokens, JWTs, password hashes, SMTP credentials, service account data, certificates, or connection strings.
- Does not mutate data.
- Logs only query length, not raw support search terms.

## Operational Use

Use `/Admin/Support` during pilot smokes and support calls:

1. Search the client username/email or business name.
2. Confirm the expected card exists.
3. Check Google/Apple Wallet state.
4. Review recent `StampLedger` events.
5. Confirm `LegacyWalletSync` is active or off as expected.

## Validation

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
