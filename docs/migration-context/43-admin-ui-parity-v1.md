# 43 Admin UI Parity v1

## Objetivo

Acercar las pantallas admin modernas al estilo operativo Web Forms despues de
instalar el shell legacy compartido.

## Cambios

- `/Admin/Dashboard` usa una grilla de acciones admin mas cercana al dashboard
  legacy.
- `/Admin/Businesses` y `/Admin/AdminUsers` usan clases de
  lista admin tipo tabla/card para acciones claras.
- Se mantiene el shell con sidebar/header/footer de `42 Modern Legacy Shell`.
- No cambia autorizacion, cookies, rutas ni reglas de negocio.
- No requiere SQL nuevo.

## Paridad Visual Cubierta

- Secciones admin visibles desde la sidebar.
- Acciones principales en cards con borde/acento azul.
- Listados administrativos con filas blancas, badges de estado y acciones.
- Mobile conserva columnas de una sola pista para evitar overflow.

## Pendiente Para PRs Siguientes

- Paridad visual fina del flujo negocio.
- Paridad visual fina del flujo cliente.
- Pulido de Wallet landing publica.
- Export/reportes mas cercanos a DataTables si el negocio lo requiere.

## Validacion

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
