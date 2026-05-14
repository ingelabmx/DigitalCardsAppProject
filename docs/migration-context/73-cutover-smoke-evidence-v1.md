# 73 Cutover Smoke Evidence v1

## Objetivo

Guardar evidencia operativa del ultimo smoke real por negocio antes de moverlo
a `ModernPrimary` o `LegacyRetired`.

El admin registra la evidencia desde `/Admin/Cutover`. La evidencia queda
asociada al negocio, al admin que la capturo y a la fecha del smoke.

## Alcance

- Nueva tabla `ModernCutoverSmoke`.
- Nuevo repositorio `ICutoverSmokeRepository` con implementaciones InMemory y
  MySQL.
- Nueva accion en `/Admin/Cutover` para registrar:
  - `/health` OK;
  - `/health/ready` OK;
  - correo real validado;
  - Apple Wallet instalado/actualizado;
  - Google Wallet guardado/actualizado;
  - sello moderno probado;
  - soporte revisado.
- Vista de la ultima evidencia en cada negocio de `/Admin/Cutover`.
- Evento seguro en `ModernAuditEvent`:
  `CutoverSmokeEvidenceRecorded`.

## Datos Sensibles

La evidencia no guarda tokens, JWT, push tokens, passwords, hashes,
connection strings, rutas locales ni certificados. Las notas son texto libre de
operacion y deben mantenerse sin secretos.

## SQL

Antes de usar este flujo con HostGator, aplicar manualmente:

```text
docs/migration-context/73-cutover-smoke-evidence-hostgator.sql
```

## Uso Operativo

1. Entrar a `/Admin/Login`.
2. Abrir `/Admin/Cutover`.
3. Buscar el negocio.
4. Ejecutar smoke real:
   - `https://app.puntelio.com/health`;
   - `https://app.puntelio.com/health/ready`;
   - correo real;
   - Apple Wallet en iPhone;
   - Google Wallet;
   - sello moderno;
   - revision en `/Admin/Support`.
5. Marcar los checks completados y guardar evidencia.
6. Cambiar estado a `ModernPrimary` solo si la evidencia esta completa o hay
   una decision operativa explicita.

## Rollback

Si el smoke falla, dejar el negocio en `PilotModern` o regresar a `LegacyOnly`.
Web Forms sigue vivo como fallback mientras no se marque `LegacyRetired`.
