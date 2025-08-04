Describe 'Get-EVXEvent - Parallel RecordId persistence' {
    It 'Returns an event for each parallel query' {
        $recordFile = Join-Path -Path $TestDrive -ChildPath 'records.json'

        $events = 1..5 | ForEach-Object -Parallel {
            param($key, $filePath)
            Import-Module -Name PSEventViewer -Force
            Get-EVXEvent -LogName 'Application' -MaxEvents 1 -RecordIdFile $filePath -RecordIdKey $key
        } -ArgumentList $recordFile

        $events | Should -HaveCount 5
        Get-Content -Path $recordFile -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
    }
}
