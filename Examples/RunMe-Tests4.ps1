$IDRequiringSplitOver23 = 1102, 5136, 5137, 5141, 4364, 4647, 4672, 4727, 4730, 4731, 4734, 4759, 4760, 4754, 4758, 4728, 4729, 4732, 4733, 4756, 4757, 4761, 4762, 4725, 4722, 4725, 4767, 4723, 4724, 4726, 4740, 4720, 4738

Get-WinEvent -FilterHashtable @{ LogName = 'Security'; ID = $IDRequiringSplitOver23 }
Get-Events -Id $IDRequiringSplitOver23 -LogName 'Security'
