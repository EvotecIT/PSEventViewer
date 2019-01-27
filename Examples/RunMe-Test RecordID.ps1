Clear-Host
#Import-Module PSEventViewer -Force
#Get-Events -LogName 'ForwardedEvents' -ID 1105 -RecordID 3512231 -Verbose
#Get-Events -LogName 'Security' -ID 5379 -Verbose


#Get-Events -LogName 'Security' -RecordID 5287804 -Machine AD1.AD.EVOTEC.XYZ | fl Message, MemberName, MemberNameWithoutCN
#Get-Events -LogName 'Security' -RecordID 5844279 -Machine AD1.AD.EVOTEC.XYZ | fl Message, MemberName, MemberNameWithoutCN


$MemberName = @(
    'CN=Weird\, Name\, with   ,OU=Users-Offboarded,OU=Production,DC=ad,DC=evotec,DC=xyz'
    'CN=Weird Name\, with $\,.,OU=Users-Offboarded,OU=Production,DC=ad,DC=evotec,DC=xyz'
    'CN=Weird Name,OU=Users-Offboarded,OU=Production,DC=ad,DC=evotec,DC=xyz'
    'CN=Weird Name,DC=ad,DC=evotec,DC=xyz'
    'CN=Weird Name,DC=ad,DC=evotec,DC=xyz'
    'CN=Mailbox Database 1527735546,CN=Databases,CN=Exchange Administrative Group (FYDIBOHF23SPDLT),CN=Administrative Groups,CN=Evotec,CN=Microsoft Exchange,CN=Services,CN=Configuration,DC=ad,DC=evotec,DC=xyz'
    'CN=Test My\, User,OU=Users-Offboarded,OU=Production,DC=ad,DC=evotec,DC=xyz'
)

Write-Color '- Members' -Color Yellow
foreach ($Member in $MemberName) {

    $Member
}

Write-Color '- MyVersion' -Color Green
foreach ($Member in $MemberName) {

    $Member -replace '^CN=|,(OU|DC|CN).*$'
}
Write-Color '- NewVersion' -Color Red
foreach ($Member in $MemberName) {
  
    $Member -replace 'CN=|\\,|,(OU|DC|CN).*$'
}

Write-Color '- Final version?' -Color Blue
foreach ($Member in $MemberName) {
  
    $Member -replace 'CN=|\\|,(OU|DC|CN).*$'
}