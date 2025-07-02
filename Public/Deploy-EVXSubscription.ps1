function Deploy-EVXSubscription {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$SubscriptionXmlPath,

        [Parameter(Mandatory)]
        [string[]]$ComputerName,

        [System.Management.Automation.PSCredential]$Credential
    )

    foreach ($computer in $ComputerName) {
        $session = $null
        try {
            if ($Credential) {
                $session = New-PSSession -ComputerName $computer -Credential $Credential
            } else {
                $session = New-PSSession -ComputerName $computer
            }
            Copy-Item -Path $SubscriptionXmlPath -Destination "C:\\Windows\\Temp\\subscription.xml" -ToSession $session
            Invoke-Command -Session $session -ScriptBlock {
                wecutil cs C:\Windows\Temp\subscription.xml
            }
        } finally {
            if ($session) { Remove-PSSession -Session $session }
        }
    }
}
