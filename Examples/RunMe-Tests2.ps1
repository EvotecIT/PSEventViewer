# Example old way
#Get-WinEvent -FilterHashtable @{ LogName = 'Security'; Id = 1102 } -ComputerName 'AD1' | Format-List *

# Example new way
Import-Module PSEventViewer -Force
Get-Events -LogName 'Security' -Id 1102 -ComputerName 'AD1' -Verbose -MaxEvents 5 | Format-List *
Get-Events -LogName 'Setup' -Id 9 -ComputerName 'AD1' -MaxEvents 1 | Format-List *
Get-Events -LogName 'Setup' -Id 2 -ComputerName 'AD1' -MaxEvents 10 | Format-List *