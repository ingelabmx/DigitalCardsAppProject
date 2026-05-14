# Public Enrollment Consent v1

Este PR agrega aceptacion obligatoria de terminos/privacidad para registro publico.

## Alcance

- `/Register` requiere aceptar terminos y privacidad.
- `/Enroll/{businessToken}` requiere aceptar terminos y privacidad.
- Nueva tabla `ModernClientConsent`.
- Nuevo repositorio `IClientConsentRepository` con implementacion InMemory/MySQL.
- Nueva pagina `/Privacy` con texto operativo basico.

## Datos guardados

- `UserID`;
- `BusinessID` opcional;
- version de politica;
- origen (`Register` o `PublicBusinessEnrollment`);
- fecha de aceptacion.

No se guardan passwords, tokens Wallet, CardID publico, JWTs, push tokens ni secretos.

## Rollout

Aplicar manualmente en HostGator:

```text
docs/migration-context/69-public-enrollment-consent-hostgator.sql
```
