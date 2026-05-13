param(
    [string]$StatePath = "$env:USERPROFILE\.digitalcards\run",
    [string]$BaseUrl = "https://app.puntelio.com",
    [switch]$SkipHealth
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-ProcessState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$PidPath
    )

    if (-not (Test-Path -LiteralPath $PidPath)) {
        return [pscustomobject]@{
            Name = $Name
            ProcessId = $null
            Running = $false
            PidFile = $PidPath
        }
    }

    $processId = [int](Get-Content -LiteralPath $PidPath -Raw)
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
    [pscustomobject]@{
        Name = $Name
        ProcessId = $processId
        Running = $null -ne $process
        PidFile = $PidPath
    }
}

$processes = @(
    Get-ProcessState -Name "puntelio-app" -PidPath (Join-Path $StatePath "puntelio-app.pid")
    Get-ProcessState -Name "puntelio-cloudflared" -PidPath (Join-Path $StatePath "puntelio-cloudflared.pid")
)

if ($SkipHealth) {
    return $processes
}

$health = $null
try {
    $health = & (Join-Path $PSScriptRoot "check-puntelio-health.ps1") -BaseUrl $BaseUrl
} catch {
    $health = [pscustomobject]@{
        BaseUrl = $BaseUrl
        HealthStatusCode = $null
        ReadyStatusCode = $null
        Error = $_.Exception.Message
        CheckedAt = (Get-Date).ToString("o")
    }
}

[pscustomobject]@{
    Processes = $processes
    Health = $health
}
