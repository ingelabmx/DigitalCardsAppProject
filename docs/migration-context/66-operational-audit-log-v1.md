# Operational Audit Log v1

Este PR agrega auditoria operacional para acciones sensibles del flujo moderno.

## Alcance

- Nueva tabla `ModernAuditEvent`.
- Nuevo repositorio `IAuditEventRepository` con implementacion InMemory/MySQL.
- Nueva pagina admin `/Admin/Audit`.
- Registro de eventos para:
  - creacion de admins;
  - reset de password admin;
  - creacion/edicion de negocios;
  - reset de password de negocio;
  - cambios de branding;
  - cambios piloto/cutover;
  - cambios piloto de cliente;
  - generacion de links publicos por negocio;
  - exports de soporte;
  - reintentos Wallet desde soporte.

## Seguridad

La auditoria no guarda passwords, hashes, tokens Wallet, JWTs, push tokens, connection strings, rutas locales ni certificados. Los eventos guardan IDs legacy/modernos y resumen seguro de operacion.

## Rollout

Aplicar manualmente en HostGator:

```text
docs/migration-context/66-operational-audit-log-hostgator.sql
```

Despues validar:

- entrar a `/Admin/Login`;
- crear o editar un negocio test;
- abrir `/Admin/Audit`;
- confirmar evento visible;
- exportar soporte y confirmar evento `SupportExported`.
