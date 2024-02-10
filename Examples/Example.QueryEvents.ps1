Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$EventId = @(
    1..30
    5136, 5137, 5168
)

$Output = Find-WinEvent -LogName 'Security' -EventId $EventId -Verbose -MachineName 'AD1', 'AD2', 'AD0' -ParallelOption Parallel
$Output | Format-Table