$User = 'Administrator'
$XML = Get-EventsFilter -NamedDataFilter @{'SubjectUserName' = $User; 'TargetUserName' = $User } -LogName 'ForwardedEvents'

<# Output of command above
    <QueryList>
        <Query Id="0" Path="ForwardedEvents">
            <Select Path="ForwardedEvents">
                    *[EventData[(Data[@Name='SubjectUserName'] = 'Administrator') or (Data[@Name='TargetUserName'] = 'Administrator')]]
            </Select>
        </Query>
    </QueryList>
#>
#$XML
Get-WinEvent -FilterXml $XML