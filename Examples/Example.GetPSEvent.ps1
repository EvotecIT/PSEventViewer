Clear-Host
Import-Module .\PSEventViewer.psd1 -Force

<#
$Test = Get-PSEvent -LogName 'Application' -EventId 1026 -Verbose -MaxEvents 1
$Test | Format-Table
$Test.Data
$Test.Data['NoNameA1']
#>

#Get-PSEvent -LogName 'Security' -EventId 4932,4933 -Verbose -MachineName 'AD1', 'AD2' -Mode ParallelForEach | Format-Table

#Find-GenericEvent -LogName 'Security' -EventId 4932,4933 -Verbose -MachineName 'AD1', 'AD2' -Mode Parallel | Select-Object -first 10 | Format-Table
#Get-PSEvent -LogName 'Security' -EventId 4932,4933 -Verbose -MachineName 'AD1', 'AD2' -Mode ParallelForEachBuiltin | Select-Object -first 10 | Format-Table

#Find-NamedEvent -Type ADComputerChangeDetailed -MachineName AD1,AD2 -Verbose | Format-Table

Find-GenericEvent -LogName 'Security' -EventId 5136, 5137, 5168 -Verbose -MachineName 'AD1', 'AD2', 'AD0' -Mode ParallelForEachBuiltin | Select-Object -First 1 | Format-Table

return


$List = [System.Collections.Generic.List[string]]::new()
$List.Add('AD1')
$List.Add('AD2')
$List.Add('AD0')

$IDs = [System.Collections.Generic.List[int]]::new()
$IDs.Add(5136)
$IDs.Add(5137)
$IDs.Add(5168)

$V = [PSEventViewer.EventSearching]::QueryLogsParallel('Security', $Ids, $List)
$V | Format-Table
