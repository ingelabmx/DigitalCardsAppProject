# Puntelio — Design System v2
# Inspirado en: Auralis (stitch.withgoogle.com preview)
# Estilo: Premium SaaS · Warm Off-White · Tipografía Bold Ultra · Botones Pill

> Claude DEBE leer este archivo completo antes de modificar cualquier UI.
> Aplica a: puntelio.com (landing) y app.puntelio.com (dashboards).

---

## Filosofía visual

Extraído pixel a pixel del sitio de referencia Auralis:

1. **Fondo cálido** — no blanco puro, sino un warm off-white (#EDECE8). Toda la página.
2. **Tipografía protagonista** — headings enormes, peso 800, izquierda-alineados en hero.
3. **Botones pill** — border-radius 999px en TODOS los botones. Sin excepción.
4. **Blobs de color** — los únicos colores vivos van DENTRO de feature cards como gradientes decorativos suaves. No en fondos de página.
5. **Cards blancas** — fondo #FFFFFF, borde sutil, radius grande (~20px).
6. **Espaciado brutal** — secciones con 100px+ de padding vertical.
7. **Navbar minimalista** — blanco, borde inferior fino, logo bold izquierda, CTA pill derecha.
8. **Hero asimétrico** — heading enorme izquierda + descripción derecha (no centrado).

---

## Fuente — DM Sans

```html
<link href="https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;700;800&display=swap" rel="stylesheet">
```

```css
font-family: 'DM Sans', -apple-system, BlinkMacSystemFont, sans-serif;
```

### Escala tipográfica
```
--text-xs:      11px / 600 / uppercase / tracking 0.08em   → labels de categoría, "TRUSTED BY"
--text-sm:      13px / 400                                  → metadata, timestamps, footer
--text-base:    15px / 400 / line-height 1.6                → body text
--text-md:      17px / 500                                  → subtítulos, descripciones de hero
--text-lg:      22px / 700                                  → títulos de sección, card titles
--text-xl:      32px / 800                                  → page headings en dashboard
--text-hero:    64px / 800 / line-height 1.0                → hero principal (desktop)
--text-hero-sm: 42px / 800 / line-height 1.05               → hero mobile
--text-section: 52px / 800 / line-height 1.08 / text-center → headings de sección centrados
--text-stat:    40px / 800 / line-height 1                  → números grandes
```

---

## Paleta de colores

### Fondos (de la referencia Auralis)
```css
--bg-page:      #EDECE8;    /* warm off-white — fondo de TODA la página y secciones */
--bg-surface:   #FFFFFF;    /* cards, navbar, sidebar, panels */
--bg-subtle:    #E8E7E3;    /* hover de items, fondos inactivos */
--bg-pill:      #E4E3DF;    /* eyebrow pills, nav item hover */
```

### Texto
```css
--text-primary:   #0A0A0A;    /* headings, texto principal — casi negro */
--text-secondary: #6B6B6B;    /* body descriptions, subtítulos */
--text-muted:     #9B9B9B;    /* timestamps, labels terciarios, footer */
--text-on-dark:   #FFFFFF;    /* texto sobre fondo oscuro (#0A0A0A) */
```

### Bordes
```css
--border:         #D8D7D3;    /* borde de cards, navbar, inputs */
--border-subtle:  #E4E3DF;    /* borde muy sutil, separadores */
```

### Azul Puntelio (acento — solo en CTA y momentos clave)
```css
--blue:           #1D6AF5;
--blue-dark:      #1553C7;
--blue-light:     #EBF2FF;
--blue-pill-bg:   #DBEAFE;
```

### Semánticos (solo para estados funcionales)
```css
--green:    #16A34A;   --green-bg:  #F0FDF4;   --green-border: #BBF7D0;
--amber:    #D97706;   --amber-bg:  #FFFBEB;   --amber-border: #FDE68A;
--red:      #DC2626;   --red-bg:    #FEF2F2;   --red-border:   #FECACA;
```

### Blobs decorativos (solo dentro de feature cards)
```css
/* Blob azul-púrpura */
background: radial-gradient(ellipse at 30% 50%, rgba(147,197,253,0.6) 0%, rgba(196,181,253,0.5) 40%, transparent 70%);

/* Blob rojo-naranja */
background: radial-gradient(ellipse at 40% 40%, rgba(252,165,165,0.7) 0%, rgba(251,146,60,0.5) 40%, transparent 70%);

/* Blob verde-menta */
background: radial-gradient(ellipse at 50% 50%, rgba(167,243,208,0.6) 0%, rgba(147,197,253,0.4) 50%, transparent 70%);

/* Blob lavanda */
background: radial-gradient(ellipse at 50% 50%, rgba(221,214,254,0.7) 0%, rgba(196,181,253,0.5) 40%, transparent 70%);
```

### Stamps (sellos)
```css
--stamp-on:     #F59E0B;
--stamp-off:    #D8D7D3;
--stamp-border: #D97706;
```

---

## Espaciado

```css
--space-2:   8px;    --space-3:  12px;  --space-4:  16px;
--space-5:   20px;   --space-6:  24px;  --space-8:  32px;
--space-10:  40px;   --space-12: 48px;  --space-16: 64px;
--space-20:  80px;   --space-24: 96px;  --space-32: 128px;
```

---

## Bordes y sombras

```css
--radius-sm:   8px;    /* badges, small pills */
--radius-md:   12px;   /* inputs */
--radius-lg:   18px;   /* cards estándar */
--radius-xl:   24px;   /* cards grandes, modales */
--radius-pill: 999px;  /* TODOS los botones y eyebrow pills */

--shadow-card: 0 1px 2px rgba(0,0,0,0.06), 0 1px 3px rgba(0,0,0,0.04);
--shadow-md:   0 4px 12px rgba(0,0,0,0.07), 0 2px 4px rgba(0,0,0,0.04);
--shadow-lg:   0 8px 24px rgba(0,0,0,0.10), 0 4px 8px rgba(0,0,0,0.05);
```

---

## Componentes

### Botones — TODOS con border-radius: 999px (pill)

```css
/* Base de todos los botones */
.p-btn {
  display: inline-flex; align-items: center; gap: 8px;
  border-radius: 999px;              /* SIEMPRE pill */
  font-family: 'DM Sans', sans-serif;
  font-weight: 700;
  cursor: pointer; border: none;
  transition: all 0.15s ease;
  white-space: nowrap; text-decoration: none;
}
/* Negro — CTA principal (como "Sign up", "Get started") */
.p-btn-primary {
  background: #0A0A0A; color: #FFFFFF;
  padding: 12px 24px; font-size: 15px;
}
.p-btn-primary:hover { background: #2A2A2A; }

/* Azul Puntelio — acciones de marca */
.p-btn-blue {
  background: #1D6AF5; color: #FFFFFF;
  padding: 12px 24px; font-size: 15px;
}
.p-btn-blue:hover { background: #1553C7; }

/* Outline — CTA secundario (como "Contact sales", "Talk to sales") */
.p-btn-outline {
  background: transparent; color: #0A0A0A;
  border: 1.5px solid #D8D7D3;
  padding: 11px 24px; font-size: 15px;
}
.p-btn-outline:hover { background: #E4E3DF; }

/* Ghost */
.p-btn-ghost {
  background: transparent; color: #6B6B6B;
  padding: 10px 18px; font-size: 14px;
}
.p-btn-ghost:hover { background: #E4E3DF; color: #0A0A0A; }

/* Tamaños */
.p-btn-sm { padding: 8px 18px; font-size: 13px; }
.p-btn-lg { padding: 15px 32px; font-size: 16px; }
```

### Eyebrow pills (etiquetas sobre headings)

```css
.p-eyebrow {
  display: inline-block;
  background: #E4E3DF;
  color: #6B6B6B;
  font-size: 13px; font-weight: 500;
  padding: 5px 14px;
  border-radius: 999px;
  margin-bottom: 20px;
}
/* Variante con punto de color */
.p-eyebrow-dot::before {
  content: ''; display: inline-block;
  width: 6px; height: 6px; border-radius: 50%;
  background: #16A34A; margin-right: 8px;
  vertical-align: middle;
}
```

### Cards

```css
.p-card {
  background: #FFFFFF;
  border: 1px solid #D8D7D3;
  border-radius: 18px;
  padding: 28px;
  box-shadow: var(--shadow-card);
}
.p-card-flush {           /* sin padding — para tablas */
  background: #FFFFFF;
  border: 1px solid #D8D7D3;
  border-radius: 18px;
  overflow: hidden;
  box-shadow: var(--shadow-card);
}
.p-card-header {
  padding: 18px 24px;
  border-bottom: 1px solid #D8D7D3;
  display: flex; align-items: center;
  justify-content: space-between;
}
/* Feature card con blob decorativo (landing) */
.p-feature-card {
  background: #FFFFFF;
  border: 1px solid #D8D7D3;
  border-radius: 20px;
  overflow: hidden;
  box-shadow: var(--shadow-card);
}
.p-feature-card .blob-area {
  height: 240px; position: relative;
  background: #F5F4F0;  /* fallback */
}
.p-feature-card .card-label {
  position: absolute; top: 16px; left: 16px;
  background: rgba(255,255,255,0.85);
  backdrop-filter: blur(8px);
  padding: 5px 14px; border-radius: 999px;
  font-size: 13px; font-weight: 500; color: #0A0A0A;
}
.p-feature-card .card-body {
  padding: 20px 24px;
}
/* Mini feature card (4-col grid) */
.p-mini-card {
  background: #E8E7E3;
  border: 1px solid #D8D7D3;
  border-radius: 16px;
  padding: 24px 20px;
}
.p-mini-card .mini-icon {
  font-size: 22px; color: #0A0A0A;
  margin-bottom: 12px;
}
.p-mini-card .mini-label {
  font-size: 14px; font-weight: 600; color: #0A0A0A;
}
```

### Stat cards (dashboard)

```css
.p-stat {
  background: #FFFFFF;
  border: 1px solid #D8D7D3;
  border-radius: 18px;
  padding: 24px 28px;
  box-shadow: var(--shadow-card);
}
.p-stat-label {
  font-size: 11px; font-weight: 600;
  text-transform: uppercase; letter-spacing: 0.08em;
  color: #9B9B9B; margin-bottom: 14px;
  display: flex; align-items: center; gap: 8px;
}
.p-stat-value {
  font-size: 40px; font-weight: 800;
  color: #0A0A0A; line-height: 1;
}
.p-stat-sub {
  font-size: 13px; color: #9B9B9B; margin-top: 8px;
}
.p-stat-icon {
  width: 30px; height: 30px; border-radius: 8px;
  display: inline-flex; align-items: center;
  justify-content: center; font-size: 15px;
}
.p-stat-icon.blue   { background: #EBF2FF; color: #1D6AF5; }
.p-stat-icon.amber  { background: #FEF3C7; color: #D97706; }
.p-stat-icon.green  { background: #F0FDF4; color: #16A34A; }
```

### Badges

```css
.p-badge {
  display: inline-flex; align-items: center; gap: 5px;
  padding: 3px 10px; border-radius: 999px;
  font-size: 12px; font-weight: 600;
}
.p-badge-green  { background: #F0FDF4; color: #15803D; border: 1px solid #BBF7D0; }
.p-badge-amber  { background: #FFFBEB; color: #B45309; border: 1px solid #FDE68A; }
.p-badge-red    { background: #FEF2F2; color: #B91C1C; border: 1px solid #FECACA; }
.p-badge-blue   { background: #EFF6FF; color: #1D4ED8; border: 1px solid #BFDBFE; }
.p-badge-gray   { background: #E4E3DF; color: #6B6B6B; border: 1px solid #D8D7D3; }
.p-badge-black  { background: #0A0A0A; color: #FFFFFF; }
```

### Flow steps

```css
.p-step {
  background: #FFFFFF;
  border: 1px solid #D8D7D3;
  border-radius: 18px;
  padding: 24px;
  position: relative; overflow: hidden;
  transition: all 0.2s ease;
  cursor: default;
}
.p-step:hover {
  box-shadow: var(--shadow-lg);
  transform: translateY(-4px);
}
.p-step-num {
  font-size: 11px; font-weight: 700;
  color: #9B9B9B; letter-spacing: 0.06em;
  margin-bottom: 12px;
}
.p-step-title { font-size: 16px; font-weight: 800; color: #0A0A0A; margin-bottom: 6px; }
.p-step-desc  { font-size: 14px; color: #6B6B6B; }
.p-step-icon  {
  position: absolute; right: 20px; top: 20px;
  font-size: 22px; color: #D8D7D3;
}
```

### Stamps

```css
.p-stamps { display: flex; flex-wrap: wrap; gap: 8px; margin: 14px 0; }
.p-stamp {
  width: 26px; height: 26px; border-radius: 50%;
  background: #D8D7D3; border: 2px solid #D8D7D3;
  transition: all 0.2s;
}
.p-stamp.on {
  background: #F59E0B; border-color: #D97706;
  box-shadow: 0 0 0 3px rgba(245,158,11,0.15);
}
```

### Tablas

```css
.p-table { width: 100%; border-collapse: collapse; }
.p-table th {
  font-size: 11px; font-weight: 700; text-transform: uppercase;
  letter-spacing: 0.08em; color: #9B9B9B;
  padding: 12px 20px; text-align: left;
  border-bottom: 1px solid #D8D7D3;
}
.p-table td {
  padding: 14px 20px; font-size: 14px; color: #0A0A0A;
  border-bottom: 1px solid #E4E3DF; vertical-align: middle;
}
.p-table tr:last-child td { border-bottom: none; }
.p-table tr:hover td { background: #F8F7F3; }
.p-table .td-muted { color: #9B9B9B; font-size: 13px; }
```

### Inputs

```css
.p-input {
  width: 100%; padding: 11px 16px;
  border: 1.5px solid #D8D7D3; border-radius: 12px;
  font-size: 14px; font-family: 'DM Sans', sans-serif;
  color: #0A0A0A; background: #FFFFFF; outline: none;
  transition: border-color 0.15s;
}
.p-input:focus { border-color: #0A0A0A; }
.p-input::placeholder { color: #C4C3BF; }
```

### Empty state

```css
.p-empty { text-align: center; padding: 60px 24px; }
.p-empty-icon  { font-size: 40px; color: #D8D7D3; margin-bottom: 16px; }
.p-empty-title { font-size: 17px; font-weight: 800; color: #0A0A0A; margin-bottom: 8px; }
.p-empty-desc  { font-size: 14px; color: #9B9B9B; max-width: 280px; margin: 0 auto; }
```

---

## Layout — Landing page (puntelio.com)

### Navbar (extraído de Auralis)
```
Fondo:        #FFFFFF
Border-bottom: 1px solid #D8D7D3
Height:        60px
Padding:       0 40px
Layout:        logo bold izquierda | links centro | CTA derecha

Logo:          "puntelio" — font 18px / 800 / color #0A0A0A
               (los 3 círculos de marca a la izquierda del texto)

Nav links:     font 14px / 500 / color #6B6B6B — hover: #0A0A0A
               sin subrayado, sin background en hover

CTA:           botón pill negro .p-btn-primary.p-btn-sm → "Empezar gratis"
               + opcional: link "Entrar" en ghost antes del botón

Sticky:        position: sticky; top: 0; z-index: 100;
               backdrop-filter: blur(12px) al hacer scroll (JS class)
               background: rgba(255,255,255,0.92) cuando sticky
```

### Hero section (estilo Auralis — ASIMÉTRICO)
```
Fondo:           var(--bg-page) → #EDECE8
Padding:         100px 40px 80px
Max-width:       1200px centrado

Layout:          CSS Grid — 2 columnas: 55% heading | 45% description
                 (NO centrado — izquierda-alineado como Auralis)

Columna izquierda:
  Eyebrow:       .p-eyebrow → "Tarjetas de lealtad digitales"
  Heading:       64px / 800 / color #0A0A0A / line-height 1.0
                 Texto: "Lealtad digital para negocios y clientes."
                 Romper en 2-3 líneas naturales
  CTAs:          fila con gap 12px — margin-top: 36px
                 Primario:   .p-btn-primary.p-btn-lg → "Empezar gratis"
                 Secundario: .p-btn-outline.p-btn-lg → "Hablar con ventas"

Columna derecha:
  Alineación:    flex-end en desktop, padding-top para alignar con texto
  Descripción:   17px / 400 / color #6B6B6B / line-height 1.65
                 max-width: 380px
```

### Feature cards bajo el hero (estilo Auralis — 2 cols grandes)
```
Fondo de sección: var(--bg-page)
Padding:          0 40px 100px
Grid:             2 columnas con gap 20px

Card izquierda (grande, con blob):
  .p-feature-card
  blob-area: 280px height
  Blob:    radial-gradient azul-púrpura (como Auralis "Omnichannel agents")
  Label pill en top-left: "Para el negocio"
  Card body:
    Título: 18px / 800
    Desc:   14px / color #6B6B6B

Card derecha (grande, con blob):
  .p-feature-card
  blob-area: 280px height, blob suave gris/lavanda
  Label pill: "Para el cliente"
  Card body:
    Título: 18px / 800
    Desc:   14px / color #6B6B6B
```

### Mini feature grid (4 cols — estilo Auralis)
```
Fondo:   var(--bg-page)
Padding: 0 40px 80px
Grid:    4 columnas con gap 16px

Cada .p-mini-card:
  ícono Bootstrap Icon grande (bi-qr-code, bi-star, bi-credit-card-2-front, bi-graph-up)
  label: 14px / 600 / color #0A0A0A
```

### "Trusted by" strip (logo bar)
```
Fondo:   #EDECE8
Padding: 40px
Border-y: 1px solid #D8D7D3

Label:   "CONFIADO POR NEGOCIOS DE TODO TIPO" — 11px / 600 / uppercase / tracking / centrado
Logos:   flex row centrado, gap 40px, font 14px / 700 / color #6B6B6B / uppercase
         (texto placeholder de nombres de negocios si no hay logos reales)
```

### Sección CTA central (con heading enorme centrado)
```
Fondo:    var(--bg-page)
Padding:  100px 40px
Centrado

Heading:  52px / 800 / centrado
          "Un flujo para el mostrador. Otro para el cliente."
Sub:      17px / 400 / color #6B6B6B / centrado / max-width 560px
Botones:  centrados, gap 12px — primary + outline
```

### CTA final oscura
```
Fondo:    #0A0A0A
Padding:  80px 40px
Centrado

Heading:  42px / 800 / color #FFFFFF
Sub:      16px / 400 / color #9B9B9B
Botón:    .p-btn-outline + override: border-color #FFFFFF; color #FFFFFF
          hover: background #FFFFFF; color #0A0A0A
```

### Footer
```
Fondo:       #FFFFFF
Border-top:  1px solid #D8D7D3
Padding:     28px 40px
Layout:      logo + copyright izquierda | links derecha
Font:        13px / 400 / color #9B9B9B
Links:       Twitter, GitHub, Terms, Privacy (o equivalentes)
```

---

## Layout — Sidebar (dashboards)

```
Ancho:        220px fijo
Fondo:        #FFFFFF
Border-right: 1px solid #D8D7D3
Padding:      20px 12px

Logo zona:    padding 12px 12px 28px
              "puntelio" — 18px / 800 / color #0A0A0A

Sección label:
  font: 11px / 600 / uppercase / tracking 0.08em / color #9B9B9B
  margin: 20px 0 6px 10px

Nav item:
  padding: 9px 14px / border-radius: 999px / margin: 1px 0
  font: 14px / 500 / color #6B6B6B
  ícono bi-* 16px + gap 10px
  transition: 0.12s

Nav activo:   background: #0A0A0A / color: #FFFFFF / font-weight: 700
Nav hover:    background: #E4E3DF / color: #0A0A0A

Bottom:       Cerrar sesión separado por border-top #D8D7D3
```

### Topbar

```
Height:       56px
Fondo:        #FFFFFF
Border-bottom: 1px solid #D8D7D3
Padding:      0 32px

Derecha:
  Pill usuario: background #E4E3DF / border-radius 999px / padding 7px 16px
  Rol: 10px / 700 / uppercase / color #9B9B9B
  Nombre: 13px / 700 / color #0A0A0A
```

### Content area

```
Fondo:   #EDECE8
Padding: 40px
Gap entre secciones: 28px
```

### Page header (dentro del content)

```
Margin-bottom: 36px

Eyebrow: .p-eyebrow → nombre del negocio / "MIS TARJETAS" / etc.
Heading: 32px / 800 / color #0A0A0A
Sub:     16px / 400 / color #6B6B6B / margin-top: 8px
```

### Grid de stats (3 columnas)

```css
display: grid;
grid-template-columns: repeat(3, 1fr);
gap: 20px;
margin-bottom: 28px;
```

### Actividad reciente (2 columnas)

```css
display: grid;
grid-template-columns: 1fr 1fr;
gap: 20px;
```

---

## Responsive

```css
@media (max-width: 768px) {
  /* Sidebar: hidden + hamburger menu */
  /* Hero: 1 columna, font-size 42px */
  /* Stats: 1 columna */
  /* Feature cards: 1 columna */
  /* Mini grid: 2 columnas */
  /* Activity: 1 columna */
  /* Content padding: 20px */
}
```

---

## Reglas absolutas

### HACER
- DM Sans en todo — sin excepciones
- Botones siempre pill (border-radius: 999px)
- Fondo de página: #EDECE8 (no blanco puro)
- Cards: #FFFFFF con border 1px solid #D8D7D3
- Nav activo sidebar: fondo #0A0A0A, texto blanco
- Headings de página: 32px / 800
- Stat values: 40px / 800
- Errores siempre en .p-badge-red

### NO HACER
- ❌ Fondo blanco puro en la página (#FFF) — usar #EDECE8
- ❌ Botones sin border-radius pill
- ❌ Gradientes en fondos de página
- ❌ Blobs/gradientes fuera de feature cards
- ❌ Inline styles
- ❌ Fuentes distintas a DM Sans
- ❌ Sombras exageradas
- ❌ Colores fuera de esta paleta
- ❌ Renombrar IDs/clases usados por JS
- ❌ Eliminar atributos Razor (asp-for, asp-action, etc.)
- ❌ Tocar controllers, services, models, migrations
