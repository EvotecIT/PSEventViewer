Clear-Host
Import-Module PSEventViewer -Force
Get-Events -LogName 'Security' -ID 5379 -RecordID 19626 -Verbose | Format-Table # takes 380 miliseconds
Get-Events -LogName 'Security' -ID 5379 -Verbose | Where { $_.RecordID -eq 19626 } | Format-Table  # takes 6 minutes+