Import-Module PSEventViewer -Force

#$WrongCredentials = (Get-Credential)
$Credentials = (Get-Credential)

$AllEvents = Get-Events -LogName 'Application' -ID 16384 -Machine 'AD1' -MaxEvents 3 -Verbose -ErrorVariable Test -Credential $Credentials

$AllEvents | Format-Table -a

$Test | Format-Table -a


#Get-WinEvent -LogName Application -ComputerName AD1 -Credential $Credentials -FilterHas

$FilterHashTable = @{
    LogName = 'Application'
    ID      = 16384
}
$Event1 = Get-WinEvent -FilterHashtable $FilterHashTable -ComputerName AD1 -MaxEvents 3 -Credential $Credentials
$Event1 | Format-Table -a



return

$Data = @(
    if ($AllEvents[2].NoNameB1 -match "\n") {
        $AllEvents[2].NoNameB1 -split '\n'
    }
)
$AllEvents[2] | Format-List *