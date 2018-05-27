# Example old way
Get-WinEvent -FilterHashtable @{ LogName = 'Security'; Id = 1102 } -ComputerName 'AD1' | Format-List *

# Example new way
Import-Module PSEventViewer -Force
Get-Events -LogName 'Security' -Id 1102 -ComputerName 'AD1' | Format-List *