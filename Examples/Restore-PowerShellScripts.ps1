Import-Module PSEventViewer -Force

$Query = @{
    Type = 'WindowsPowerShell'
    MachineName = 'DC01', 'DC02'
    Path = 'C:\RecoveredScripts'
    MaxScripts = 100
    MaxEventsScanned = 50000
    MaxPendingScripts = 512
    MaxCachedEvents = 2048
    IncludeQueryInfo = $true
}
Get-EVXPowerShellScript @Query

Get-EVXPowerShellScriptExecution `
    -Type WindowsPowerShell `
    -MachineName DC01 `
    -MaxEvents 100 `
    -MaxEventsScanned 50000
