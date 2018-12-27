Clear-Host
Import-Module PSEventViewer -Force
Get-Events -LogName 'ForwardedEvents' -ID 1105 -RecordID 3512231 -Verbose
#Get-Events -LogName 'Security' -ID 5379 -Verbose


Get-Events -LogName 'ForwardedEvents' -RecordID 3512231 -Verbose