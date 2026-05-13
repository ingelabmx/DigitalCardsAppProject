# Puntelio Pilot Cutover Checklist

Use this file as the operator checklist for a real business cutover on
`app.puntelio.com`.

## Preflight

- [ ] `https://app.puntelio.com/health` is healthy.
- [ ] `https://app.puntelio.com/health/ready` is healthy.
- [ ] Cloudflare Tunnel is running.
- [ ] App is running on the configured local port.
- [ ] `PublicBaseUrl` is `https://app.puntelio.com`.
- [ ] SMTP smoke succeeded.
- [ ] Google Wallet origin includes `https://app.puntelio.com`.
- [ ] Apple pass installs from `app.puntelio.com`.
- [ ] Business exists and can login.
- [ ] Business branding is configured or fallback is acceptable.

## PilotModern Smoke

- [ ] Admin sets business to `PilotModern`.
- [ ] Business logs in.
- [ ] Business opens `/Business/Cards`.
- [ ] Business searches or registers controlled client.
- [ ] Business sends Wallet link.
- [ ] Client receives real email.
- [ ] Apple Wallet installs on iPhone.
- [ ] Google Wallet saves successfully.
- [ ] Business adds stamp from modern flow.
- [ ] Apple Wallet updates.
- [ ] Google Wallet updates.
- [ ] `StampLedger` shows `ModernBusiness`.
- [ ] `/Admin/Support` shows safe state.
- [ ] Optional legacy stamp sync succeeds.

## Promotion Decision

- [ ] Staff can operate without admin help.
- [ ] No repeated SMTP/APNs/Google/MySQL/readiness failures.
- [ ] Support can diagnose a card.
- [ ] Rollback path is understood.
- [ ] Admin sets business to `ModernPrimary`.

## Rollback

- [ ] Admin sets business back to `LegacyOnly`.
- [ ] Business operates from Web Forms.
- [ ] `LegacyWalletSync` decision made.
- [ ] Affected cards reviewed in `/Admin/Support`.
