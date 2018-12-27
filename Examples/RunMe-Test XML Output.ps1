$User = 'Administrator'
$XML = Get-WinEventXPathFilter -NamedDataFilter @{'SubjectUserName' = $User; 'TargetUserName' = $User } -LogName 'ForwardedEvents'
Get-WinEvent -FilterXml $XML