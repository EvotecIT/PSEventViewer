Clear-Host
Import-Module PSEventViewer -Force

Get-EVXEvent -LogName 'System' -ID 1001,1018 -ProviderName 'Microsoft-Windows-WER-SystemErrorReporting' -Verbose
Get-EVXEvent -LogName 'System' -ID 42,41,109 -ProviderName 'Microsoft-Windows-Kernel-Power' -Verbose
Get-EVXEvent -LogName 'System' -ID 1,12,13 -ProviderName 'Microsoft-Windows-Kernel-General' -Verbose
Get-EVXEvent -LogName 'System' -ID 6005,6006,6008,6013 -ProviderName 'EventLog' -Verbose
#Get-EVXEvent -LogName 'Security' -ID 1001,1018 -ProviderName 'Microsoft-Windows-WER-SystemErrorReporting' -Verbose
#Get-EVXEvent -LogName 'Security' -ID 1001,1018 -ProviderName 'Microsoft-Windows-WER-SystemErrorReporting' -Verbose