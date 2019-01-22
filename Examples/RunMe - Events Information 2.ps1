Import-Module ..\PSEventViewer.psd1 -Force

$Size = Get-EventsInformation -FilePath $EventLogsDirectory.FullName -LogName 'Security','System' -RunAgainstDC 
$Size | Format-Table -AutoSize Source, EventNewest, EventOldest,FileSize, FileSizeCurrentGB, FileSizeMaximumGB, IsEnabled, IsLogFull, LastAccessTime, LastWriteTime, LogFilePath, LOgName