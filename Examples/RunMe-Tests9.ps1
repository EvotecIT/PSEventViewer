Clear-Host
Import-Module PSEventViewer -Force
Get-Events -LogName 'Security' -ID 5379 -RecordID 19626 -Verbose
Get-Events -LogName 'Security' -ID 5379 -Verbose