Describe 'Find-WinEvent integration (local)' {
    $localMachine = $env:COMPUTERNAME

    It 'lists Application/System logs without error' {
        $logs = Find-WinEvent -Verbose -ListLog 'Application','System' -MachineName $localMachine
        $logs | Should -Not -BeNullOrEmpty
        $logs.LogName | Should -Contain 'Application'
        $logs.LogName | Should -Contain 'System'
    }

    It 'queries System log events without throwing' {
        { Find-WinEvent -Verbose -LogName 'System' -MachineName $localMachine -MaxEvents 1 | Select-Object -First 1 } | Should -Not -Throw
    }

    It 'queries Application log events without throwing' {
        { Find-WinEvent -Verbose -LogName 'Application' -MachineName $localMachine -MaxEvents 1 | Select-Object -First 1 } | Should -Not -Throw
    }

    It 'returns events from Application locally when MachineName is omitted' {
        $events = Find-WinEvent -Verbose -LogName 'Application' -MaxEvents 2
        $events | Should -Not -BeNullOrEmpty
        $events.Count = 2
    }

    It 'runs on local machine without MachineName specified' {
        $events = Find-WinEvent -Verbose -LogName 'System' -MaxEvents 2
        $events | Should -Not -BeNullOrEmpty
        $events.Count = 2
    }

    It 'queries a named event type without throwing (OS startup)' {
        { Find-WinEvent -Verbose -Type OSStartup -MachineName $localMachine -MaxEvents 1 | Select-Object -First 1 } | Should -Not -Throw
    }
}
