Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$findWinEventSplat = @{
    Type        = 'ADSMBServerAuditV1'
    MachineName = 'AD1', 'AD2'
    Verbose     = $true
}

Find-WinEvent @findWinEventSplat -TimePeriod Last3Days | Format-Table *