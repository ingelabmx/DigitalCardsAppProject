param(
    [string]$StatePath = "$env:USERPROFILE\.digitalcards\run"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Stop-FromPidFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$PidPath
    )

    if (-not (Test-Path -LiteralPath $PidPath)) {
        return [pscustomobject]@{
            Name = $Name
            Status = "NoPidFile"
            ProcessId = $null
        }
    }

    $processId = [int](Get-Content -LiteralPath $PidPath -Raw)
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $processId -Force
        Remove-Item -LiteralPath $PidPath -Force
        return [pscustomobject]@{
            Name = $Name
            Status = "Stopped"
            ProcessId = $processId
        }
    }

    Remove-Item -LiteralPath $PidPath -Force
    [pscustomobject]@{
        Name = $Name
        Status = "NotRunning"
        ProcessId = $processId
    }
}

Stop-FromPidFile -Name "puntelio-app" -PidPath (Join-Path $StatePath "puntelio-app.pid")
Stop-FromPidFile -Name "puntelio-cloudflared" -PidPath (Join-Path $StatePath "puntelio-cloudflared.pid")
