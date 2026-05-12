# DigitalCardsApp
cd C:\Users\eguillen\source\repos\DigitalCardsAppProject
dotnet build DigitalCardsApp.Modern.sln
dotnet run --project src\DigitalCards.Web\DigitalCards.Web.csproj --launch-profile http
http://localhost:5088


# Cerrar antes de compilar
Get-Process DigitalCards.Web -ErrorAction SilentlyContinue | Stop-Process
dotnet build DigitalCardsApp.Modern.sln


#Cloudflare
Despues de dotnet run:
cloudflared tunnel --url http://localhost:5088
Update: PublicBaseUrl

Vuelve a ejectuar:
dotnet run --no-launch-profile --project src\DigitalCards.Web\DigitalCards.Web.csproj --urls http://localhost:5088
Ahora abre:
https://TU-URL.trycloudflare.com


#Issues
Las tarjetas quedan ligadas al dominio web.

# Login negocio moderno

El flujo ASP.NET Core moderno usa cookie auth para negocio:

- Login: `http://localhost:5088/Business/Login`
- Dashboard protegido: `http://localhost:5088/Business/Dashboard`
- Logout: `http://localhost:5088/Business/Logout`

Las paginas `/Business/Dashboard`, `/Business/Enroll` y `/Business/Stamp`
requieren cookie valida. Ya no se debe pasar `businessId` por URL ni por hidden
input; el negocio se toma desde los claims de la sesion.

Para pruebas automatizadas con fakes, se puede ignorar la configuracion local
real:

```powershell
$env:DigitalCards__SkipUserLocalConfiguration='true'
```

Si la app real no arranca por configuracion, valida que
`%USERPROFILE%\.digitalcards\appsettings.Local.json` sea JSON valido.

# Legacy Wallet Sync
Mientras Web Forms siga agregando sellos directo en HostGator, activa el worker
moderno solo en pruebas controladas:

```json
"DigitalCards": {
  "LegacyWalletSync": {
    "Enabled": true,
    "PollIntervalSeconds": 60,
    "LookbackMinutes": 1440,
    "BatchSize": 50
  }
}
```

El worker no cambia `ClientCard`; solo detecta cambios recientes y dispara patch
Google Wallet y push Apple Wallet. Los diagnósticos seguros se activan con:

```json
"DigitalCards": {
  "Diagnostics": {
    "EnableWalletDiagnostics": true
  }
}
```

Endpoint:

```text
/internal/wallet-diagnostics/{CardID}
```
