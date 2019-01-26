#Get-WinEvent -FilterHashTable @{ LogName = 'ForwardedEvents'} -MaxEvents 5 -Verbose

$XML = @'
<QueryList><Query Id="0" Path="security"><Select Path="C:\System32\Winevt\Logs\ForwardedEvents.evtx">*</Select></Query></QueryList>
'@

Get-WinEvent -FilterXml $XML -MaxEvents 10

<#
function Test {
    [CmdletBinding()]
    param(

    )
    $e = Get-WinEvent -LogName 'ForwardedEvents' -MaxEvents 1 -Verbose
    $e

}

#>

#<QueryList><Query Id="0" Path="forwardedevents"><Select Path="forwardedevents">*[([EventData[Data[@Name='ContainerLog']='ForwardedEvents']] or [UserData/*/ContainerLog='ForwardedEvents'])]</Select></Query></QueryList>.
