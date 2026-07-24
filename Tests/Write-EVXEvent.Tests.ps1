Describe 'Write-EVXEvent manifest provider writer' {
    It 'exports a thin schema-aware manifest writer' {
        $Command = Get-Command Write-EVXEvent

        $Command.CommandType | Should -Be 'Cmdlet'
        $Command.Parameters.Keys | Should -Contain 'ProviderName'
        $Command.Parameters.Keys | Should -Contain 'Id'
        $Command.Parameters.Keys | Should -Contain 'Version'
        $Command.Parameters.Keys | Should -Contain 'Payload'
        $Command.Parameters.Keys | Should -Contain 'WhatIf'
    }

    It 'validates payload count before writing' {
        {
            Write-EVXEvent `
                -ProviderName Microsoft-Windows-PowerShell `
                -Id 4100 `
                -Payload @('only-one') `
                -Confirm:$false `
                -ErrorAction Stop
        } | Should -Throw '*expects 3 payload value*'
    }

    It 'writes a registered event that both engines read identically' {
        $Marker = 'EVX-' + [guid]::NewGuid().ToString('N')
        $StartTime = (Get-Date).AddSeconds(-1)

        $Result = Write-EVXEvent `
            -ProviderName Microsoft-Windows-PowerShell `
            -Id 4100 `
            -Payload @('Context', $Marker, 'Payload') `
            -Confirm:$false `
            -ErrorAction Stop

        $EVXEvent = $null
        $WinEvent = $null
        foreach ($Attempt in 1..10) {
            $EVXEvent = Get-EVXEvent `
                -LogName Microsoft-Windows-PowerShell/Operational `
                -ProviderName Microsoft-Windows-PowerShell `
                -EventId 4100 `
                -StartTime $StartTime `
                -ReadMode Full `
                -MaxEvents 20 |
                Where-Object { $_.Data.Values -contains $Marker } |
                Select-Object -First 1
            $WinEvent = Get-WinEvent `
                -FilterHashtable @{
                    LogName = 'Microsoft-Windows-PowerShell/Operational'
                    ProviderName = 'Microsoft-Windows-PowerShell'
                    Id = 4100
                    StartTime = $StartTime
                } `
                -MaxEvents 20 `
                -ErrorAction SilentlyContinue |
                Where-Object { $_.Properties.Value -contains $Marker } |
                Select-Object -First 1
            if ($EVXEvent -and $WinEvent) {
                break
            }
            Start-Sleep -Milliseconds 100
        }

        $Result.Success | Should -BeTrue
        $Result.NativeStatus | Should -Be 0
        $Result.Definition.LogName |
            Should -Be 'Microsoft-Windows-PowerShell/Operational'
        $Result.Definition.PayloadFields.Name |
            Should -Be @('ContextInfo', 'UserData', 'Payload')
        $EVXEvent | Should -Not -BeNullOrEmpty
        $WinEvent | Should -Not -BeNullOrEmpty
        $EVXEvent.Data.Values |
            Should -Be $WinEvent.Properties.Value
    }
}
