# https://devblogs.microsoft.com/powershell/powershell-the-blue-team/

Restore-PowerShellScript -Type WindowsPowerShell -Path $PSScriptRoot\Scripts -ComputerName AD1, AD2 -Verbose

#$Events1 = Get-EVXEvent -LogName 'Microsoft-Windows-PowerShell/Operational' -ID 4103, 4104 -Verbose -Path "$Env:USERPROFILE\Desktop\PowerShellCore.evtx"
#$Events1 = Get-EVXEvent -LogName 'Microsoft-Windows-PowerShell/Operational' -ID 4104, 4105, 4106 -Verbose -Path "$Env:USERPROFILE\Desktop\PowerShellWindows.evtx"
#$Events1 = Get-EVXEvent -LogName 'Microsoft-Windows-PowerShell/Operational' -ID 4104 -Verbose -Path "$Env:USERPROFILE\Desktop\PowerShellWindows.evtx"
#$Events = Get-EVXEvent -ID 4103, 4104, 4105, 4106 -Verbose -Path "$Env:USERPROFILE\Desktop\PowerShell-26082020.evtx"
#Restore-PowerShellScript -Events $Events -Path $PSScriptRoot\ScriptsMalware -AddMarkdown #-Format
#Restore-PowerShellScript -Events $Events1 -Path $PSScriptRoot\Scripts -AddMarkdown -Format
