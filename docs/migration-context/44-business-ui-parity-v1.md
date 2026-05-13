# PR 44: Business UI Parity v1

## Summary

This catch-up PR applies the Web Forms-inspired dashboard shell to the business
operator flow that was accidentally skipped in the PR sequence.

The goal is functional visual parity, not pixel-perfect copying. The modern app
keeps Razor Pages, cookie auth, Wallet tokens and existing services intact while
making the business screens feel closer to the original Web Forms dashboard.

## Changed Screens

- `/Business/Dashboard`
  - adds a business operation shell;
  - shows the daily flow as search, resend Wallet and stamp;
  - keeps the existing operational metrics and recent cards.
- `/Business/Cards`
  - upgrades the search surface into an operator toolbar;
  - adds a progressive QR scanner panel using the browser `BarcodeDetector`
    API when available;
  - adds a visual card face with stamp track;
  - adds Google/Apple status pills near the card actions.
- `/Business/Enroll`
  - uses the same business operation panel style;
  - keeps the existing association behavior.
- `/Business/Stamp`
  - uses the same business operation panel style;
  - remains as a quick stamp fallback while `/Business/Cards` is the primary
    operating surface.

## QR Scanner

The scanner is progressive enhancement:

- it only starts when the browser supports `BarcodeDetector` and camera access;
- it fills the existing search input and submits the existing search form;
- it does not bypass server-side authorization or business ownership checks;
- it stores no image, token or camera data.

If unsupported, the normal username/email search remains the source of truth.

## Security

- Business identity still comes from `DigitalCards.Business` claims.
- No `businessId` is exposed or trusted from forms.
- Wallet endpoints remain public-token based.
- The QR scanner only writes into the search box; it does not call Wallet,
  stamp, SMTP or MySQL directly.
- No secrets, JWTs, push tokens, passwords, certificate paths or connection
  strings are rendered.

## Validation

Automated validation:

```powershell
git diff --check
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```

Manual smoke:

1. Login negocio.
2. Open `/Business/Dashboard`.
3. Open `/Business/Cards`.
4. Search a client by username/email.
5. Confirm card face, Wallet status and stamp actions render.
6. Add a stamp and confirm Wallet updates still happen.
7. On a supported mobile browser, tap `Escanear` and confirm QR search fills
   the existing search field.

## SQL

No SQL is required.
