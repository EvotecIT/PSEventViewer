Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$findWinEventSplat = @{
    Type        = 'ADComputerChangeDetailed', 'ADUserChangeDetailed', 'ADGroupChange', 'ADGroupCreateDelete', 'ADGroupMembershipChange'
    MachineName = 'AD1', 'AD2', 'AD0'
    Verbose     = $true
}

Find-WinEvent @findWinEventSplat -TimePeriod Today | Format-Table