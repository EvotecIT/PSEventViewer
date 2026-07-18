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

    It 'advances capped polling through a contiguous oldest-first prefix' {
        $CheckpointPath = Join-Path $TestDrive 'contiguous-checkpoint.json'

        $First = @(Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -RecordIdKey 'contiguous' -MaxEvents 1 -ReadMode Metadata)
        $Second = @(Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -RecordIdKey 'contiguous' -MaxEvents 1 -ReadMode Metadata)
        if ($First.Count -eq 0 -or $Second.Count -eq 0) {
            Set-ItResult -Skipped -Because 'The System event log did not contain two checkpointable events.'
            return
        }

        [long] $Second[0].RecordId | Should -BeGreaterThan ([long] $First[0].RecordId)
        $Persisted = Get-Content -LiteralPath $CheckpointPath -Raw | ConvertFrom-Json
        [long] $Persisted.contiguous | Should -Be ([long] $Second[0].RecordId)
    }

    It 'creates a missing checkpoint parent without retrying it as lock contention' {
        $CheckpointPath = Join-Path $TestDrive 'missing\parent\checkpoint.json'
        $Elapsed = Measure-Command {
            $null = @(Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -RecordIdKey 'nested' -MaxEvents 1 -ReadMode Metadata)
        }

        Test-Path -LiteralPath $CheckpointPath | Should -BeTrue
        $Elapsed.TotalSeconds | Should -BeLessThan 5
    }

    It 'rejects corrupt checkpoint JSON instead of silently replaying the log' {
        $CheckpointPath = Join-Path $TestDrive 'corrupt-checkpoint.json'
        Set-Content -LiteralPath $CheckpointPath -Value 'not-json' -Encoding UTF8

        { Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -MaxEvents 1 -ReadMode Metadata -ErrorAction Stop } |
            Should -Throw
    }

    It 'detects a replaced boundary even when the source has refilled above the checkpoint' {
        $CheckpointPath = Join-Path $TestDrive 'replaced-boundary-checkpoint.json'
        $Initial = @(Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -RecordIdKey 'replaced-boundary' -MaxEvents 3 -ReadMode Metadata)
        if ($Initial.Count -lt 3) {
            Set-ItResult -Skipped -Because 'The System event log did not contain three checkpointable events.'
            return
        }
        $PreviousBoundary = [long] $Initial[-1].RecordId

        $StatePath = $CheckpointPath + '.state.json'
        $State = Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
        $State.Checkpoints.'replaced-boundary'.BoundaryIdentity = 'SIMULATED-REPLACED-LOG-GENERATION'
        $State | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $StatePath -Encoding UTF8

        $AfterReplacement = @(Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -RecordIdKey 'replaced-boundary' -MaxEvents 1 -ReadMode Metadata -WarningAction SilentlyContinue)
        $AfterReplacement.Count | Should -Be 1
        [long] $AfterReplacement[0].RecordId | Should -BeLessThan $PreviousBoundary
    }

    It 'creates the same checkpoint boundary identity across read modes' {
        $Full = Get-EVXEvent -LogName System -MaxEvents 1 -ReadMode Full | Select-Object -First 1
        if (-not $Full -or -not $Full.RecordId) {
            Set-ItResult -Skipped -Because 'The System event log contained no checkpointable event.'
            return
        }

        $Metadata = Get-EVXEvent -LogName System -EventRecordId $Full.RecordId -MaxEvents 1 -ReadMode Metadata | Select-Object -First 1
        if (-not $Metadata) {
            Set-ItResult -Skipped -Because 'The selected System event aged out before it could be queried again.'
            return
        }

        [EventViewerX.EventCheckpointBoundaryIdentity]::Create($Full.PSObject.BaseObject) |
            Should -Be ([EventViewerX.EventCheckpointBoundaryIdentity]::Create($Metadata.PSObject.BaseObject))
    }
}
