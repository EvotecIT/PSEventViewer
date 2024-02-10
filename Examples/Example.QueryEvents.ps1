Clear-Host
Import-Module .\PSEventViewer.psd1 -Force

Find-WinEvent -LogName 'Security' -EventId 5136, 5137, 5168 -Verbose -MachineName 'AD1', 'AD2', 'AD0' -ParallelOption Parallel | Format-Table