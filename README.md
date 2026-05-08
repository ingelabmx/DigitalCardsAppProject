# DigitalCardsApp
cd C:\Users\eguillen\source\repos\DigitalCardsAppProject
dotnet build DigitalCardsApp.Modern.sln
dotnet test DigitalCardsApp.Modern.sln
dotnet run --no-launch-profile --project src\DigitalCards.Web\DigitalCards.Web.csproj --urls http://localhost:5088
http://localhost:5088


# Cerrar antes de compilar
Get-Process DigitalCards.Web -ErrorAction SilentlyContinue | Stop-Process
dotnet build DigitalCardsApp.Modern.sln
