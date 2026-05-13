# 40 Support Export v1

## Objetivo

Agregar una descarga segura de diagnostico desde `/Admin/Support` para soporte
operativo durante pilotos y activaciones reales.

## Comportamiento

- El admin busca por cliente, negocio, tarjeta o URL Wallet.
- La misma pantalla muestra el diagnostico seguro existente.
- El boton `Exportar diagnostico JSON` descarga un archivo
  `support-diagnostic-*.json`.
- El export incluye:
  - conteos de clientes, negocios y tarjetas;
  - estado visible de clientes y negocios;
  - sellos, estados Wallet y eventos recientes de `StampLedger`;
  - configuracion operativa no secreta de `LegacyWalletSync`.

## Seguridad

- Requiere cookie `DigitalCards.Admin` y policy `AdminOnly`.
- No muestra ni exporta tokens Wallet completos, enrollment tokens, auth tokens,
  push tokens, JWTs, passwords, hashes, certificados, connection strings, rutas
  locales ni credenciales.
- El log registra longitud del query, no el texto buscado.
- No muta datos y no requiere SQL nuevo.

## Uso Operativo

1. Entrar a `/Admin/Support`.
2. Buscar cliente, negocio, tarjeta o link Wallet.
3. Revisar el resultado en pantalla.
4. Descargar el JSON si se necesita compartir evidencia interna de soporte.
5. Adjuntar el archivo solo en canales internos controlados.

## Validacion

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
