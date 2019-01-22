Import-Module PSEventViewer -Force

$StartTime = (Get-Date ).AddDays(-100)

function Test {
    [CmdletBinding()]
    param()

    #$StartTime.ToUniversalTime()

    $xml = Get-EventsFilter -LogName 'ForwardedEvents' -RecordID '3512231', '3512232' -ProviderName 'Microsoft-Windows-Eventlog'

    # <QueryList><Query Id="0" Path="security"><Select Path="security">*[((System/EventID=4624))]</Select></Query></QueryList>.

    $xml
    Get-WinEvent -FilterXml $xml -Verbose:$true

}

#Test -Verbose

Write-Color "Output 1" -Color Red

Get-EventsFilter -StartTime '1/1/2015 01:30:00 PM' -EndTime '1/1/2015 02:00:00 PM' -LogName 'ForwardedEvents'


Write-Color "Output 2" -Color Red

$User = 'Administrator'
Get-EventsFilter -ID 4663 -NamedDataFilter @{'SubjectUserName' = $User; 'TargetUserName' = $User } -LogName 'ForwardedEvents'
