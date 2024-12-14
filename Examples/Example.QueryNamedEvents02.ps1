Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$findWinEventSplat = @{
    Type        = 'ADUserLogonFailed'  # 'ADComputerChangeDetailed', 'ADUserChangeDetailed', 'ADGroupChange', 'ADGroupCreateDelete', 'ADGroupMembershipChange'
    MachineName = 'AD1', 'AD2', 'AD0', 'ADCS'
    Verbose     = $true
}

Find-WinEvent @findWinEventSplat -TimePeriod CurrentDay | Format-List
##Test | Format-Table