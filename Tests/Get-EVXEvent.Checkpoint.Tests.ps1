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

        $Events = @(Get-EVXEvent -LogName System -RecordIdFile $CheckpointPath -MaxEvents 1 -MaxEventsScanned 5 -ReadMode Metadata)

        @($Events | Where-Object { $_.RecordId -le $Latest.RecordId }) | Should -BeNullOrEmpty
    }
}
