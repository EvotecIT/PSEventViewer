Clear-Host
Import-Module PSEventViewer -Force
Get-Events -LogName 'ForwardedEvents' -NamedDataFilter @{'SubjectUserName' = $User; 'TargetUserName' = $User } -Verbose -ExcludeID 5136
#Get-Events -LogName 'ForwardedEvents'