Import-Module PSEventViewer -Force
Clear-Host
Write-Color 'Start processing events - Tests for expected output' -Color Red
# #, 4722, 4723, 4724, 4725, 4726, 4738, 4740, 4767
$TestServers = 'AD1.ad.evotec.xyz'
$ID = 104
$TestEvents1 = Get-Events -Machine $TestServers -Id $ID -LogName 'System' -MaxEvents 5 #-DisableParallel #-Verbose
$ID = 16384
$TestEvents2 = Get-Events -Machine $TestServers -Id $ID -LogName 'Application' -MaxEvents 5 #-DisableParallel #-Verbose
$ID = 4634
$TestEvents3 = Get-Events -Machine $TestServers -Id $ID -LogName 'Security' -MaxEvents 5 #-DisableParallel #-Verbose
$ID = 4688
$TestEvents4 = Get-Events -Machine $TestServers -Id $ID -LogName 'Security' -MaxEvents 5 #-DisableParallel #-Verbose
$ID = 105
$TestEvents5 = Get-Events -Machine $TestServers -Id $ID -LogName 'Application' -MaxEvents 5  #-DisableParallel  #-Verbose
$ID = 7036
$TestEvents6 = Get-Events -Machine $TestServers -Id $ID -LogName 'System' -MaxEvents 5 #-DisableParallel #-Verbose
$ID = 32
$TestEvents7 = Get-Events -Machine $TestServers -Id $ID -LogName 'System' -MaxEvents 5 #-DisableParallel #-Verbose
$ID = 1014
$TestEvents8 = Get-Events -Machine $TestServers -Id $ID -LogName 'System' -MaxEvents 5 #-DisableParallel #-Verbose

Write-Color 'Jump 1' -Color Green
$TestEvents1 | fl Channel, BackupPath, SubjectDomainName, SubjectUserName
Write-Color 'Jump 2' -Color Yellow
$TestEvents2 | fl NoNameA0, NoNameA1
Write-Color 'Jump 3' -Color Green
$TestEvents3 | fl TargetUserName, TargetDomainName, TargetUserSid
Write-Color 'Jump 4' -Color Green
$TestEvents4 | fl Computer, SubjectUserSid, NewProcessName, ParentProcessName
Write-Color 'Jump 5' -Color Green
$TestEvents5 | fl NoNameA0, NoNameA1, NoNameA2
Write-Color 'Jump 6' -Color Green
$TestEvents6 | fl param1, param2
Write-Color 'Jump 7' -Color Green
$TestEvents7 | fl NoNameB1, NoNameB2
Write-Color 'Jump 8' -Color Green
$TestEvents8 | fl QueryName, AddressLength
Write-Color 'End processing events - Tests for expected output' -Color Red