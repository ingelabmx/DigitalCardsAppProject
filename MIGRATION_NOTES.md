# Migration Notes

Indice de contexto para iniciar la migracion ordenada de `DigitalCardsApp`:

- [00 Executive Summary](docs/migration-context/00-executive-summary.md)
- [01 Current Architecture](docs/migration-context/01-current-architecture.md)
- [02 Domain Model](docs/migration-context/02-domain-model.md)
- [03 Wallet Analysis](docs/migration-context/03-wallet-analysis.md)
- [04 Migration Target Architecture](docs/migration-context/04-migration-target-architecture.md)
- [05 Migration Roadmap](docs/migration-context/05-migration-roadmap.md)
- [06 Playwright Test Plan](docs/migration-context/06-playwright-test-plan.md)
- [07 Modern ASP.NET Core Skeleton](docs/migration-context/07-modern-skeleton.md)
- [08 MySQL Persistence Adapter](docs/migration-context/08-mysql-persistence.md)
- [09 Google Wallet Service](docs/migration-context/09-google-wallet-service.md)
- [10 Controlled Real Integrations](docs/migration-context/10-controlled-real-integrations.md)
- [11 Apple Wallet Foundation](docs/migration-context/11-apple-wallet-foundation.md)
- [12 Apple Wallet Pkpass Initial](docs/migration-context/12-apple-wallet-pkpass-initial.md)
- [13 Apple Wallet Updates](docs/migration-context/13-apple-wallet-updates.md)
- [13 Apple Wallet Updates HostGator SQL](docs/migration-context/13-apple-wallet-updates-hostgator.sql)
- [14 Legacy Wallet Sync and Operations](docs/migration-context/14-legacy-wallet-sync-and-operations.md)
- [15 Business Auth Hardening](docs/migration-context/15-business-auth-hardening.md)
- [16 Business Password Hardening](docs/migration-context/16-business-password-hardening.md)
- [16 Business Password Hardening HostGator SQL](docs/migration-context/16-business-password-hardening-hostgator.sql)
- [17 Puntelio Single Environment](docs/migration-context/17-puntelio-single-environment.md)
- [Secret Rotation Notes](docs/security/SECRET_ROTATION.md)

Esta fase agrega contexto documental y un esqueleto ASP.NET Core paralelo. No reemplaza todavia la aplicacion Web Forms. MySQL HostGator, Google Wallet real, SMTP real, Apple Wallet `.pkpass` y Apple Wallet updates por Web Service/APNs quedan disponibles por configuracion local controlada; los fakes siguen siendo el default para desarrollo, CI y Playwright. El flujo moderno de negocio ahora usa cookie auth, ya no depende de `businessId` visible en URL/formularios y migra passwords de negocio a hashes modernos sin modificar la columna legacy.
