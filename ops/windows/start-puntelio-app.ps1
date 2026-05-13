param(
    [string]$ProjectPath = "src\DigitalCards.Web\DigitalCards.Web.csproj",
    [string]$LaunchProfile = "http",
    [string]$LocalSettingsPath = "$env:USERPROFILE\.digitalcards\appsettings.Local.json",
    [string]$DataProtectionPath = "$env:USERPROFILE\.digitalcards\data-protection-keys",
    [string]$LogsPath = "$env:USERPROFILE\.digitalcards\logs",
    [string]$StatePath = "$env:USERPROFILE\.digitalcards\run",
    [switch]$Background
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $LocalSettingsPath)) {
    throw "Local settings file was not found. Expected: $LocalSettingsPath"
}

Get-Content -LiteralPath $LocalSettingsPath -Raw | ConvertFrom-Json | Out-Null
New-Item -ItemType Directory -Path $DataProtectionPath -Force | Out-Null
New-Item -ItemType Directory -Path $LogsPath -Force | Out-Null
New-Item -ItemType Directory -Path $StatePath -Force | Out-Null

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$projectFullPath = Resolve-Path (Join-Path $repoRoot $ProjectPath)
$pidPath = Join-Path $StatePath "puntelio-app.pid"

if ($Background) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $stdoutPath = Join-Path $LogsPath "puntelio-app-$timestamp.out.log"
    $stderrPath = Join-Path $LogsPath "puntelio-app-$timestamp.err.log"
    $process = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $projectFullPath, "--launch-profile", $LaunchProfile) `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -WindowStyle Hidden `
        -PassThru

    Set-Content -Path $pidPath -Value $process.Id -Encoding ASCII
    [pscustomobject]@{
        Name = "puntelio-app"
        ProcessId = $process.Id
        StdOut = $stdoutPath
        StdErr = $stderrPath
        PidFile = $pidPath
    }
    return
}

dotnet run --project $projectFullPath --launch-profile $LaunchProfile
