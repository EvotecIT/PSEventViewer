Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

Find-WinEvent -LogName 'Security' -EventId 4905 -Verbose -ParallelOption Disable | Format-Table

Find-WinEvent -LogName 'Security' -EventId 4905 -Verbose -ParallelOption Parallel | Format-Table