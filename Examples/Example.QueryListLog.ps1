Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

Find-WinEvent -Verbose -ListLog "Application", "System", "Security*" -MachineName AD3, AD1, EVOMONSTER, AD2, AD0, AD15, AD30, ADRODC.ad.evotec.pl | Format-Table