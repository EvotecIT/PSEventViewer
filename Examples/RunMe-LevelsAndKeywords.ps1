Import-Module PSEventViewer -Force

Get-EVXEvent -MaxEvents 5 -LogName Security -Keywords AuditSuccess | Format-List *