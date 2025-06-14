Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$writeWinEventSplat = @{
    LogName           = 'Application'
    EventId           = 1
    Message           = 'This is a test message'
    Verbose           = $true
    ProviderName      = 'PSEventViewer'
    Category          = 1
    #AdditionalFields  = 'Field1', 'Field2', 'Field3'
    EventLogEntryType = 'Error'
    ComputerName      = "AD1.ad.evotec.xyz"
}

Write-WinEvent @writeWinEventSplat