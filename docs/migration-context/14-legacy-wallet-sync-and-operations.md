# Legacy Wallet Sync and Operations

This phase stabilizes the real Wallet flow while Web Forms still writes stamps
directly to `ClientCard`.

## Legacy Wallet Sync Worker

The modern app can optionally run a polling worker:

```json
{
  "DigitalCards": {
    "LegacyWalletSync": {
      "Enabled": true,
      "PollIntervalSeconds": 60,
      "LookbackMinutes": 1440,
      "BatchSize": 50
    }
  }
}
```

Defaults keep the worker disabled. It only runs with
`DigitalCards:PersistenceProvider=MySql`.

The worker:

- scans recent `ClientCard` changes from HostGator;
- patches Google Wallet when `ClientCard.CardIDGoogle` exists;
- pushes Apple Wallet updates when the pass has registered devices;
- never changes `ClientCard`;
- keeps only an in-memory fingerprint to avoid repeating the same sync while
  the app is running.

Because no checkpoint table is added in this phase, a restart can repeat a
recent patch. Google patch and Apple push are treated as idempotent.

## Safe Diagnostics

Enable only for controlled troubleshooting:

```json
{
  "DigitalCards": {
    "Diagnostics": {
      "EnableWalletDiagnostics": true
    }
  }
}
```

Then inspect a card without exposing secrets:

```text
GET /internal/wallet-diagnostics/{CardID}
```

The response includes stamp counts, whether Google is issued, whether Apple is
tracked, registered Apple device count, and Apple update tag. It does not return
push tokens, authentication tokens, JWTs, connection strings, certificates or
passwords.

## Operational Runbook

Before testing real Wallet updates:

1. Confirm `DigitalCards:PublicBaseUrl` is the stable public HTTPS URL.
2. Confirm MySQL, Google, SMTP and Apple providers are real in local/staging
   config outside the repo.
3. Install an Apple pass from the current public URL.
4. Confirm `AppleWalletDevice` and `AppleWalletRegistration` have rows.
5. Add a stamp from the modern app and confirm the pass updates.
6. Enable `LegacyWalletSync` only when testing stamps added by Web Forms.
7. Add a stamp from Web Forms and watch modern logs for the legacy sync summary.

If a pass does not update, check in this order:

- app health and tunnel/HTTPS reachability;
- Apple device registration count for the `CardID`;
- APNs status in logs;
- `ClientCard.LastCheck`, `CheckQTY`, `HistoricCheckQTY`;
- `AppleWalletPass.UpdatedAt` and `UpdateTag`.

Do not log or commit `.p12`, WWDR files, service account JSON, SMTP credentials,
connection strings, push tokens, auth tokens, JWTs or generated `.pkpass` files.
