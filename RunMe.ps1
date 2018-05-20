Import-Module PSEventViewer -Force
Clear-Host

# Define dates
$DateFrom = (get-date).AddHours(-11)
$DateTo = (get-date).AddHours(1)

$IDRequiringSplitOver23 = 1102, 5136, 5137, 5141, 4364, 4647, 4672, 4727, 4730, 4731, 4734, 4759, 4760, 4754, 4758, 4728, 4729, 4732, 4733, 4756, 4757, 4761, 4762, 4725, 4722, 4725, 4767, 4723, 4724, 4726, 4740, 4720, 4738
$IDNotRequiringSplit = 1102, 5136, 5137, 5141, 4364, 4647, 4672, 4727, 4730, 4731, 4734, 4759, 4760, 4754, 4758, 4728, 4729, 4732, 4733, 4756
$ID = 916

$TestServers = 'AD1'
<#
Get-Events -DateFrom $DateFrom -DateTo $DateTo -EventId $id -LogType 'Application' -Verbose -maxevents 5 # | fl *
Get-Events -DateFrom $DateFrom -DateTo $DateTo -EventId 916 -LogType 'Application' -MaxEvents 10 -Verbose
Get-Events -EventId 916 -LogType 'Application' -MaxEvents 10 -Verbose
Get-Events -Id $ID -LogName 'Security' -DateFrom $DateFrom -DateTo $DateTo -Verbose

$TestEvents = Get-Events -DateFrom $DateFrom -DateTo $DateTo -EventId $id -LogType 'Application' -Verbose -Maxevents 5 -Oldest
$TestEvents | Select-Object Computer, TimeCreated, Id, LevelDisplayName, Message | Format-Table -AutoSize

$TestEvents1 = Get-Events -Machine $TestServers -Id $IDNotRequiringSplit -LogName 'Security' -DateFrom $DateFrom -DateTo $DateTo -Verbose
$TestEvents1 | Select-Object Computer, TimeCreated, Id, LevelDisplayName, Message | Format-Table -AutoSize

$TestEvents2 = Get-Events -Machine $TestServers -Id $IDRequiringSplitOver23 -LogName 'Security' -DateFrom $DateFrom -DateTo $DateTo -Verbose
$TestEvents2 | Select-Object Computer, TimeCreated, Id, LevelDisplayName, Message | Format-Table -AutoSize

$ID = 4720, 4738
$TestEvents3 = Get-Events -Machine $TestServers -Id $ID -LogName 'Security' -Verbose
$TestEvents3
#$TestEvents3 | Select-Object Computer, TimeCreated, Id, LevelDisplayName, Message | Format-Table -AutoSize
#>