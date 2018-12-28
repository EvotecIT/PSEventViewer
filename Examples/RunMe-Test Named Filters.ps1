Clear-Host
Import-Module ..\PSEventViewer.psd1 -Force

[string] $User = 'Administrator'
#Get-Events -LogName 'ForwardedEvents' -NamedDataFilter @{'SubjectUserName' = $User; 'TargetUserName' = $User } -Verbose -ExcludeID 5136



Get-Events -LogName 'ForwardedEvents' -NamedDataExcludeFilter @{'SubjectUserName' = $User; 'TargetUserName' = $User } -Verbose -ExcludeID 5136
#Get-Events -LogName 'ForwardedEvents'