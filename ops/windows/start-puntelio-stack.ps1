param(
    [string]$BaseUrl = "https://app.puntelio.com",
    [switch]$SkipHealthCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$app = & (Join-Path $PSScriptRoot "start-puntelio-app.ps1") -Background
$tunnel = & (Join-Path $PSScriptRoot "start-puntelio-tunnel.ps1") -Background

[pscustomobject]@{
    AppProcessId = $app.ProcessId
    TunnelProcessId = $tunnel.ProcessId
    AppStdOut = $app.StdOut
    AppStdErr = $app.StdErr
    TunnelStdOut = $tunnel.StdOut
    TunnelStdErr = $tunnel.StdErr
}

if (-not $SkipHealthCheck) {
    Start-Sleep -Seconds 8
    & (Join-Path $PSScriptRoot "check-puntelio-health.ps1") -BaseUrl $BaseUrl
}
