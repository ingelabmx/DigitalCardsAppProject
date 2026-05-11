# Apple Wallet Pkpass Initial

## Scope

This phase adds an initial real Apple Wallet `.pkpass` generator without new
HostGator tables and without Apple Wallet Web Service updates or APNs.

The default remains fake:

```json
{
  "DigitalCards": {
    "AppleWallet": {
      "Provider": "Fake"
    }
  }
}
```

Real Apple Wallet is enabled only with machine-local configuration outside the
repo.

## Configuration

Required outside source control:

```json
{
  "DigitalCards": {
    "AppleWallet": {
      "Provider": "Apple",
      "TeamIdentifier": "APPLE_TEAM_ID",
      "PassTypeIdentifier": "pass.com.example.digitalcards",
      "OrganizationName": "DigitalCards",
      "CertificatePath": "C:\\Users\\eguillen\\.digitalcards\\secrets\\apple-pass-certificate.p12",
      "CertificatePassword": "APPLE_CERTIFICATE_PASSWORD",
      "WwdrCertificatePath": "C:\\Users\\eguillen\\.digitalcards\\secrets\\AppleWWDR.cer",
      "AssetsPath": "C:\\Users\\eguillen\\.digitalcards\\apple-wallet-assets"
    }
  }
}
```

`AssetsPath` must contain at least `icon.png`. Additional pass assets such as
`icon@2x.png`, `logo.png`, `logo@2x.png`, `strip.png` or localized `.lproj`
files can be added later.

Do not commit `.p12`, `.cer`, `.pem`, `.pkpass`, certificate passwords or
generated pass packages.

## Behavior

The customer still enters through `/Wallet/Select/{token}` and chooses Apple
Wallet.

- With `Provider=Fake`, `/Wallet/Apple/{token}` shows the pending Apple Wallet
  state used by CI and Playwright.
- With `Provider=Apple`, `/Wallet/Apple/{token}` redirects to
  `/Wallet/Apple/Download/{token}`.
- The download endpoint returns `application/vnd.apple.pkpass` with a deterministic
  serial number derived from the loyalty card id.

The generated pass is a `storeCard` and includes:

- business name;
- client name;
- current stamps;
- lifetime stamps;
- creation date;
- QR barcode with the client username.

## Signing

The package builder creates:

- `pass.json`;
- asset files from `AssetsPath`;
- `manifest.json` with SHA-1 hashes for all unsigned package files;
- `signature` using a detached PKCS#7 signature with the Pass Type ID
  certificate and WWDR certificate;
- final ZIP bytes served as `.pkpass`.

Errors intentionally avoid printing certificate paths or passwords.

## Manual Smoke

Run this only on a machine that has `%USERPROFILE%\.digitalcards\appsettings.Local.json`,
the Pass Type ID certificate, WWDR certificate and assets configured:

```powershell
$env:RUN_APPLE_PKPASS_SMOKE = '1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter ApplePkpassSmoke
Remove-Item Env:\RUN_APPLE_PKPASS_SMOKE -ErrorAction SilentlyContinue
```

The smoke uses `InMemory` persistence plus fake Google Wallet and fake email. It
generates a signed `.pkpass` in memory and validates the ZIP structure without
writing the package or secrets to the repo.

## Still Pending

Automatic Apple Wallet updates are implemented in the next document:
`13-apple-wallet-updates.md`. That phase adds device registrations, push tokens,
authentication token hashes, update tags, Apple Wallet Web Service endpoints and
APNs integration.
