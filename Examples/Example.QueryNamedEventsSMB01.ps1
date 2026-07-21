Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$findWinEventSplat = @{
    Type              = 'ADSMBServerAuditV1'
    MachineName       = 'AD1', 'AD2'
    ResolveDns        = $true
    DnsTimeoutMs      = 1000
    DnsMaxConcurrency = 8
    Verbose           = $true
}

Get-EVXEvent @findWinEventSplat -TimePeriod Last3Days | `
    Format-Table When, Computer, ClientAddress, ClientDNSName, ClientDnsResolutionStatus
