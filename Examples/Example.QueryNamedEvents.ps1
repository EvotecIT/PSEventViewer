Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

Find-WinEvent -Type ADComputerChangeDetailed, ADUserChangeDetailed -MachineName AD1, AD2, AD0 -Verbose | Format-Table