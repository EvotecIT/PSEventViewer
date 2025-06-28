Clear-Host
Import-Module ..\PSEventViewer.psd1 -Force

[string] $User = 'Administrator'
#Get-EVXEvent -LogName 'ForwardedEvents' -NamedDataFilter @{'SubjectUserName' = $User; 'TargetUserName' = $User } -Verbose -ExcludeID 5136



Get-EVXEvent -LogName 'ForwardedEvents' -NamedDataExcludeFilter @{'SubjectUserName' = $User; 'TargetUserName' = $User } -Verbose -ExcludeID 5136
#Get-EVXEvent -LogName 'ForwardedEvents'