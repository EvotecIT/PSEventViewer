Clear-Host
Import-Module PSEventViewer -Force
Import-Module PSSharedGoods -Force

#Get-EVXEvent -LogName 'Security' -ID 4624 -MaxEvents 10 -Verbose | `
# Format-Table Date, Action, Who, ObjectAffected, Computer, IpAddress, IpPort, MachineName,ProviderName, LogonProcessName -AutoSize

$Output = Get-EVXEvent -LogName 'Security' -ID 4624 -MaxEvents 10 -Verbose -Machine 'AD1',AD3,AD2 #-ErrorAction SilentlyContinue -ErrorVariable MyErrors
$Output | ft RecordID, ID, GatheredFrom, GatheredLogName
#$Output[0] | Format-List * #Date, Action, Who, ObjectAffected, Computer, IpAddress, IpPort, MachineName,ProviderName, LogonProcessName
#Get-EVXEvent -LogName 'Security' -ID 4624 -MaxEvents 10 -Verbose -Machine 'AD1','AD2'
# | Format-Table Date, Action, Who, ObjectAffected, Computer, IpAddress, IpPort, MachineName,ProviderName, LogonProcessName


#$MyErrors.Count
#$MyErrors #.Exception.Message