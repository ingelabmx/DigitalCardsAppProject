# Client Profile Password and QR v1

## Summary

This PR moves client password management into `/Client/Profile`, fixes QR containment on client pages, and removes platform-specific Apple/Google counters from the client-facing dashboard and card list.

## Changes

- `/Client/Profile` now includes a `Modificar contrasena` section.
- `/Client/ChangePassword` redirects to `/Client/Profile#password`.
- The client sidebar and dashboard no longer show a separate password page link.
- `/Client/Dashboard` removes Apple/Google counters and points password changes to profile.
- `/Client/Cards` shows unified `Tarjeta lista` / `Tarjeta pendiente` state instead of platform-level Apple/Google status.
- Client QR cards are constrained with max dimensions and overflow protection for desktop and mobile.

## Scope

No SQL is required. Authentication and password hashing behavior are unchanged; only the page that hosts the password form moved.

## Validation

- `git diff --check`
- `dotnet test DigitalCardsApp.Modern.sln`
- `RUN_PLAYWRIGHT=1 dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj`
