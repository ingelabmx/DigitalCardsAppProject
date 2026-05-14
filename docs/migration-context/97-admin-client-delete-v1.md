# Admin Client Delete v1

## Summary

This PR adds permanent client deletion from the modern admin area. The action removes the global client account and related modern operational data while preserving businesses.

## Behavior

From `/Admin/Clients`, an authenticated admin can search for a client and delete the client permanently after typing the client's username or email.

The delete flow removes:

- the `UserClient` record for the client;
- all `ClientCard` relationships for that client;
- Wallet link tokens for those cards;
- Apple Wallet pass registrations for those cards;
- stamp ledger records for those cards;
- modern client credentials;
- password reset tokens for the client;
- client consent rows;
- retired client pilot rows when present.

The delete flow does not remove businesses.

## Audit

The application records a safe `ClientDeleted` audit event before deleting operational data. The audit summary does not include passwords, hashes, Wallet tokens, push tokens, JWTs, or connection strings.

## Scope

No SQL is required. This PR uses tables already introduced by previous phases.

## Validation

- `git diff --check`
- `dotnet test DigitalCardsApp.Modern.sln`
- `RUN_PLAYWRIGHT=1 dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj`
