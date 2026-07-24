Import-Module PSEventViewer -Force

# Inventory can target remote collectors. Updates are intentionally local-only
# because the Windows Event Collector write API has no remote session contract.
Get-EVXCollectorSubscription -Name '*' |
    Select-Object Name, Enabled, ConfigurationMode, DeliveryMode, Query

Set-EVXCollectorSubscription `
    -Name 'Domain Controllers' `
    -Enabled $true `
    -Confirm:$false
