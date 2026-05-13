# PR 49: Production Pilot Cutover v1

## Summary

This PR defines the operating procedure for moving one real business at a time
from Web Forms fallback to the modern ASP.NET Core flow on `app.puntelio.com`.

It does not change Wallet code, authentication, persistence or Web Forms. The
goal is to make cutover repeatable, diagnosable and reversible.

## Cutover States

Use `/Admin/BusinessProfile/{businessId}` to set the business activation status:

- `LegacyOnly`: business operates in Web Forms. Modern business pages are
  blocked when `DigitalCards:Pilot:Enabled=true`.
- `PilotModern`: business is testing modern flow with controlled clients.
- `ModernPrimary`: business uses modern flow for daily operations, while Web
  Forms remains available as fallback.
- `LegacyRetired`: future state for a business that should no longer use legacy
  stamp screens.

## Business Cutover Checklist

Before moving a business to `PilotModern`:

1. Confirm `https://app.puntelio.com/health` returns healthy.
2. Confirm `/health/ready` is healthy with MySQL, config and Data Protection.
3. Confirm Cloudflare Tunnel is running against the expected local port.
4. Confirm `DigitalCards:PublicBaseUrl` is `https://app.puntelio.com`.
5. Confirm Google Wallet origins include `https://app.puntelio.com`.
6. Confirm Apple Wallet pass has `webServiceURL` under
   `https://app.puntelio.com/apple-wallet`.
7. Confirm SMTP sends to a controlled mailbox.
8. Confirm the business has branding or acceptable fallback values.
9. Confirm the business can login at `/Business/Login`.
10. Confirm admin support can find the business in `/Admin/Support`.

## Pilot Smoke

For each business in `PilotModern`:

1. Login admin at `/Admin/Login`.
2. Set the business to `PilotModern`.
3. Login business at `/Business/Login`.
4. Open `/Business/Cards`.
5. Search or register a controlled client.
6. Associate the client with the business.
7. Re-send Wallet link.
8. Open the real email.
9. Install Apple Wallet from iPhone.
10. Save Google Wallet from Android or browser-supported device.
11. Add one stamp from `/Business/Cards`.
12. Confirm Apple updates after APNs/update check.
13. Confirm Google Wallet updates after patch.
14. Confirm `StampLedger` shows `ModernBusiness`.
15. Confirm `/Admin/Support` shows safe state with no secrets.
16. Optional: add one stamp from Web Forms and confirm `LegacyWalletSync`.

## Promotion To ModernPrimary

Move a business from `PilotModern` to `ModernPrimary` only when all are true:

- business staff can complete login, search, association, resend and stamp
  without admin help;
- at least one real Apple install and one Google save have been validated;
- stamp updates succeeded for Apple and Google;
- `StampLedger` and `/Admin/Support` can explain the latest card state;
- business branding is acceptable;
- rollback to Web Forms has been tested or clearly rehearsed;
- no repeated readiness, SMTP, APNs, Google Wallet or MySQL errors remain.

## Rollback

Fast rollback for one business:

1. Admin opens `/Admin/BusinessProfile/{businessId}`.
2. Set activation status to `LegacyOnly`.
3. Save.
4. Business continues operating in Web Forms.
5. Keep `LegacyWalletSync.Enabled=true` only if legacy stamps should still
   update Wallets.
6. Use `/Admin/Support` to inspect affected cards.

Global rollback:

```json
{
  "DigitalCards": {
    "Pilot": {
      "Enabled": false
    },
    "LegacyWalletSync": {
      "Enabled": false
    }
  }
}
```

Then restart the modern app.

## Support Data To Capture

For every cutover attempt, record outside the repo:

- business name and `BusinessID`;
- date/time of smoke;
- client test email or username;
- Apple installed: yes/no;
- Google saved: yes/no;
- stamp update result;
- `StampLedger` status;
- any safe error summaries from `/Admin/Support`;
- rollback decision if needed.

Do not record tokens, JWTs, push tokens, passwords, certificate paths,
connection strings or service-account content.

## No SQL

This PR adds no schema changes. It relies on previous tables:

- `ModernPilotBusiness`;
- `ModernBusinessCredential`;
- `ModernBusinessBranding`;
- `WalletLinkToken`;
- `StampLedger`;
- Apple Wallet registration tables.
