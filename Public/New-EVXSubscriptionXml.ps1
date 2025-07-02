function New-EVXSubscriptionXml {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$SubscriptionId,

        [string]$Description,

        [Parameter(Mandatory)]
        [string]$Query,

        [string]$LogFile = 'ForwardedEvents',

        [string[]]$Computer,

        [string[]]$Group
    )

    $xml = [System.Xml.Linq.XElement]::new('Subscription',
        [System.Xml.Linq.XAttribute]::new('xmlns','http://schemas.microsoft.com/2006/03/windows/events/subscription'),
        [System.Xml.Linq.XElement]::new('SubscriptionId',$SubscriptionId),
        [System.Xml.Linq.XElement]::new('SubscriptionType','SourceInitiated')
    )
    if ($Description) {
        $xml.Add([System.Xml.Linq.XElement]::new('Description',$Description))
    }
    $xml.Add([System.Xml.Linq.XElement]::new('Query',[System.Xml.Linq.XCData]::new($Query)))
    $xml.Add([System.Xml.Linq.XElement]::new('LogFile',$LogFile))

    if ($Computer -or $Group) {
        $allowed = [System.Xml.Linq.XElement]::new('AllowedSourceDomainComputers')
        foreach ($c in $Computer) {
            $allowed.Add([System.Xml.Linq.XElement]::new('Computer', $c))
        }
        foreach ($g in $Group) {
            $comp = [System.Xml.Linq.XElement]::new('Computer')
            $comp.SetAttributeValue('Group', $g)
            $allowed.Add($comp)
        }
        $xml.Add($allowed)
    }
    return $xml.ToString()
}
