# 47 Legacy Reporting Parity v1

## Objetivo

Agregar un reporte operativo minimo en la app moderna para reducir dependencia
de consultas manuales o pantallas legacy de lectura, sin tocar Web Forms ni
crear tablas nuevas.

## Cambios

- Nueva pagina admin protegida `/Admin/Reports`.
- Nuevo `AdminAppService.GetReportsAsync`.
- Resumen read-only de:
  - negocios;
  - clientes unicos con tarjetas;
  - tarjetas recientes;
  - sellos actuales e historicos;
  - Google Wallet emitidas;
  - Apple Wallet rastreadas;
  - alertas Wallet desde `StampLedger`.
- Links desde dashboard admin y sidebar.
- No hay SQL nuevo.

## Alcance

Este PR no reemplaza reportes avanzados del legacy. Es un reporte operativo
minimo para piloto y soporte. Si se requieren filtros historicos pesados,
DataTables o exportes contables, deben entrar en PR separado.

## Seguridad

- Requiere cookie `DigitalCards.Admin`.
- No muestra tokens, JWTs, push tokens, passwords, hashes, certificados,
  connection strings ni rutas locales.
- Lee datos existentes desde repositorios modernos/legacy.

## Validacion

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
