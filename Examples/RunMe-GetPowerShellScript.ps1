# Restore bounded script-block results from live logs and save each script to disk.
$query = @{
    Type              = 'WindowsPowerShell'
    MachineName       = 'AD1', 'AD2'
    Path              = Join-Path -Path $PSScriptRoot -ChildPath 'Scripts'
    MaxScripts        = 100
    MaxEventsScanned  = 50000
    MaxPendingScripts = 512
    MaxCachedEvents   = 2048
    Verbose           = $true
}
Get-EVXPowerShellScript @query

# The same reusable reconstruction path accepts an exported EVTX file.
Get-EVXPowerShellScript -Type WindowsPowerShell -EventLogPath "$Env:USERPROFILE\Desktop\PowerShell.evtx" -MaxScripts 100 -MaxEventsScanned 50000
