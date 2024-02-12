Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$Output = Find-WinEvent -LogName 'Security' -EventId 4627 -Verbose -MachineName 'AD1', 'AD2', 'AD0' -ParallelOption Parallel | Select-Object -First 2
$Output[0] | Format-List
$Output[0].MessageData
