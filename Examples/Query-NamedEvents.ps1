Import-Module PSEventViewer -Force

Get-EVXEvent `
    -NamedEvent ADUserLogonFailed, ADUserLockouts `
    -MachineName DC01, DC02 `
    -TimePeriod Last24Hours `
    -ReadMode Full `
    -MaxEvents 500 |
    Select-Object TimeCreated, NamedEventName, MachineName, UserName, IpAddress

Get-EVXEvent `
    -NamedEvent ADSMBServerAuditV1 `
    -MachineName DC01, DC02 `
    -TimePeriod Last3Days `
    -ResolveDns `
    -DnsTimeoutMs 1000 `
    -DnsMaxConcurrency 8 |
    Select-Object When, Computer, ClientAddress, ClientDNSName,
        ClientDnsResolutionStatus
