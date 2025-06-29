# https://devblogs.microsoft.com/powershell/powershell-the-blue-team/

Get-EVXPowerShellScript -Type WindowsPowerShell -Path $PSScriptRoot\Scripts -ComputerName AD1, AD2 -Verbose

$Events1 = Get-EVXEvent -LogName 'Microsoft-Windows-PowerShell/Operational' -ID 4103, 4104 -Verbose -Path "$Env:USERPROFILE\Desktop\PowerShellCore.evtx"
$Events2 = Get-EVXEvent -LogName 'Microsoft-Windows-PowerShell/Operational' -ID 4104, 4105, 4106 -Verbose -Path "$Env:USERPROFILE\Desktop\PowerShellWindows.evtx"
$Events = Get-EVXEvent -ID 4103, 4104, 4105, 4106 -Verbose -Path "$Env:USERPROFILE\Desktop\PowerShell-26082020.evtx"
#Get-EVXPowerShellScript -Events $Events -Path $PSScriptRoot\ScriptsMalware
#Get-EVXPowerShellScript -Events $Events1 -Path $PSScriptRoot\Scripts -Format
