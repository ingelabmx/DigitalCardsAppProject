# PR 48: Business Self-Service v1

## Summary

This phase lets an authenticated business manage safe public branding from the
modern app without asking an admin to edit every visual change.

The scope is intentionally narrow: businesses can update Wallet-facing branding
only. They cannot change login email, password, pilot state, activation status,
or any admin-controlled migration setting.

## What Changed

- Added `/Business/Branding` behind the existing `DigitalCards.Business`
  cookie and `BusinessOnly` policy.
- Added a business sidebar/dashboard entry for `Branding Wallet`.
- Reused `ModernBusinessBranding` for:
  - public business name;
  - logo path/upload;
  - primary and secondary colors;
  - program name and description.
- Reused the existing safe logo upload pipeline:
  - files stay outside the repo;
  - served under `/uploads/business-logos/...`;
  - max size 2 MB;
  - PNG/JPG/JPEG/WebP only;
  - no SVG uploads.
- Added application and web tests for business-owned branding updates.

## Security Boundaries

- The business can update only its own `BusinessID`, taken from auth claims.
- Physical upload paths are never rendered in HTML.
- Passwords, hashes, tokens, JWTs, push tokens, connection strings and local
  certificate paths are not displayed.
- Pilot guardrails still apply: if a business is blocked from the modern flow,
  `/Business/Branding` shows the pilot block message instead of the form.

## Wallet Impact

New branding is used by existing branding-aware surfaces:

- Wallet email templates;
- `/Wallet/Select/{token}`;
- Google Wallet object/patch when the business has a public logo;
- Apple Wallet `.pkpass` generated after the branding change;
- client dashboard and card views.

Already installed Apple passes may require a Wallet update push or reinstall to
show a changed logo. Google Wallet updates on future patch operations.

## HostGator SQL

This PR requires a small compatibility change because the original branding
table recorded only admin edits:

```text
docs/migration-context/48-business-self-service-v1-hostgator.sql
```

It makes `ModernBusinessBranding.UpdatedByAdminUserID` nullable. Admin edits
still store the admin user id; business self-service edits store `NULL` there.

## Rollout

Manual smoke:

1. Login negocio en `/Business/Login`.
2. Abrir `/Business/Branding`.
3. Subir un PNG y guardar colores/nombre publico.
4. Reenviar link Wallet desde `/Business/Cards`.
5. Confirmar que el correo y landing usan el branding.
6. Emitir Apple/Google y validar visualmente el logo/color cuando aplique.
