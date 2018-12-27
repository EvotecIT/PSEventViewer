Clear-Host
Import-Module PSEventViewer -Force

#Get-Events -LogName 'Security' -ID 4624 -MaxEvents 10 -Verbose | Format-Table Date, Action, Who, ObjectAffected, Computer, IpAddress, IpPort, MachineName,ProviderName, LogonProcessName -AutoSize
$Output = Get-Events -LogName 'Security' -ID 4624 -MaxEvents 10 -Verbose -Machine 'AD1',AD3,AD2
$Output | ft RecordID, EventId, GatheredFrom, GatheredLogName
#$Output[0] | Format-List * #Date, Action, Who, ObjectAffected, Computer, IpAddress, IpPort, MachineName,ProviderName, LogonProcessName
#Get-Events -LogName 'Security' -ID 4624 -MaxEvents 10 -Verbose -Machine 'AD1','AD2' | Format-Table Date, Action, Who, ObjectAffected, Computer, IpAddress, IpPort, MachineName,ProviderName, LogonProcessName