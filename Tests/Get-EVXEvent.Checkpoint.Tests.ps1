Describe 'Get-EVXEvent checkpoint compatibility' {
    It 'preserves SuppressHashFilter for checkpointed channel queries' {
        $CheckpointPath = Join-Path $TestDrive 'suppressed-checkpoint.json'
        $Latest = Get-EVXEvent `
            -LogName System `
            -MaxEvents 1 `
            -ReadMode Metadata
        if (-not $Latest) {
            Set-ItResult -Skipped -Because 'The System event log contained no events.'
            return
        }

        $Events = @(
            Get-EVXEvent `
                -FilterHashtable @{
                    LogName = 'System'
                    StartTime = $Latest.TimeCreated.AddSeconds(-1)
                    EndTime = $Latest.TimeCreated.AddSeconds(1)
                    SuppressHashFilter = @{
                        Id = $Latest.Id
                    }
                } `
                -RecordIdFile $CheckpointPath `
                -RecordIdKey 'suppressed' `
                -MaxEvents 10 `
                -ReadMode Metadata
        )

        $Events.Id | Should -Not -Contain $Latest.Id
    }

    It 'uses each offline file path as its multi-source checkpoint identity' {
        $Fixture = Join-Path $PSScriptRoot 'Logs\NamedFilterExamples.evtx'
        $FirstPath = Join-Path $TestDrive 'first.evtx'
        $SecondPath = Join-Path $TestDrive 'second.evtx'
        Copy-Item -LiteralPath $Fixture -Destination $FirstPath
        Copy-Item -LiteralPath $Fixture -Destination $SecondPath
        $FirstPath = [IO.Path]::GetFullPath($FirstPath)
        $SecondPath = [IO.Path]::GetFullPath($SecondPath)
        $CheckpointPath = Join-Path $TestDrive 'multi-file-checkpoint.json'
        $Checkpoint = @{}
        $Checkpoint["multi-file|$FirstPath|$FirstPath"] = [long]::MaxValue
        $Checkpoint["multi-file|$SecondPath|$SecondPath"] = [long]::MaxValue
        $Checkpoint | ConvertTo-Json -Compress |
            Set-Content -LiteralPath $CheckpointPath -Encoding UTF8

        $Events = @(
            Get-EVXEvent `
                -Path $FirstPath, $SecondPath `
                -RecordIdFile $CheckpointPath `
                -RecordIdKey 'multi-file' `
                -MaxEvents 2 `
                -ReadMode Metadata `
                -WarningAction SilentlyContinue
        )

        $Events.Count | Should -Be 2
        foreach ($Event in $Events) {
            $Event.RecordId | Should -BeLessThan ([long]::MaxValue)
        }
    }

    It 'uses each FilterXml channel as its checkpoint identity' {
        $System = Get-EVXEvent -LogName System -MaxEvents 1 -ReadMode Metadata |
            Select-Object -First 1
        $Application = Get-EVXEvent -LogName Application -MaxEvents 1 -ReadMode Metadata |
            Select-Object -First 1
        if (-not $System -or -not $Application) {
            Set-ItResult -Skipped -Because 'System and Application both require one checkpointable event.'
            return
        }

        [xml] $Query = @"
<QueryList>
  <Query Id="0" Path="System">
    <Select Path="System">*[System[EventRecordID=$($System.RecordId)]]</Select>
  </Query>
  <Query Id="1" Path="Application">
    <Select Path="Application">*[System[EventRecordID=$($Application.RecordId)]]</Select>
  </Query>
</QueryList>
"@
        $CheckpointPath = Join-Path $TestDrive 'filterxml-channels.json'

        $Events = @(
            Get-EVXEvent `
                -FilterXml $Query `
                -RecordIdFile $CheckpointPath `
                -RecordIdKey 'filterxml-channels' `
                -MaxEvents 2 `
                -ReadMode Metadata
        )

        $Events.Count | Should -Be 2
        $Persisted = Get-Content -LiteralPath $CheckpointPath -Raw |
            ConvertFrom-Json
        $Keys = @($Persisted.PSObject.Properties.Name)
        @($Keys | Where-Object { $_ -like 'filterxml-channels|*|System' }).Count |
            Should -Be 1
        @($Keys | Where-Object { $_ -like 'filterxml-channels|*|Application' }).Count |
            Should -Be 1
    }

    It 'honors legacy default keys instead of replaying records after upgrade' {
        $Latest = Get-EVXEvent -LogName System -MaxEvents 1 -ReadMode Metadata | Select-Object -First 1
        if (-not $Latest -or -not $Latest.RecordId) {
            Set-ItResult -Skipped -Because 'The System event log contained no checkpointable events.'
            return
        }

        $CheckpointPath = Join-Path $TestDrive 'legacy-checkpoint.json'
        $LegacyCheckpoint = @{ 'System|' = [long] $Latest.RecordId } | ConvertTo-Json -Compress
        Set-Content -LiteralPath $CheckpointPath -Value $LegacyCheckpoint -Encoding UTF8

        $Events = @(Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -MaxEvents 1 -MaxEventsScanned 5 -ReadMode Metadata)

        @($Events | Where-Object { $_.RecordId -le $Latest.RecordId }) | Should -BeNullOrEmpty
    }

    It 'does not fan out an aggregate legacy checkpoint across named-event sources' {
        $Baseline = @(Get-EVXEvent -Type OSStartup -MaxEvents 1)
        if ($Baseline.Count -eq 0) {
            Set-ItResult -Skipped -Because 'The local logs contained no OSStartup named event.'
            return
        }

        $CheckpointPath = Join-Path $TestDrive 'aggregate-named-checkpoint.json'
        @{ aggregate = [long]::MaxValue } | ConvertTo-Json -Compress |
            Set-Content -LiteralPath $CheckpointPath -Encoding UTF8

        $Events = @(Get-EVXEvent -Type OSStartup -RecordIdFile $CheckpointPath -RecordIdKey aggregate -MaxEvents 1)

        $Events.Count | Should -Be 1
    }

    It 'uses the same checkpoint layout for equivalent duplicate targets' {
        $CheckpointPath = Join-Path $TestDrive 'normalized-target-checkpoint.json'
        $First = @(Get-EVXEvent -LogName System -MachineName $env:COMPUTERNAME -RecordIdFile $CheckpointPath -RecordIdKey normalized -MaxEvents 1 -ReadMode Metadata)
        $Second = @(Get-EVXEvent -LogName System -MachineName $env:COMPUTERNAME, $env:COMPUTERNAME.ToLowerInvariant() -RecordIdFile $CheckpointPath -RecordIdKey normalized -MaxEvents 1 -ReadMode Metadata)
        if ($First.Count -eq 0 -or $Second.Count -eq 0) {
            Set-ItResult -Skipped -Because 'The System event log did not contain two checkpointable events.'
            return
        }

        [long] $Second[0].RecordId | Should -BeGreaterThan ([long] $First[0].RecordId)
        $Persisted = Get-Content -LiteralPath $CheckpointPath -Raw | ConvertFrom-Json
        @($Persisted.PSObject.Properties.Name) | Should -Contain normalized
        @($Persisted.PSObject.Properties.Name | Where-Object { $_ -like 'normalized|*' }) | Should -BeNullOrEmpty
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

    It 'uses distinct default checkpoints for distinct fallback message cultures' {
        $CheckpointPath = Join-Path $TestDrive 'fallback-culture-identities.json'

        $null = @(
            Get-EVXEvent `
                -LogName System `
                -RecordIdFile $CheckpointPath `
                -MessageRegex '(?!)' `
                -MessageCulture en-US `
                -FallbackMessageCulture pl-PL `
                -MaxEventsScanned 1 `
                -ReadMode Message
        )
        $null = @(
            Get-EVXEvent `
                -LogName System `
                -RecordIdFile $CheckpointPath `
                -MessageRegex '(?!)' `
                -MessageCulture en-US `
                -FallbackMessageCulture de-DE `
                -MaxEventsScanned 1 `
                -ReadMode Message
        )

        $Persisted = Get-Content -LiteralPath $CheckpointPath -Raw |
            ConvertFrom-Json
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

    It 'keeps a contiguous checkpoint across more than 22 native event-id chunks' {
        $Recent = @(Get-EVXEvent -LogName System -MaxEvents 500 -ReadMode Metadata)
        $PopularIds = @($Recent | Group-Object -Property Id | Sort-Object -Property Count -Descending | Select-Object -First 2 -ExpandProperty Name)
        if ($PopularIds.Count -lt 2) {
            Set-ItResult -Skipped -Because 'The System log did not contain two event IDs for a chunked checkpoint proof.'
            return
        }

        $SparseIds = [Collections.Generic.List[int]]::new()
        $SeenIds = [Collections.Generic.HashSet[int]]::new()
        foreach ($PopularId in $PopularIds) {
            $Value = [int] $PopularId
            if ($SeenIds.Add($Value)) {
                $SparseIds.Add($Value)
            }
        }
        $Value = 1000001
        while ($SparseIds.Count -lt 463) {
            if ($SeenIds.Add($Value)) {
                $SparseIds.Add($Value)
            }
            $Value++
        }

        $SparseIds.Count | Should -Be 463

        $CheckpointPath = Join-Path $TestDrive 'chunked-contiguous-checkpoint.json'
        $First = @(Get-EVXEvent -LogName System -EventId $SparseIds -RecordIdFile $CheckpointPath -RecordIdKey 'chunked-contiguous' -MaxEvents 2 -ReadMode Metadata)
        $Second = @(Get-EVXEvent -LogName System -EventId $SparseIds -RecordIdFile $CheckpointPath -RecordIdKey 'chunked-contiguous' -MaxEvents 2 -ReadMode Metadata)
        if ($First.Count -lt 2 -or $Second.Count -lt 2) {
            Set-ItResult -Skipped -Because 'The System log did not contain four events matching the chunked filter.'
            return
        }

        [long] ($Second | Measure-Object -Property RecordId -Minimum).Minimum |
            Should -BeGreaterThan ([long] ($First | Measure-Object -Property RecordId -Maximum).Maximum)
        $Persisted = Get-Content -LiteralPath $CheckpointPath -Raw | ConvertFrom-Json
        [long] $Persisted.'chunked-contiguous' | Should -Be ([long] ($Second | Measure-Object -Property RecordId -Maximum).Maximum)
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
