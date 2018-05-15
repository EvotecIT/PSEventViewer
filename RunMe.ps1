Import-Module PSEventViewer -Force

Clear-Host

$ID = 1102, 5136, 5137, 5141, 4364, 4647, 4672, 4727, 4730, 4731, 4734, 4759, 4760, 4754, 4758, 4728, 4729, 4732, 4733, 4756, 4757, 4761, 4762, 4725, 4722, 4725, 4767, 4723, 4724, 4726, 4740, 4720, 4738

$DateFrom = (get-date).AddHours(-5)
$DateTo = (get-date).AddHours(1)

Get-Events -DateFrom $DateFrom -DateTo $DateTo -EventId 916 -LogType 'Application'
Get-Events -DateFrom $DateFrom -DateTo $DateTo -EventId 916 -LogType 'Application' -MaxEvents 10 -Verbose
Get-Events -EventId 916 -LogType 'Application' -MaxEvents 10 -Verbose
Get-Events -Id $ID -LogName 'Security' -DateFrom $DateFrom -DateTo $DateTo -Verbose