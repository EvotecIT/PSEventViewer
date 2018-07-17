
# Find out the structure of event
Clear-Host
Get-WinEvent -FilterHashtable @{ LogName = 'Setup'; Id = 2 } -ComputerName 'AD1' -MaxEvents 1 | Format-List *
Get-WinEvent -FilterHashtable @{ LogName = 'Setup'; Id = 2 } -ComputerName 'AD1' -MaxEvents 10 | Format-List Message, TimeCreated, MachineName
Get-WinEvent -FilterHashtable @{ LogName = 'Setup'; Id = 2 } -ComputerName 'AD1' -MaxEvents 10 | Where-Object { $_.Message -like '*KB4103723*' } |  Format-List Message, TimeCreated, MachineName

Clear-Host
Import-Module PSEventViewer -Force
Get-Events -LogName 'Setup' -ID 2 -ComputerName 'AD1' -MaxEvents 1 | Format-List *
Get-Events -LogName 'Setup' -ID 2 -ComputerName 'AD1' -MaxEvents 10 | Format-List MachineName, IntendedPackageState, PackageIdentifier, Date, ErrorCode
Get-Events -LogName 'Setup' -ID 2 -ComputerName 'AD1' -MaxEvents 10 | Where-Object { $_.PackageIdentifier -eq 'KB4103723' } | Format-List MachineName, IntendedPackageState, PackageIdentifier, Date, ErrorCode