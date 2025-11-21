Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$Output = Find-WinEvent -LogName 'Security' -EventId 4627 -Verbose -MachineName 'AD1', 'AD2', 'AD0', "ADRODC.ad.evotec.pl" -ParallelOption Parallel | Select-Object -First 2
#$Output
#$Output[0] | Format-List
#$Output[0].MessageData
return

Find-WinEvent -Machine AD0 -LogName Security 5136, 5137, 5139, 5141, 4741, 4742, 4740, 4727, 4730, 4731, 4734, 4744, 4748, 4749, 4753, 4754, 4758, 4759, 4763 -StartTime (Get-Date).AddDays(-1) -EndTime (Get-Date) -Verbose | Format-Table

Find-WinEvent -Machine AD0, AD1, AD2 -LogName Security -Id 4625 -Verbose  #-StartTime (Get-Date).AddDays(-1) -EndTime (Get-Date) -Verbose | Format-Table

Find-WinEvent -Machine AD0, AD1, AD2 -LogName Security -Id 4624 -Verbose  #-StartTime (Get-Date).AddDays(-1) -EndTime (Get-Date) -Verbose | Format-Table