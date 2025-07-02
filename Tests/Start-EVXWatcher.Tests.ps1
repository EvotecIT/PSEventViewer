Describe 'Start-EVXWatcher - Parameter validation' {
    It 'Fails when NumberOfThreads is less than 1' {
        { Start-EVXWatcher -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1 -Action {} -NumberOfThreads 0 } | Should -Throw
    }
    It 'Fails when NumberOfThreads is greater than 1024' {
        { Start-EVXWatcher -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1 -Action {} -NumberOfThreads 2000 } | Should -Throw
    }
}
