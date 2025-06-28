
Clear-Host
Import-Module PSEventViewer -Force
Get-EVXEvent -LogName 'Setup' -ID 2 -ComputerName 'AD1' -MaxEvents 1 -Verbose | Format-List *
Get-EVXEvent -LogName 'Setup' -ID 2 -ComputerName 'AD1' -MaxEvents 1 -Verbose | Format-List *