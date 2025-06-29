# Demonstrates using Get-EVXPowerShellScript cmdlet
Get-EVXPowerShellScript -Type WindowsPowerShell -Path $PSScriptRoot\Scripts -ComputerName AD1, AD2 -Verbose

$Events = Get-EVXEvent -LogName 'Microsoft-Windows-PowerShell/Operational' -ID 4103,4104 -Verbose -Path "$Env:USERPROFILE\Desktop\PowerShell.evtx"
#Get-EVXPowerShellScript -Events $Events -Path $PSScriptRoot\Scripts -Format

