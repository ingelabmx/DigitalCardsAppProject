# 61 Public Business Enrollment v1

Esta fase agrega un link publico por negocio para que clientes se registren
directamente en un programa de lealtad moderno.

## Cambios

- Nueva tabla `BusinessEnrollmentLinkToken`.
- Los tokens se guardan solo como SHA-256 y sufijo seguro.
- Admin genera/regenera link desde `/Admin/BusinessProfile/{businessId}`.
- Nueva landing publica `/Enroll/{businessToken}`.
- El cliente se registra, se asocia al negocio y recibe el correo Wallet.
- El negocio debe estar permitido por las reglas piloto/activacion existentes.

## Rollout

Aplicar manualmente en HostGator despues del merge:

```text
docs/migration-context/61-public-business-enrollment-v1-hostgator.sql
```

Luego:

1. Entrar a `/Admin/BusinessProfile/{businessId}`.
2. Generar link publico.
3. Abrir `/Enroll/{businessToken}`.
4. Registrar cliente controlado.
5. Confirmar correo real y landing Wallet.

## Seguridad

- No se guarda el token plano en base de datos.
- Al regenerar link se revocan tokens activos anteriores del negocio.
- No se muestran passwords ni hashes despues del submit.
