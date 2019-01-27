Clear-Host
Import-Module PSSharedGoods -Force
Import-Module ..\PSEventViewer.psd1 -force
Write-Color "Start 1" -Color Red
$Output1 = Get-Events -LogName 'Setup' -ID 2 -ComputerName 'Evo1' -MaxEvents 1 -Verbose #| Format-Table *
Write-Color 'Start 2' -Color Red
$Output2 = Get-Events -LogName 'Setup' -ComputerName 'Evo1' -MaxEvents 1 -Verbose #| Format-Table *
Write-Color 'Start 3' -Color Blue
$Output3 = Get-Events -LogName 'Security' -ComputerName 'AD1.AD.EVOTEC.XYZ' -ID 4720, 4738, 5136, 5137, 5141, 4722, 4725, 4767, 4723, 4724, 4726, 4728, 4729, 4732, 4733, 4746, 4747, 4751, 4752, 4756, 4757, 4761, 4762, 4785, 4786, 4787, 4788, 5136, 5137, 5141, 5136, 5137, 5141, 5136, 5137, 5141 -Verbose
Write-Color 'Start 4' -Color Blue
$List = @()
#$List += @{ Server = 'AD1'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'Computer' }
#$List += @{ Server = 'AD2'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'Computer' }
#$List += @{ Server = 'C:\MyEvents\Archive-Security-2018-08-21-23-49-19-424.evtx'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'File' }
#$List += @{ Server = 'C:\MyEvents\Archive-Security-2018-09-15-09-27-52-679.evtx'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'File' }
#$List += @{ Server = 'Evo1'; LogName = 'Setup'; EventID = 2; Type = 'Computer'; }
$List += @{ Server = 'AD1.ad.evotec.xyz'; LogName = 'Security'; EventID = 4720, 4738, 5136, 5137, 5141, 4722, 4725, 4767, 4723, 4724, 4726, 4728, 4729, 4732, 4733, 4746, 4747, 4751, 4752, 4756, 4757, 4761, 4762, 4785, 4786, 4787, 4788, 5136, 5137, 5141, 5136, 5137, 5141, 5136, 5137, 5141; Type = 'Computer' }
#$List += @{ Server = 'Evo1'; LogName = 'Setup'; Type = 'Computer'; }
$Output4 = Get-Events -ExtendedInput $List -Verbose

Write-Color "End 1" -Color Red
$Output1 | Format-Table -AutoSize

Write-Color "End 2" -Color Red
$Output2 | Format-Table -AutoSize

Write-Color 'END 3' -Color Blue
$Output3 | Format-Table -AutoSize

Write-Color "End 4" -Color Red
$Output4 | Format-Table -AutoSize