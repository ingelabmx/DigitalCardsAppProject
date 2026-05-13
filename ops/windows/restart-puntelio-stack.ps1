param(
    [string]$BaseUrl = "https://app.puntelio.com",
    [switch]$SkipHealthCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "stop-puntelio-stack.ps1")
Start-Sleep -Seconds 2
& (Join-Path $PSScriptRoot "start-puntelio-stack.ps1") -BaseUrl $BaseUrl -SkipHealthCheck:$SkipHealthCheck
