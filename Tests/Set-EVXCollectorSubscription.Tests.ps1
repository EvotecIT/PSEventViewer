Describe 'Set-EVXCollectorSubscription' {
    It 'exposes a confirmed local service-API write surface' {
        $Command = Get-Command Set-EVXCollectorSubscription
        $Command.Parameters.Keys | Should -Contain 'Name'
        $Command.Parameters.Keys | Should -Contain 'Enabled'
        $Command.Parameters.Keys | Should -Contain 'WhatIf'
        $Command.Parameters.Keys | Should -Contain 'Confirm'
    }

    It 'does not create a missing subscription' {
        $Name = 'PSEventViewer-Missing-' +
            [Guid]::NewGuid().ToString('N')
        {
            Set-EVXCollectorSubscription `
                -Name $Name `
                -Enabled $true `
                -Confirm:$false `
                -ErrorAction Stop
        } | Should -Throw
        Get-EVXCollectorSubscription -Name $Name |
            Should -BeNullOrEmpty
    }
}
