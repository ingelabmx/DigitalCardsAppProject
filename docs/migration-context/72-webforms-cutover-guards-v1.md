# 72 Web Forms Cutover Guards v1

## Objetivo

Agregar los primeros guardrails reales en Web Forms para respetar el estado moderno por negocio sin apagar Web Forms globalmente.

## Cambios

- Web Forms lee `ModernPilotBusiness.ActivationStatus` por `BusinessID`.
- Si el negocio esta en `LegacyRetired`, el login legacy de negocio se bloquea y las paginas legacy redirigen a `NotAuthorized.aspx?modern=legacy-retired`.
- Si el negocio esta en `ModernPrimary`, Web Forms sigue funcionando como fallback, pero muestra aviso para usar `app.puntelio.com`.
- Si la tabla moderna no existe o hay error de lectura, Web Forms conserva comportamiento legacy (`LegacyOnly`).
- Se agrega `ModernAppUrl` en `Web.config` con default `https://app.puntelio.com`.

## Paginas Cubiertas

- `BusinessLogin.aspx`
- `BusinessDashboardPage.aspx`
- `BusinessInsertionPage.aspx`
- `BusinessCheckPage.aspx`
- `NotAuthorized.aspx`

## Seguridad

- No se imprimen connection strings ni detalles SQL.
- No se cambia login admin/cliente legacy.
- No se cambian stored procedures existentes.
- No hay SQL nuevo; usa `ModernPilotBusiness.ActivationStatus` de PR 38.

## Operacion

Smoke recomendado despues del merge:

1. Poner un negocio en `ModernPrimary` desde `/Admin/Cutover`.
2. Entrar a Web Forms con ese negocio.
3. Confirmar aviso amarillo y que el fallback legacy sigue operando.
4. Poner el negocio en `LegacyRetired`.
5. Confirmar que Web Forms bloquea login/entrada legacy y muestra mensaje para usar `app.puntelio.com`.
6. Regresar a `ModernPrimary` o `PilotModern` si se necesita fallback.

## Rollback

Cambiar el negocio a `ModernPrimary`, `PilotModern` o `LegacyOnly` desde la app moderna. Si la tabla moderna falla, Web Forms vuelve a comportamiento legacy por diseno.
