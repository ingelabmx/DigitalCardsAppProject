# 45 Client UI Parity v1

## Objetivo

Acercar el dashboard moderno de cliente al estilo operativo del Web Forms
original sin cambiar autorizacion, Wallets ni reglas de negocio.

## Cambios

- `/Client/Dashboard` usa una cabecera tipo dashboard legacy con acciones
  compactas.
- El resumen cliente muestra identificador visual estilo QR, usuario, correo,
  sellos actuales/historicos y conteos Wallet.
- Las tarjetas del cliente muestran una cara visual de lealtad con sellos,
  estado Apple/Google y links Wallet con token opaco.
- Login, cambio de contrasena, forgot y reset password de cliente usan una
  superficie visual consistente con el shell moderno.
- No hay SQL nuevo.

## Seguridad

- El cliente sigue viendo solo tarjetas asociadas a su `ClientId` autenticado.
- Los links Wallet siguen usando tokens opacos.
- No se muestran passwords, hashes, JWTs, push tokens, connection strings ni
  rutas locales.
- Cookies admin/negocio no autorizan paginas cliente.

## Validacion

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```

## Pendiente

- Si se requiere QR real escaneable para cliente, agregarlo en un PR separado
  con generacion server-side probada.
- Pulir Wallet landing publica en `feature/wallet-public-ui-polish-v1`.
