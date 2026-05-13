param(
    [string]$BaseUrl = "https://app.puntelio.com",
    [int]$TimeoutSeconds = 15
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$healthUri = "$BaseUrl/health"
$readyUri = "$BaseUrl/health/ready"
$health = Invoke-WebRequest $healthUri -UseBasicParsing -TimeoutSec $TimeoutSeconds
$ready = Invoke-WebRequest $readyUri -UseBasicParsing -TimeoutSec $TimeoutSeconds

[pscustomobject]@{
    BaseUrl = $BaseUrl
    HealthStatusCode = [int]$health.StatusCode
    ReadyStatusCode = [int]$ready.StatusCode
    CheckedAt = (Get-Date).ToString("o")
}
