Import-Module PSEventViewer -Force
Clear-Host
Write-Color 'Start processing events - Tests for expected output' -Color Red
# #, 4722, 4723, 4724, 4725, 4726, 4738, 4740, 4767
$TestServers = 'AD1.ad.evotec.xyz'
$ID = 104
$TestEvents1 = Get-Events -Machine $TestServers -Id $ID -LogName 'System' -MaxEvents 1 #-DisableParallel #-Verbose
$ID = 16384
$TestEvents2 = Get-Events -Machine $TestServers -Id $ID -LogName 'Application' -MaxEvents 1 #-DisableParallel #-Verbose
$ID = 4634
$TestEvents3 = Get-Events -Machine $TestServers -Id $ID -LogName 'Security' -MaxEvents 1 #-DisableParallel #-Verbose
$ID = 4688
$TestEvents4 = Get-Events -Machine $TestServers -Id $ID -LogName 'Security' -MaxEvents 1 #-DisableParallel #-Verbose
$ID = 105
$TestEvents5 = Get-Events -Machine $TestServers -Id $ID -LogName 'Application' -MaxEvents 1  #-DisableParallel  #-Verbose
$ID = 7036
$TestEvents6 = Get-Events -Machine $TestServers -Id $ID -LogName 'System' -MaxEvents 1 #-DisableParallel #-Verbose
$ID = 32
$TestEvents7 = Get-Events -Machine $TestServers -Id $ID -LogName 'System' -MaxEvents 1 #-DisableParallel #-Verbose
$ID = 1014
$TestEvents8 = Get-Events -Machine $TestServers -Id $ID -LogName 'System' -MaxEvents 1 #-DisableParallel #-Verbose
$ID = 8198
$TestEvents9 = Get-Events -Machine $TestServers -Id $ID -LogName 'Application' -MaxEvents 1  #-DisableParallel  #-Verbose
$ID = 10154
$TestEvents10 = Get-Events -Machine $TestServers -Id $ID -LogName 'System' -MaxEvents 1 #-DisableParallel #-Verbose

Write-Color 'Jump 1' -Color Green
$TestEvents1 | fl Channel, BackupPath, SubjectDomainName, SubjectUserName, MessageSubject, Message
Write-Color 'Jump 2' -Color Yellow
$TestEvents2 | fl NoNameA0, NoNameA1, MessageSubject, Message
Write-Color 'Jump 3' -Color Green
$TestEvents3 | fl TargetUserName, TargetDomainName, TargetUserSid, MessageSubject, Message
Write-Color 'Jump 4' -Color Green
$TestEvents4 | fl Computer, SubjectUserSid, NewProcessName, ParentProcessName, MessageSubject
Write-Color 'Jump 5' -Color Green
$TestEvents5 | fl NoNameA0, NoNameA1, NoNameA2, MessageSubject
Write-Color 'Jump 6' -Color Green
$TestEvents6 | fl param1, param2, MessageSubject
Write-Color 'Jump 7' -Color Green
$TestEvents7 | fl NoNameB1, NoNameB2, MessageSubject
Write-Color 'Jump 8' -Color Green
$TestEvents8 | fl QueryName, AddressLength, MessageSubject
Write-Color 'Jump 9' -Color Green
$TestEvents9 | fl NoNameA0, NoNameA1, MessageSubject
Write-Color 'Jump 10' -Color Green
$TestEvents10 | fl spn1, spn2, MessageSubject
Write-Color 'End processing events - Tests for expected output' -Color Red