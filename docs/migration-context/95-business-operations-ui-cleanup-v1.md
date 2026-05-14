# Business Operations UI Cleanup v1

## Summary

This PR cleans the visible business workflow without changing persistence or Wallet behavior. The business-facing UI now treats Apple and Google as one operational concept: the customer's digital card. Platform-specific wording remains for the public Wallet install pages only.

## Changes

- `/Business/Dashboard` no longer shows separate Google Wallet, Apple Wallet, or Wallet alert counters.
- Recent stamp events on the dashboard no longer expose source labels such as `LegacySync` or platform-level `Google: OK` / `Apple: OK` details.
- Recent stamps now show client, date/time, current stamp count, and a simple `Actualizado` / `Con alerta` state.
- `/Business/Cards` uses a more compact search row and a wider search button.
- `/Business/Cards` no longer shows `G` / `A` platform chips, Apple device counts, or separate Google/Apple status cards.
- The card detail uses a unified `Tarjeta lista` / `Tarjeta pendiente` status.
- The previous `Administrar` jump button was removed; resend, stamp, deactivate/reactivate, and delete actions are visible directly in the detail.
- The delete action is presented as a clearer danger panel.
- `/Business/Stamp` shows `Tarjeta` instead of platform-specific Wallet text and clears the search input after a successful stamp.

## Scope

No SQL is required. No real Wallet provider behavior changed. This is a UI and test cleanup PR for the business workflow.

## Validation

- `git diff --check`
- `dotnet test DigitalCardsApp.Modern.sln`
- `RUN_PLAYWRIGHT=1 dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj`
