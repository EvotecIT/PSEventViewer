Import-Module PSEventViewer -Force

# Inventory can target remote collectors. Updates are intentionally local-only
# because the Windows Event Collector write API has no remote session contract.
Get-EVXCollectorSubscription -Name '*' |
    Select-Object Name, Enabled, ConfigurationMode, DeliveryMode, Query

# Build a reusable failed-logon subscription without hand-authoring WEC XML.
$Filter = New-EVXFilter `
    -EventId 4625 `
    -ProviderName Microsoft-Windows-Security-Auditing `
    -TimePeriod Last24Hours
$Definition = New-EVXCollectorSubscription `
    -Name 'Failed logons' `
    -SourceComputer DC01, DC02 `
    -LogName Security `
    -Filter $Filter `
    -Description 'Security 4625 from domain controllers' `
    -Enabled $false

# Applying a definition is explicit and supports WhatIf/Confirm.
$Definition | Set-EVXCollectorSubscription -Confirm:$false

Set-EVXCollectorSubscription `
    -Name 'Domain Controllers' `
    -Enabled $true `
    -Confirm:$false
