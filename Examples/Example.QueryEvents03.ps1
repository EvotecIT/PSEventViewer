Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force -Verbose

Find-WinEvent -LogName 'System' -Verbose -ParallelOption Parallel -MachineName 'EVOMONSTER', 'AD1' -MaxEvents 1 | Format-Table

Find-WinEvent -LogName 'System' -Verbose -ParallelOption Parallel -MachineName 'EVOMONSTER', 'AD1' -MaxEvents 1 -Expand | Format-Table