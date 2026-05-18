# 88 Brand Consistency v1

## Objetivo

Unificar la identidad visual moderna sin copiar el bundle completo de Web Forms. La meta sigue siendo paridad funcional: Puntelio/DigitalCards debe sentirse consistente en paginas publicas, admin, negocio, cliente y Wallet.

## Tokens visuales

La hoja `wwwroot/css/site.css` define tokens CSS base:

- `--dc-primary`: azul principal de acciones.
- `--dc-primary-dark`: estado hover/focus.
- `--dc-accent`: apoyo para elementos informativos.
- `--dc-ink`: texto principal del dashboard.
- `--dc-muted`: texto secundario.
- `--dc-soft`: fondos de paneles suaves.
- `--dc-border`: bordes de cards/tablas.
- `--dc-radius`: radio base de 8px.

## Cambios

- El nav publico usa un lockup visual de Puntelio con marca compacta.
- El shell autenticado conserva la marca DigitalCards y footer `Propiedad de Ingelab`.
- Botones primarios y outline usan los mismos tokens que el sidebar y cards.
- Cards, metricas, sidebar y header empiezan a consumir tokens comunes.
- Se agrega smoke test para asegurar presencia de marca, footer y tokens CSS.

## Alcance

- Sin cambios funcionales.
- Sin cambios de base de datos.
- Sin copiar assets legacy completos.
- La personalizacion por negocio sigue viviendo en branding de negocio y Wallet.

## Validacion

```powershell
git diff --check
dotnet test tests\DigitalCards.Web.Tests\DigitalCards.Web.Tests.csproj --filter BrandConsistency_RendersSharedBrandTokensAndFooters
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
