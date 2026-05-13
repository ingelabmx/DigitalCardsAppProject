param(
    [string]$TunnelName = "puntelio-app",
    [string]$ConfigPath = "$env:USERPROFILE\.cloudflared\config.yml"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ConfigPath)) {
    throw "Cloudflare tunnel config was not found. Expected: $ConfigPath"
}

cloudflared tunnel --config $ConfigPath run $TunnelName

