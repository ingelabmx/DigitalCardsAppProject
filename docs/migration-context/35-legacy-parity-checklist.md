# PR 35: Legacy Parity Checklist

## Summary

This checklist tracks which Web Forms capabilities already have a modern ASP.NET Core equivalent, which ones are operationally usable but still incomplete, and which ones remain legacy-only.

The goal is to make replacement decisions by flow, not by hope. Web Forms stays available until the modern path is covered by tests, runbooks, and a rollback plan.

## Status Legend

- **Ready:** modern flow exists, has tests, and can be piloted.
- **Pilot:** modern flow works for controlled users/businesses but needs more operations hardening before broad use.
- **Partial:** foundation exists but key pieces are missing.
- **Legacy Only:** still handled by Web Forms or manual admin/DB operations.

## Admin Flows

| Flow | Modern Status | Notes |
| --- | --- | --- |
| Admin login | Ready | `/Admin/Login` uses `UserClient.RoleID=1`, modern credential migration, and admin cookie. |
| Admin creation/reset | Ready | `/Admin/AdminUsers` and `/Admin/CreateAdmin`; no public bootstrap endpoint. |
| Business pilot management | Ready | `/Admin/Businesses` backed by `ModernPilotBusiness`. |
| Client pilot guardrail | Retired | Client allowlist no longer gates modern flows; enabled businesses associate clients directly. |
| Business creation | Ready | `/Admin/CreateBusiness` inserts into legacy `Business` and creates modern credential. |
| Business profile/password reset | Ready | `/Admin/BusinessProfile/{businessId}` edits legacy-safe fields and resets credentials. |
| Business branding | Pilot | Branding table exists and feeds email/Wallet/client UI; logo upload is not implemented. |
| Admin support center | Ready | `/Admin/Support` gives safe read-only diagnostics. |
| Delete/deactivate business | Legacy Only | Not implemented in modern app. |
| Admin reporting/export | Legacy Only | No modern reports/export center yet. |

## Business Flows

| Flow | Modern Status | Notes |
| --- | --- | --- |
| Business login/logout | Ready | Cookie auth with `DigitalCards.Business`. |
| Dashboard | Pilot | `/Business/Dashboard` shows operational summary and recent Wallet/ledger state. |
| Search cards/clients | Pilot | `/Business/Cards` searches cards owned by authenticated business. |
| Enroll/associate client | Pilot | `/Business/Enroll` creates/reuses `ClientCard` and sends Wallet link. |
| Resend Wallet email | Pilot | `/Business/Cards` resend uses opaque Wallet link token. |
| Add stamp | Pilot | Modern stamp updates `ClientCard`, patches Google, pushes Apple, and writes `StampLedger`. |
| Stamp audit | Pilot | `StampLedger` starts from modern deployment; no historical backfill. |
| Legacy stamp sync | Pilot | `LegacyWalletSync` detects Web Forms changes and updates Wallets. |
| Rich business reporting | Legacy Only | Dashboard v2 is a summary, not full reporting parity. |
| Business self-service profile/branding | Legacy Only | Admin controls branding/profile today. |

## Client Flows

| Flow | Modern Status | Notes |
| --- | --- | --- |
| Client registration | Pilot | Modern registration creates legacy and modern credentials. |
| Client login/logout | Ready | Cookie auth with `DigitalCards.Client`. |
| Client dashboard | Pilot | `/Client/Dashboard` and `/Client/Cards` show owned cards only with legacy-style visual shell. |
| Change password | Ready | Updates legacy and modern credentials. |
| Forgot/reset password | Ready | One-time hashed reset tokens via email. |
| Wallet landing | Ready | Public token-based landing remains outside cookie auth and uses business branding. |
| Full profile editing | Legacy Only | Modern client profile edit is not implemented. |
| Client history beyond current cards | Partial | Card state is visible; detailed historical activity is not complete. |

## Wallet And Email Flows

| Flow | Modern Status | Notes |
| --- | --- | --- |
| Google Wallet issue | Ready | Real provider controlled by config; fakes default for CI. |
| Google Wallet patch on stamp | Ready | Modern stamp and LegacyWalletSync can patch. |
| Apple `.pkpass` issue | Ready | Installs on iPhone with real certificates. |
| Apple Web Service registration | Ready | Device registration and pass update endpoints exist. |
| Apple APNs update | Ready | Push happens when modern app or sync detects stamp changes. |
| Wallet link security | Ready | New links use opaque tokens stored by hash. Legacy compatibility still configurable. |
| SMTP Wallet email | Ready | Real SMTP controlled by external config. |
| Email templates | Pilot | Wallet and reset flows use templates; welcome/internal alerts are template-ready. |
| Wallet diagnostics | Ready | `/Admin/Support` and internal diagnostics provide safe state. |

## Operations And Deployment

| Flow | Modern Status | Notes |
| --- | --- | --- |
| Single real config | Ready | `%USERPROFILE%\.digitalcards\appsettings.Local.json`. |
| Cloudflare domain | Ready | `app.puntelio.com` is canonical for Wallet links. |
| Health checks | Ready | `/health` and `/health/ready`. |
| Data Protection keys | Ready | External path supported for stable cookies. |
| Pilot guardrails | Ready | Business allowlist is admin-managed; client allowlist is retired. |
| Production service hosting | Partial | Needs a dedicated runbook/service setup PR. |
| Monitoring/alerts | Partial | Logs exist; no external monitoring integration yet. |
| Rollback | Pilot | Primary rollback remains Web Forms plus disabling pilot/sync. |

## Replacement Gates

Do not retire a Web Forms flow until all items are true:

1. The modern flow is listed as **Ready** or explicitly accepted as **Pilot** for a named business.
2. Playwright covers the critical path with fakes.
3. A real smoke test has passed on `app.puntelio.com`.
4. The rollback path is documented.
5. The admin support center can diagnose the expected card/business/client state.

## Next Recommended PRs

1. `feature/production-service-hosting-v1`
   - make the runtime stable as a service, including app/cloudflared startup and restart.
2. `feature/gradual-webforms-replacement`
   - define the operational activation plan by business.
3. `feature/business-self-service-v1`
   - eventually move selected profile/branding controls from admin to business.
