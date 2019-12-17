Import-Module ..\PSEventViewer.psd1 -Force

$FilePath = 'C:\Test\Archive-System-2019-11-18-16-45-35-050.evtx'
$FilePath = 'C:\Test\Active Directory Web Services.evtx'
$Events = Get-Events -Path $FilePath -Oldest -MaxEvents 1 -Verbose
$Events

$EventsNewest = Get-Events -Path $FilePath -MaxEvents 1 -Verbose
$EventsNewest

#Get-WinEvent -Path $FilePath -Oldest -MaxEvents 1
#Get-WinEvent -Path $FilePath -MaxEvents 1