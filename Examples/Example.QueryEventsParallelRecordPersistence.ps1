# Demonstrates parallel event retrieval while preserving record IDs
$recordFile = Join-Path -Path $PSScriptRoot -ChildPath 'RecordIds.json'
$computers = @($Env:COMPUTERNAME, $Env:COMPUTERNAME)

$null = $computers | ForEach-Object -Parallel {
    param($machine, $filePath)
    Import-Module -Name PSEventViewer -Force
    Get-EVXEvent -LogName 'Application' -MachineName $machine -MaxEvents 1 -RecordIdFile $filePath -RecordIdKey $machine | Out-Null
} -ArgumentList $recordFile

Write-Host "Record IDs saved to $recordFile"
Get-Content -Path $recordFile
