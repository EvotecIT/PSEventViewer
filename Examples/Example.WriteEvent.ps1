Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$writeWinEventSplat = @{
    LogName           = 'Application'
    EventId           = 5136
    Message           = 'This is a test message'
    Verbose           = $true
    ProviderName      = 'PSEventViewer1'
    Category          = 1
    AdditionalFields  = 'Field1', 'Field2', 'Field3'
    EventLogEntryType = 'Error'
}

Write-WinEvent @writeWinEventSplat