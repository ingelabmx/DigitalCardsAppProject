param(
    [string]$LogsPath = "$env:USERPROFILE\.digitalcards\logs",
    [int]$Tail = 80
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $LogsPath)) {
    throw "Logs folder was not found. Expected: $LogsPath"
}

$files = Get-ChildItem -LiteralPath $LogsPath -Filter "puntelio-*.log" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 6

foreach ($file in $files) {
    Write-Host ""
    Write-Host "===== $($file.Name) ====="
    Get-Content -LiteralPath $file.FullName -Tail $Tail
}
