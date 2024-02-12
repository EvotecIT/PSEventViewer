Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

Find-WinEvent -LogName 'System' -Verbose -ParallelOption Parallel -MachineName 'EVOMONSTER', 'AD1' | Format-Table