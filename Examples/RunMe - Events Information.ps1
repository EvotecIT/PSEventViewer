Import-Module ..\PSEventViewer.psd1 -Force

$Computers = 'EVO1', 'AD1','AD1'
$LogName = 'Security'

$EventLogsDirectory = Get-ChildItem -Path 'C:\MyEvents'

$Size = Get-EventsInformation -FilePath $EventLogsDirectory.FullName -Computer $Computers -LogName 'Security' -Verbose
$Size | ft -a Source, EventNewest, EventOldest,FileSize, FileSizeCurrentGB, FileSizeMaximumGB, IsEnabled, IsLogFull, LastAccessTime, LastWriteTime