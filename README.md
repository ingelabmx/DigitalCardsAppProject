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