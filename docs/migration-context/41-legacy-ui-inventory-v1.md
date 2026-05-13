# 41 Legacy UI Inventory v1

## Objetivo

Documentar el diseno visual Web Forms que debe guiar la paridad de UI en
ASP.NET Core. Este PR no cambia comportamiento ni tablas; deja un contrato
visual para los siguientes PRs de shell, admin, negocio, cliente y Wallet.

## Principio De Paridad

La meta es paridad visual funcional: que la app moderna se sienta como la app
original para admin, negocios y clientes, manteniendo Razor Pages limpio,
responsive y seguro. No se busca copiar markup Web Forms ni hacer pixel-perfect
ciego si eso rompe accesibilidad, mobile o mantenimiento.

## Baseline Visual Web Forms

Archivos base revisados:

- `DigitalCardsApp/Login.aspx`
- `DigitalCardsApp/BusinessLogin.aspx`
- `DigitalCardsApp/Registry.aspx`
- `DigitalCardsApp/AdminDisplayPage.aspx`
- `DigitalCardsApp/BusinessDashboardPage.aspx`
- `DigitalCardsApp/BusinessCheckPage.aspx`
- `DigitalCardsApp/ClientPage.aspx`
- `DigitalCardsApp/assets/css/styles.min.css`
- `DigitalCardsApp/assets/scss/variables/_theme-variables.scss`
- `DigitalCardsApp/assets/scss/styles.scss`

El tema legacy usa:

- Bootstrap con tema propio;
- Montserrat como fuente principal;
- sidebar izquierdo fijo de `270px`;
- header superior de `70px`;
- cards blancas sobre fondo claro;
- tablas Bootstrap/DataTables;
- SimpleBar para scroll de sidebar;
- Tabler Icons e Iconify para iconos;
- logo DigitalCards o logo del negocio en sidebar;
- footer recurrente `Propiedad de IngeLabs`.

## Layout Legacy

La estructura dominante es:

- `page-wrapper` como contenedor raiz;
- `left-sidebar` para navegacion;
- `brand-logo` con imagen grande `220x100`;
- `sidebar-nav scroll-sidebar` con secciones por rol;
- `body-wrapper` como area principal;
- `app-header` con menu mobile y bienvenida;
- `container-fluid` para contenido;
- `card` y `card-body` como superficie de trabajo.

Este shell aparece en admin, negocio y cliente. Las paginas de login y registro
usan un contenedor centrado con `radial-gradient`, card compacta, logo
`DigitalCards-Logo.jpg`, texto `Tus tarjetas de recompensas` y boton primario
de ancho completo.

## Navegacion Por Rol

### Admin

Paginas legacy principales:

- `AdminInsertionPage.aspx`: agregar negocio.
- `AdminDisplayPage.aspx`: tabla de negocios.
- `AdminModPage.aspx`: modificar negocio.
- `Logout.aspx`.

La sidebar usa seccion `Administradores` y links con iconos Solar/Iconify. La
vista de negocios usa DataTables con botones copy/csv/excel/pdf/print.

### Negocio

Paginas legacy principales:

- `BusinessDashboardPage.aspx`: dashboard con chart, soporte y ultimas checadas.
- `BusinessInsertionPage.aspx`: tarjetas/alta de cliente.
- `BusinessCheckPage.aspx`: checadas con input de cliente y QR scanner.
- `Logout.aspx`.

La sidebar muestra el logo del negocio, seccion `Duenos de negocios`, links
`Dashboard`, `Tarjetas`, `Checadas` y `Cerrar sesion`. El header dice
`Bienvenido, {Negocio}`.

### Cliente

Pagina legacy principal:

- `ClientPage.aspx`: `Mis Tarjetas`, QR y lista de tarjetas.

La sidebar usa seccion `Cliente`, link `Mis Tarjetas` y logout. El contenido
principal muestra un card de QR y otro card de lista de tarjetas.

## Assets Visuales

Assets revisados:

- `DigitalCardsApp/assets/images/logos/DigitalCards-Logo.jpg`
- `DigitalCardsApp/assets/images/logos/DigitalCards-Icon-removebg.png`
- `DigitalCardsApp/assets/images/logos/logo-light.svg`
- `DigitalCardsApp/Logos/logo.jpg`
- logos de negocio en `DigitalCardsApp/Logos`.

Regla para modernizar:

- reutilizar identidad visual y proporcion de logos;
- no copiar secretos ni rutas locales;
- usar `/uploads/business-logos/...` para logos modernos de negocio;
- mantener fallback global cuando el negocio no tenga logo.

## Componentes A Replicar En ASP.NET Core

Los siguientes componentes deben existir antes de retirar Web Forms:

- shell autenticado con sidebar/header/footer;
- variante de login centrada;
- nav por rol Admin/Business/Client;
- cards de dashboard;
- tablas responsive con acciones claras;
- badges de estado;
- alertas success/error;
- formularios compactos con botones primarios de ancho completo cuando aplique;
- scanner QR en flujo negocio si el navegador/dispositivo lo soporta;
- estados mobile para colapsar sidebar.

## Secuencia Visual Recomendada

1. `feature/modern-legacy-shell-v1`: crear shell compartido y CSS base.
2. `feature/admin-ui-parity-v1`: aplicar shell a admin.
3. `feature/business-ui-parity-v1`: aplicar shell a negocio y checadas.
4. `feature/client-ui-parity-v1`: aplicar shell a cliente.
5. `feature/wallet-public-ui-polish-v1`: pulir landing Wallet publica.

## Criterios De Aceptacion

- La app moderna conserva los flujos y permisos actuales.
- La navegacion por rol no muestra links de otro rol.
- Desktop se parece al dashboard Web Forms: sidebar, header, cards y tablas.
- Mobile no se rompe ni encima texto.
- Wallet, correo, Google, Apple y APNs no cambian por este inventario.
- Playwright sigue verde con fakes.

## Validacion

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
