# Controlled Real Integrations

## Scope

This phase separates real integration switches so the modern ASP.NET Core app
can run MySQL, Google Wallet and SMTP independently.

Defaults remain fake:

```json
{
  "DigitalCards": {
    "PersistenceProvider": "InMemory",
    "GoogleWallet": {
      "Provider": "Fake"
    },
    "Email": {
      "Provider": "Fake"
    }
  }
}
```

## Local Configuration

Keep real values outside the repo in:

```text
%USERPROFILE%\.digitalcards\appsettings.Local.json
```

Use `src/DigitalCards.Web/appsettings.Local.example.json` as the shape. Do not
commit SMTP passwords, Google service account JSON, connection strings or signed
Wallet URLs.

Important keys:

- `DigitalCards:PersistenceProvider`: `InMemory` or `MySql`.
- `DigitalCards:PublicBaseUrl`: public origin used in email links.
- `DigitalCards:GoogleWallet:Provider`: `Fake` or `Google`.
- `DigitalCards:Email:Provider`: `Fake` or `Smtp`.
- `DigitalCards:Smoke:*`: business credentials and test email settings used
  only by manual smoke tests.

## SMTP

SMTP uses MailKit/MimeKit through `SmtpEmailSender`. Gmail/Google Workspace
recommended local values:

```json
{
  "DigitalCards": {
    "Email": {
      "Provider": "Smtp",
      "Host": "smtp.gmail.com",
      "Port": 587,
      "SecureSocket": "StartTls"
    }
  }
}
```

Use an app password for Gmail accounts with 2-Step Verification or a properly
configured Google Workspace SMTP relay. Do not use a normal Google password in
configuration.

## Manual Smoke Tests

Normal `dotnet test` does not call HostGator, Google Wallet or SMTP. Manual
smokes require explicit environment flags:

```powershell
$env:RUN_MYSQL_GOOGLE_SMOKE = '1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter MySqlGoogleSmoke

$env:RUN_SMTP_SMOKE = '1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter SmtpSmoke

$env:RUN_FULL_REAL_SMOKE = '1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter FullRealSmoke
```

The tests intentionally report only pass/fail state through xUnit. They do not
print SMTP passwords, service account contents or signed Google Wallet JWTs.

Smoke settings can be overridden without editing the local JSON:

```powershell
$env:DigitalCards__Smoke__BusinessEmail = 'business@example.test'
$env:DigitalCards__Smoke__BusinessPassword = 'business-password'
```
