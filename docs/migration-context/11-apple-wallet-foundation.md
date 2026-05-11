# Apple Wallet Foundation

## Scope

This phase adds the Apple Wallet seam to the modern ASP.NET Core app without
generating production `.pkpass` files yet. The Web Forms app is unchanged and no
HostGator tables are created or altered.

The default remains safe:

```json
{
  "DigitalCards": {
    "AppleWallet": {
      "Provider": "Fake"
    }
  }
}
```

## Implementation

Application now owns an `IAppleWalletService` contract and
`DigitalCardsAppService.SelectAppleWalletAsync(token)`. The method resolves the
same card/client/business context used by Google Wallet, then delegates to the
Apple service.

Infrastructure registers `FakeAppleWalletService` by default. It returns a
pending result with no download URL. This makes the Razor Page and E2E flow
exercise the real application contract while keeping Apple production disabled.

`DigitalCards:AppleWallet:Provider=Apple` intentionally fails fast with a clear
message. This prevents accidental production activation before certificate,
package signing, web service and APNs behavior exist.

## UI Behavior

`/Wallet/Apple/{token}` is now backed by Application logic:

- valid token: shows `Apple Wallet pendiente`;
- invalid token: shows `Link no valido`;
- no `.pkpass`, JWT, certificate path or secret is emitted.

The email flow still sends the customer to `/Wallet/Select/{token}` so the user
chooses Apple Wallet or Google Wallet from the landing page.

## Production Work Still Required

Before Apple Wallet can go live:

- Apple Developer Team ID.
- Pass Type ID.
- Pass signing certificate `.p12` stored outside the repo.
- Certificate password stored outside the repo.
- Apple WWDR certificate.
- Pass package generation: `pass.json`, assets, manifest and signature.
- Download endpoint that returns `application/vnd.apple.pkpass`.
- Apple Wallet Web Service endpoints for device registration, serial updates,
  pass refresh and unregister.
- APNs integration for push updates after stamp changes.
- Persistence for serial number, authentication token hash, device library id,
  push token and last package update.
- Validation on a real iPhone.

## Tests

This phase covers:

- default DI registers `FakeAppleWalletService`;
- real Apple provider fails fast;
- selecting Apple Wallet with a valid token returns pending;
- selecting Apple Wallet with an invalid token returns `null`;
- Razor Page renders pending/not-found states;
- Playwright clicks Apple Wallet from the landing and verifies pending state.
