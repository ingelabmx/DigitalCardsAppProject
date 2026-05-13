# PR 50: Ops Service Hosting v2

## Summary

This PR turns the existing Windows helper scripts into a repeatable local
operations toolkit for `app.puntelio.com`. It does not install Windows services,
modify DNS, touch HostGator, or expose secrets.

## What Changed

- App and Cloudflare Tunnel scripts now support foreground debug mode and
  `-Background` mode.
- Background mode writes PID files to:

  ```text
  %USERPROFILE%\.digitalcards\run
  ```

- Background mode writes stdout/stderr logs to:

  ```text
  %USERPROFILE%\.digitalcards\logs
  ```

- Added stack-level scripts:
  - `start-puntelio-stack.ps1`;
  - `stop-puntelio-stack.ps1`;
  - `restart-puntelio-stack.ps1`;
  - `get-puntelio-status.ps1`;
  - `show-puntelio-logs.ps1`.

## Daily Operation

Start:

```powershell
.\ops\windows\start-puntelio-stack.ps1
```

Status:

```powershell
.\ops\windows\get-puntelio-status.ps1
```

Logs:

```powershell
.\ops\windows\show-puntelio-logs.ps1
```

Restart:

```powershell
.\ops\windows\restart-puntelio-stack.ps1
```

Stop:

```powershell
.\ops\windows\stop-puntelio-stack.ps1
```

## Safety

- Secrets remain in `%USERPROFILE%\.digitalcards\appsettings.Local.json`.
- Scripts validate JSON but do not print config content.
- Logs must be reviewed for absence of passwords, tokens, JWTs, push tokens,
  service account content, certificate paths and connection strings.
- PID files are operational state only and stay outside the repo.

## No SQL

No schema change is required.
