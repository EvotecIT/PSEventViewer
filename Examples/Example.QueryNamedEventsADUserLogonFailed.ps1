
Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$findWinEventSplat = @{
    Type        = 'ADUserLogonFailed'
    MachineName = 'DC1', 'DC2'
    Verbose     = $true
}

Find-WinEvent @findWinEventSplat -TimePeriod Last7Days | Format-Table *