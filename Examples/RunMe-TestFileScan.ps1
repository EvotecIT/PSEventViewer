Import-Module PSEventViewer -Force
Import-Module PSSharedGoods -Force

$DateFrom = Get-Date -Year 2018 -Month 09 -Day 12
$Events = Get-Events -Path 'C:\MyEvents\Archive-Security-2018-09-14-22-13-07-710.evtx' -ID 4799 -Verbose -DateFrom $DateFrom
$Events | ft -AutoSize