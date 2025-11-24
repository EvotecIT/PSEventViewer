Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force -Verbose

$T = Find-WinEvent -Machine AD0 -Type ADUser -Verbose  # | Format-Table
$T.Count
return

$Data = Find-WinEvent -MachineName AD0 -EventRecordId '28907707' -LogName Security

#Find-WinEvent -Type ADComputerChangeDetailed, ADComputerCreateChange, ADUserLockouts, ADGroupCreateDelete , ADLdapBindingDetails -MachineName AD1, AD2, AD0 -Verbose | Format-Table

# Measure-Command {
#     Find-WinEvent -Type ADComputerChangeDetailed, ADComputerCreateChange, ADUserLockouts, ADGroupCreateDelete -MachineName AD1, AD2, AD0 -Verbose -StartTime (Get-Date).AddDays(-1)

# }

Measure-Command { $T = Find-WinEvent -Machine AD0 -LogName Security -EventId 5136, 5137, 5139, 5141, 4741, 4742, 4740, 4727, 4730, 4731, 4734, 4744, 4748, 4749, 4753, 4754, 4758, 4759, 4763 -DateFrom (Get-Date).AddDays(-1) -DateTo (Get-Date) -Verbose }