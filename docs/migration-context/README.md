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

Notas:

- No se copiaron secretos encontrados.
- No se migro codigo funcional.
- El SQL solicitado fuera del repo no existe en esta maquina; el SQL analizado esta en `docs/db_dcards.sql`.
