
Clear-Host
Import-Module PSEventViewer
Get-Events -LogName 'Setup' -ID 2 -ComputerName 'AD1' -MaxEvents 1 -Verbose | Format-List *
