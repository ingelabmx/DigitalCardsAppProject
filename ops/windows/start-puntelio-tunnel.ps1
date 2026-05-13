param(
    [string]$TunnelName = "puntelio-app",
    [string]$ConfigPath = "$env:USERPROFILE\.cloudflared\config.yml",
    [string]$LogsPath = "$env:USERPROFILE\.digitalcards\logs",
    [string]$StatePath = "$env:USERPROFILE\.digitalcards\run",
    [switch]$Background
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ConfigPath)) {
    throw "Cloudflare tunnel config was not found. Expected: $ConfigPath"
}

New-Item -ItemType Directory -Path $LogsPath -Force | Out-Null
New-Item -ItemType Directory -Path $StatePath -Force | Out-Null
$pidPath = Join-Path $StatePath "puntelio-cloudflared.pid"

if ($Background) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $stdoutPath = Join-Path $LogsPath "puntelio-cloudflared-$timestamp.out.log"
    $stderrPath = Join-Path $LogsPath "puntelio-cloudflared-$timestamp.err.log"
    $process = Start-Process `
        -FilePath "cloudflared" `
        -ArgumentList @("tunnel", "--config", $ConfigPath, "run", $TunnelName) `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -WindowStyle Hidden `
        -PassThru

    Set-Content -Path $pidPath -Value $process.Id -Encoding ASCII
    [pscustomobject]@{
        Name = "puntelio-cloudflared"
        ProcessId = $process.Id
        StdOut = $stdoutPath
        StdErr = $stderrPath
        PidFile = $pidPath
    }
    return
}

cloudflared tunnel --config $ConfigPath run $TunnelName
