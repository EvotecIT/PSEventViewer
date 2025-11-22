Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

# those events will be split in 22 chunks to allow parallel processing and prevent errors
$EventId = @(
    1..30
    5136, 5137, 5168
    4688
)

$Output = Find-WinEvent -LogName 'Security' -EventId $EventId -Verbose -MachineName 'AD1', 'AD2', 'AD0', "ADRODC" -ParallelOption Parallel
$Output | Format-Table