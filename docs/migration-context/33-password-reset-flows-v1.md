# PR 33: Password Reset Flows v1

## Summary

This PR adds password reset flows for client and business accounts in the modern ASP.NET Core app without introducing ASP.NET Core Identity or changing Web Forms behavior.

The flow uses one-time opaque reset tokens:

- the email contains the plaintext token only in the reset URL;
- the database stores only `SHA-256` token hashes;
- tokens expire after one hour;
- successful reset marks the token as used;
- requesting a new reset revokes previous active tokens for the same account.

## Scope

- Client reset:
  - `/Client/ForgotPassword`
  - `/Client/ResetPassword/{token}`
- Business reset:
  - `/Business/ForgotPassword`
  - `/Business/ResetPassword/{token}`
- Fake email outbox includes reset emails for local/dev tests.
- SMTP uses the existing centralized password reset email template.
- Reset updates both:
  - the legacy password field used by Web Forms;
  - the modern credential table used by ASP.NET Core.

## New Table

Apply manually before real smoke testing:

```text
docs/migration-context/33-password-reset-flows-v1-hostgator.sql
```

Table:

- `ModernPasswordResetToken`

Important fields:

- `AccountType`: `Client` or `Business`;
- `AccountID`: legacy `UserID` or `BusinessID`;
- `TokenHash`: SHA-256 hash of the reset token;
- `TokenSuffix`: short support/debug suffix;
- `ExpiresAt`, `UsedAt`, `RevokedAt`.

The legacy dump already includes a `PasswordResetToken` table used by Web Forms.
This PR intentionally uses `ModernPasswordResetToken` to avoid altering that
legacy flow.

## Security Notes

- No reset token plaintext is stored in MySQL.
- Pages return the same generic request message whether the account exists or not.
- Passwords are never rendered after submit.
- Logs include only masked account identifiers or token suffixes.
- No public admin/bootstrap endpoint is added.

## Rollout

1. Merge the PR.
2. Apply `33-password-reset-flows-v1-hostgator.sql` in HostGator.
3. Smoke client reset with a controlled test account.
4. Smoke business reset with a controlled test business.
5. Confirm Web Forms still accepts the new password because the legacy field is updated.

## Validation

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
