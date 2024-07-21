Clear-Host
Import-Module .\PSEventViewer.psd1 -Force

Find-WinEvent -LogName 'Security' -Verbose -MachineName 'AD1', 'AD2', 'AD0' -EventRecordID 16833138,16833124,16833136 | Format-Table