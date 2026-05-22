# Project Instructions for Claude Code — Puntelio v2

## Stack
C# ASP.NET Core · Razor Pages/Views · Bootstrap 5 · Bootstrap Icons
Solución activa: DigitalCardsApp.Modern.sln — NUNCA tocar DigitalCardsApp.sln

---

## Objetivo

Rediseño visual COMPLETO inspirado en Auralis (premium SaaS):
- puntelio.com → landing pública
- app.puntelio.com → dashboards (negocio + cliente) + todas las páginas internas

Lee design.md antes de cualquier cambio. Contiene el sistema completo.

---

## BACKEND — NUNCA TOCAR

- Controllers, Services, Models, Repositories
- Migraciones, stored procedures, base de datos
- Autenticación (login, logout, sesión, claims, roles)
- Business logic de ningún tipo
- Esquemas de base de datos

---

## RAZOR — NUNCA ELIMINAR NI RENOMBRAR

- asp-for, asp-action, asp-controller, asp-page, asp-route
- asp-validation-for, asp-items, asp-append-version
- name, id, method, value en forms
- Hidden inputs, data-val-*, validation summaries

---

## JAVASCRIPT — NUNCA ROMPER

- No renombrar IDs o clases usados por JS
- No eliminar script tags ni referencias a JS
- No cambiar rutas ni URLs
- No mover elementos que JS espera en cierta posición

---

## FUENTE — OBLIGATORIO en todos los layouts

En cada _Layout*.cshtml:
```html
<link href="https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;700;800&display=swap" rel="stylesheet">
```
En site.css primera regla:
```css
body { font-family: 'DM Sans', -apple-system, sans-serif; background: #EDECE8; }
```

---

## CSS — Solo en wwwroot/css/site.css

Sin inline styles. Sin nuevos archivos CSS separados.
No eliminar clases Bootstrap existentes usadas por Razor o JS.
Solo agregar clases .p-* nuevas.

---

## Orden de trabajo obligatorio

1. Leer CLAUDE.md + design.md completos
2. Explorar proyecto (sin editar)
3. Escribir site.css completo (variables + todos los componentes)
4. Agregar fuente DM Sans a layouts
5. Actualizar sidebar + topbar en todos los layouts
6. Landing page
7. Dashboard negocio
8. Dashboard cliente
9. Páginas internas
10. Responsive mobile

---

## Regla de oro

Ante duda entre funcionalidad y estética → funcionalidad primero.
Reportar cualquier riesgo antes de aplicar el cambio.

---

## Skills activas

webapp-testing · playwright-skill · web-design-guidelines · ui-ux-pro-max-skill · theme-factory · frontend-design

---

## Estructura real del proyecto (extraída del README)

```
src/DigitalCards.Web/          ← proyecto web principal
  wwwroot/css/site.css         ← ÚNICO archivo CSS a modificar
  Views/Shared/                ← layouts compartidos
  Views/Business/              ← vistas de negocio
  Views/Client/                ← vistas de cliente
  Views/Admin/                 ← vistas admin (NO tocar UI aquí)
  Pages/                       ← Razor Pages públicas
```

## Rutas reales del proyecto

```
/                              ← landing puntelio.com
/Business/Login                ← login negocio
/Business/Dashboard            ← dashboard negocio ← PRIORIDAD
/Business/Cards                ← tarjetas y sellos
/Business/Branding             ← branding wallet
/Business/Stamp                ← checadas rápidas
/Business/Enroll               ← enrolar cliente
/Business/Logout

/Client/Login                  ← login cliente
/Client/Dashboard              ← dashboard cliente ← PRIORIDAD

/Admin/Login                   ← admin (UI secundaria, no prioridad)

/Register                      ← registro público
/Enroll/{businessToken}        ← enrolamiento por negocio (con branding)
```

## Integraciones críticas — NO romper

- **Apple Wallet**: usa `app.puntelio.com` como URL base en el `.pkpass`.
  Si cambias rutas o nombres de elementos que el wallet lee, se rompen tarjetas instaladas.
- **Google Wallet**: tiene origins configurados. No cambiar URLs.
- **SMTP**: formularios de email. No tocar los form actions.

## Proyecto web (path real)

```
src\DigitalCards.Web\DigitalCards.Web.csproj
```

Ejecutar local:
```
dotnet run --project src\DigitalCards.Web\DigitalCards.Web.csproj --launch-profile http
```
