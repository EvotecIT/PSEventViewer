Import-Module PSEventViewer -Force
$AllEvents = Get-Events -LogName 'Application' -ID 6946 -Machine 'ADConnect' -MaxEvents 10

$Data = @(
    if ($AllEvents[2].NoNameB1 -match "\n") {
        $AllEvents[2].NoNameB1 -split '\n'
    }
)
$AllEvents[2] | fl *