Describe 'Start-EVXWatcher - Parameter validation' {
    It 'Fails when NumberOfThreads is less than 1' {
        { Start-EVXWatcher -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1 -Action {} -NumberOfThreads 0 } | Should -Throw
    }
    It 'Fails when NumberOfThreads is greater than 1024' {
        { Start-EVXWatcher -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1 -Action {} -NumberOfThreads 2000 } | Should -Throw
    }

    It 'Accepts timeout parameters' {
        { Start-EVXWatcher -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1 -Action {} -TimeoutSeconds 1 -StopOnMatch } | Should -Not -Throw
    }

    It 'Registers and starts watcher by name' {
        Register-EVXWatcher -Name 'TestWatcher' -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1 -Action {}
        $id = Start-EVXWatcher -Name 'TestWatcher'
        ($id | Should -Not -BeNullOrEmpty)
        Get-EVXWatcher -Running | Where-Object { $_.Name -eq $id } | Should -Not -BeNullOrEmpty
        Remove-EVXWatcher -Name $id | Should -Be $true
    }
}
