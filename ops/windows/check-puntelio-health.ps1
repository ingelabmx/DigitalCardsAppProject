param(
    [string]$BaseUrl = "https://app.puntelio.com"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$health = Invoke-WebRequest "$BaseUrl/health" -UseBasicParsing
$ready = Invoke-WebRequest "$BaseUrl/health/ready" -UseBasicParsing

[pscustomobject]@{
    HealthStatusCode = [int]$health.StatusCode
    ReadyStatusCode = [int]$ready.StatusCode
}

