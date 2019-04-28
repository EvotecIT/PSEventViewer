Import-Module .\PSEventViewer.psd1 -Force

$Date = (Get-Date).AddDays(-3)
$Date1 = Get-Date

$User = 'Administrator'

$Events = Get-Events -Machine AD1 -DateFrom $Date -DateTo $Date1 -ID 4738 -LogName 'Security' -Verbose -NamedDataFilter @{'SubjectUserName' = $User; 'TargetUserName' = $User }
$Events