# Migration Context

Esta carpeta contiene el contexto tecnico inicial para migrar `DigitalCardsApp` desde ASP.NET Web Forms/.NET Framework 4.8 hacia ASP.NET Core moderno.

Documentos:

- `00-executive-summary.md`: resumen ejecutivo y primer PR recomendado.
- `01-current-architecture.md`: arquitectura actual, dependencias, base de datos, correo y riesgos.
- `02-domain-model.md`: entidades, tablas, stored procedures y modelo de dominio recomendado.
- `03-wallet-analysis.md`: estado de Google Wallet, Apple Wallet y recomendaciones.
- `04-migration-target-architecture.md`: arquitectura destino propuesta en ASP.NET Core.
- `05-migration-roadmap.md`: fases incrementales de migracion.
- `06-playwright-test-plan.md`: plan de pruebas end-to-end.
- `07-modern-skeleton.md`: esqueleto ASP.NET Core paralelo.
- `08-mysql-persistence.md`: adapter MySQL para tablas legacy.
- `09-google-wallet-service.md`: adapter real de Google Wallet y configuracion.
- `10-controlled-real-integrations.md`: providers separados para MySQL, Google Wallet y SMTP real.
- `11-apple-wallet-foundation.md`: contrato y fake inicial para Apple Wallet.
- `12-apple-wallet-pkpass-initial.md`: descarga inicial `.pkpass` firmada sin tablas nuevas.
- `13-apple-wallet-updates.md`: Apple Wallet Web Service, APNs y updates reales.
- `13-apple-wallet-updates-hostgator.sql`: tablas nuevas para updates Apple en HostGator.
- `14-legacy-wallet-sync-and-operations.md`: worker opcional para sellos legacy y runbook operativo.
- `15-business-auth-hardening.md`: autenticacion cookie de negocio y proteccion de paginas modernas.
- `16-business-password-hardening.md`: migracion progresiva de passwords de negocio.
- `16-business-password-hardening-hostgator.sql`: tabla nueva para hashes modernos de negocio.
- `17-puntelio-single-environment.md`: configuracion unica con `app.puntelio.com`, Cloudflare Tunnel y Wallets.
- `18-pilot-readiness.md`: guardrails de piloto para usar `app.puntelio.com` con datos reales.
- `19-production-readiness.md`: health checks, proxy, Data Protection y operacion estable.
- `19-business-operations-v1.md`: busqueda y operacion moderna de tarjetas por negocio.
- `20-wallet-link-token-hardening.md`: links Wallet opacos y compatibilidad legacy temporal.
- `20-wallet-link-token-hardening-hostgator.sql`: tabla nueva para hashes de tokens Wallet.
- `21-stamp-ledger-v1.md`: auditoria operacional de sellos modernos y legacy sync.
- `21-stamp-ledger-v1-hostgator.sql`: tabla nueva para auditoria de sellos.
- `22-admin-pilot-management-v1.md`: administracion de negocios piloto desde UI admin.
- `22-admin-pilot-management-hostgator.sql`: tabla nueva para negocios piloto.
- `23-admin-business-registration-v1.md`: creacion de negocios desde UI admin.
- `24-admin-business-management-v1.md`: edicion/reset de negocios desde UI admin.
- `25-admin-access-management-v1.md`: creacion/reset de admins sin registro publico.
- `25-admin-access-management-hostgator.sql`: tabla nueva para hashes modernos de admin.
- `26-client-pilot-management-v1.md`: administracion de clientes piloto desde UI admin.
- `26-client-pilot-management-hostgator.sql`: tabla nueva para clientes piloto.
- `27-client-dashboard-v1.md`: login/dashboard moderno de cliente con cookie propia.

Notas:

- No se copiaron secretos encontrados.
- No se migro codigo funcional.
- El SQL solicitado fuera del repo no existe en esta maquina; el SQL analizado esta en `docs/db_dcards.sql`.
