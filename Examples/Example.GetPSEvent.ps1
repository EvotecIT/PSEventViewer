Clear-Host
Import-Module .\PSEventViewer.psd1 -Force

<#
$Test = Get-PSEvent -LogName 'Application' -EventId 1026 -Verbose -MaxEvents 1
$Test | Format-Table
$Test.Data
$Test.Data['NoNameA1']
#>

#Get-PSEvent -LogName 'Security' -EventId 4932,4933 -Verbose -MachineName 'AD1', 'AD2' -Mode ParallelForEach | Format-Table

Get-PSEvent -LogName 'Security' -EventId 4932,4933 -Verbose -MachineName 'AD1', 'AD2' -Mode Parallel #| Select-Object -first 10 | Format-Table
#Get-PSEvent -LogName 'Security' -EventId 4932,4933 -Verbose -MachineName 'AD1', 'AD2' -Mode ParallelForEachBuiltin | Select-Object -first 10 | Format-Table