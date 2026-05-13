param(
    [string]$ProjectPath = "src\DigitalCards.Web\DigitalCards.Web.csproj",
    [string]$LaunchProfile = "http",
    [string]$LocalSettingsPath = "$env:USERPROFILE\.digitalcards\appsettings.Local.json",
    [string]$DataProtectionPath = "$env:USERPROFILE\.digitalcards\data-protection-keys"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $LocalSettingsPath)) {
    throw "Local settings file was not found. Expected: $LocalSettingsPath"
}

Get-Content -LiteralPath $LocalSettingsPath -Raw | ConvertFrom-Json | Out-Null
New-Item -ItemType Directory -Path $DataProtectionPath -Force | Out-Null

dotnet run --project $ProjectPath --launch-profile $LaunchProfile

