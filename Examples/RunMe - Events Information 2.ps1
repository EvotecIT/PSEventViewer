Import-Module .\PSEventViewer.psd1 -Force

$Size = Get-EventsInformation -FilePath $EventLogsDirectory.FullName -LogName 'Security' -Machine AD1 #-RunAgainstDC
$Size #| Format-Table -AutoSize Source, EventNewest, EventOldest,FileSize, FileSizeCurrentGB, FileSizeMaximumGB, IsEnabled, IsLogFull, LastAccessTime, LastWriteTime, LogFilePath, LOgName
$Size.SecurityDescriptorDiscretionaryAcl