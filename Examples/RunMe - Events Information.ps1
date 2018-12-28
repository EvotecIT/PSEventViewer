Import-Module ..\PSEventViewer.psd1 -Force

$Computer = 'EVO1', 'AD1', 'AD2'
$LogName = 'Security'

$EventLogsDirectory = Get-ChildItem -Path 'C:\MyEvents'


#$Size = Get-EventsInformation -Computer $Computer -LogName $LogName -Verbose
#$Size | Ft -a #PSObject.Properties

$Size = Get-EventsInformation -FilePath $EventLogsDirectory.FullName -Verbose
$Size[0] | fl *


#return


#$Events = Get-WinEvent -Path $EventLogsDirectory[0].FullName -MaxEvents 1 -Oldest
#$Events[0] | fl * #FL LogName, RecordID, ProviderName, MachineName