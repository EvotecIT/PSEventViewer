Import-Module PSEventViewer -Force
#Import-Module PSWriteColor

Clear-Host

$DateFrom = (get-date).AddHours(-5)
$DateTo = (get-date).AddHours(1)

#Get-Events -DateFrom $DateFrom -DateTo $DateTo -EventId 916 -LogType 'Application'
Get-Events -DateFrom $DateFrom -DateTo $DateTo -EventId 916 -LogType 'Application' -MaxEvents 10 -Verbose