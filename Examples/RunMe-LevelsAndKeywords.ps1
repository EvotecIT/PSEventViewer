Import-Module PSEventViewer -Force

Get-Events -MaxEvents 5 -LogName Security -Keywords AuditSuccess | Format-List *