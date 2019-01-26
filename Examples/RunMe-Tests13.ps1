Clear-Host
Import-Module ..\PSEventViewer.psd1 -force
Write-Color "Start 1" -Color Red
$Output1 = Get-Events -LogName 'Setup' -ID 2 -ComputerName 'Evo1' -MaxEvents 1 -Verbose -DisableParallel #| Format-Table *
Write-Color 'Start 2' -Color Red
$Output2 = Get-Events -LogName 'Setup' -ComputerName 'Evo1' -MaxEvents 1 -Verbose #| Format-Table *
Write-Color 'Start 3' -Color Blue

$List = @()
$List += @{ Server = 'AD1'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'Computer' }
$List += @{ Server = 'AD2'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'Computer' }
$List += @{ Server = 'C:\MyEvents\Archive-Security-2018-08-21-23-49-19-424.evtx'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'File' }
$List += @{ Server = 'C:\MyEvents\Archive-Security-2018-09-15-09-27-52-679.evtx'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'File' }
$List += @{ Server = 'Evo1'; LogName = 'Setup'; EventID = 2; Type = 'Computer' }
$Output3 = Get-Events -ExtendedInput $List -Verbose

Write-Color "End 1" -Color Red
$Output1 | Format-Table -AutoSize

Write-Color "End 2" -Color Red
$Output2 | Format-Table -AutoSize

Write-Color "End 3" -Color Red
$Output3 | Format-Table -AutoSize
