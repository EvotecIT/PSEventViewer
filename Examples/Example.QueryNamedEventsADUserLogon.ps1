Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$findWinEventSplat = @{
    Type        = 'ADUserLogon'
    MachineName = $env:COMPUTERNAME
    Verbose     = $true
}

Find-WinEvent @findWinEventSplat -TimePeriod Last1Hour
