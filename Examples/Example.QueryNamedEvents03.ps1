Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$findWinEventSplat = @{
    Type        = 'OSTimeChange'
    MachineName = 'AD1', 'AD2', 'AD0'
    Verbose     = $true
}

Find-WinEvent @findWinEventSplat -TimePeriod Last3Days  | Format-Table