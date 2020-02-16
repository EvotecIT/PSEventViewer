Import-Module ..\PSEventViewer.psd1 -force
#Get-PSRegistry -RegistryPath 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Channels' | Format-Table -AutoSize
#Get-PSRegistry -RegistryPath 'HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EventLog\'
Get-EventsSettings -LogName 'Internet Explorer' #-ComputerName AD1
Get-EventsSettings -LogName 'System' -ComputerName AD1
Get-EventsSettings -LogName 'Directory Service' -ComputerName AD1
Get-EventsSettings -LogName 'Windows PowerShell' #-ComputerName AD1
Get-EventsSettings -LogName 'ForwardedEvents' #-ComputerName AD1
#Set-EventsSettings -LogName 'Application' #-MaximumSize 2000 #-ComputerName