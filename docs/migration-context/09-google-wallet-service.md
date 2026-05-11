# Google Wallet Service

## Scope

This phase adds a real Google Wallet adapter behind the existing
`IGoogleWalletService` abstraction. It does not replace the Web Forms app and it
does not enable Google Wallet in production by default.

The default remains safe:

```json
{
  "DigitalCards": {
    "GoogleWallet": {
      "Provider": "Fake"
    }
  }
}
```

When `DigitalCards:GoogleWallet:Provider` is `Fake`,
`FakeGoogleWalletService` is used by local development, smoke tests and
Playwright flows.

## Implementation

The real adapter lives in `DigitalCards.Infrastructure.Wallets.GoogleWalletService`.
It uses the Google Wallet Objects API and service account signing to:

- ensure the Generic Pass class exists for a business;
- ensure the Generic Pass object exists for a customer card;
- generate a signed `https://pay.google.com/gp/v/save/<signed_jwt>` link;
- patch the object's stamp text modules when the business adds a stamp.

The adapter intentionally creates the class and object through the API before
issuing the save link. The signed JWT then references the existing object, which
keeps the link shorter and avoids embedding the full pass payload in email.

## Configuration

Required outside source control. The modern app loads this machine-local file
automatically:

```text
%USERPROFILE%\.digitalcards\appsettings.Local.json
```

Example:

```json
{
  "DigitalCards": {
    "PublicBaseUrl": "https://algo.trycloudflare.com",
    "GoogleWallet": {
      "Provider": "Google",
      "IssuerId": "3388000000023127519",
      "Origins": [
        "https://algo.trycloudflare.com"
      ],
      "CredentialsFilePath": "C:\\Users\\eguillen\\.digitalcards\\secrets\\google-wallet-service-account.json"
    }
  }
}
```

The repo also includes `src/DigitalCards.Web/appsettings.Local.example.json`.
Copy its shape, but keep the real local file out of git.

Do not commit the service account JSON. Store it outside the repository and
rotate any key that was previously committed or copied into source control.
The service account path must come from `DigitalCards:GoogleWallet:CredentialsFilePath`;
the modern app intentionally does not depend on `GOOGLE_APPLICATION_CREDENTIALS`.

Optional settings:

- `DigitalCards:GoogleWallet:Provider`
- `DigitalCards:GoogleWallet:ApplicationName`
- `DigitalCards:GoogleWallet:Language`
- `DigitalCards:GoogleWallet:HexBackgroundColor`
- `DigitalCards:GoogleWallet:HeroImageUri`
- `DigitalCards:GoogleWallet:LogoImageUri`

## Legacy Table Compatibility

The existing `ClientCard.CardIDGoogle` column stores a short Google object
suffix, not the full Google Wallet object id and not the signed save URL. The
real service therefore returns the suffix as `GoogleWalletIssueResult.ObjectId`
and builds the full id internally as:

```text
{issuerId}.{objectSuffix}
```

The signed save URL is generated on demand. It is not persisted in the legacy
table because the current schema has no safe column for it.

## What Is Still Needed

- A Google Wallet issuer id with publishing/demo access configured in Google
  Wallet Business Console.
- A rotated Google Cloud service account JSON authorized for Wallet Objects.
- A public HTTPS origin for production save links.
- Public image URLs for the card logo and hero image.
- A real smoke test using a non-production Google Wallet issuer before enabling
  `DigitalCards:GoogleWallet:Provider=Google` outside local development.

## Smoke Verification

On 2026-05-11, a local smoke test was executed against the configured Google
Wallet issuer using `DigitalCards:GoogleWallet:Provider=Google`, in-memory
persistence and the fake email outbox. The test confirmed:

- client registration and business enrollment still work without HostGator;
- Google Wallet class/object creation succeeds;
- a `https://pay.google.com/gp/v/save/...` URL is generated;
- adding a stamp patches the Google Wallet object successfully;
- no signed JWT, service account JSON or private key was committed or copied
  into this document.

## References

- Google Wallet web/email issue flow:
  `https://developers.google.com/wallet/generic/web`
- Google Wallet JWT flow:
  `https://developers.google.com/wallet/generic/use-cases/jwt`
- Google Wallet Generic Class insert:
  `https://developers.google.com/wallet/reference/rest/v1/genericclass/insert`
- Google Wallet Generic Object patch:
  `https://developers.google.com/wallet/reference/rest/v1/genericobject/patch`
