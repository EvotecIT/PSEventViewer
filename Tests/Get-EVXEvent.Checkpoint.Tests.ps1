Describe 'Get-EVXEvent checkpoint compatibility' {
    It 'honors legacy default keys instead of replaying records after upgrade' {
        $Latest = Get-EVXEvent -LogName System -MaxEvents 1 -ReadMode Metadata | Select-Object -First 1
        if (-not $Latest -or -not $Latest.RecordId) {
            Set-ItResult -Skipped -Because 'The System event log contained no checkpointable events.'
            return
        }

        $CheckpointPath = Join-Path $TestDrive 'legacy-checkpoint.json'
        $LegacyCheckpoint = @{ 'System|' = [long] $Latest.RecordId } | ConvertTo-Json -Compress
        Set-Content -LiteralPath $CheckpointPath -Value $LegacyCheckpoint -Encoding UTF8

        $QueryOutput = @(Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -MaxEvents 1 -MaxEventsScanned 5 -ReadMode Metadata -Verbose 4>&1)
        $Events = @($QueryOutput | Where-Object { $_ -isnot [System.Management.Automation.VerboseRecord] })
        $VerboseText = @($QueryOutput | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] } | ForEach-Object Message) -join [Environment]::NewLine

        @($Events | Where-Object { $_.RecordId -le $Latest.RecordId }) | Should -BeNullOrEmpty
        $VerboseText | Should -Match 'EventRecordID&gt;'
    }

    It 'advances the checkpoint for scanned records rejected by MessageRegex' {
        $CheckpointPath = Join-Path $TestDrive 'filtered-progress-checkpoint.json'

        $Events = @(Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -RecordIdKey 'filtered-progress' -MessageRegex '(?!)' -MaxEventsScanned 1 -ReadMode Message)

        $Events | Should -BeNullOrEmpty
        Test-Path -LiteralPath $CheckpointPath | Should -BeTrue
        $Checkpoint = Get-Content -LiteralPath $CheckpointPath -Raw | ConvertFrom-Json
        [long] $Checkpoint.'filtered-progress' | Should -BeGreaterThan 0
    }

    It 'restarts a checkpoint when the current log record IDs are lower after reset or replacement' {
        $CheckpointPath = Join-Path $TestDrive 'reset-checkpoint.json'
        @{ 'reset-proof' = [long]::MaxValue } | ConvertTo-Json -Compress |
            Set-Content -LiteralPath $CheckpointPath -Encoding UTF8

        $Events = @(Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -RecordIdKey 'reset-proof' -MaxEvents 1 -ReadMode Metadata -WarningAction SilentlyContinue)

        $Events.Count | Should -Be 1
        $Events[0].RecordId | Should -BeLessThan ([long]::MaxValue)
        $Persisted = Get-Content -LiteralPath $CheckpointPath -Raw | ConvertFrom-Json
        [long] $Persisted.'reset-proof' | Should -Be ([long] $Events[0].RecordId)
    }

    It 'uses distinct default checkpoints for distinct filtered queries' {
        $CheckpointPath = Join-Path $TestDrive 'filter-identities.json'

        $null = @(Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -MessageRegex '(?!)' -MaxEventsScanned 1 -ReadMode Message)
        $null = @(Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -MessageRegex '.*' -MaxEventsScanned 1 -ReadMode Message)

        $Persisted = Get-Content -LiteralPath $CheckpointPath -Raw | ConvertFrom-Json
        @($Persisted.PSObject.Properties).Count | Should -Be 2
    }
}
