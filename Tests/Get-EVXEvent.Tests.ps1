BeforeDiscovery {
    # Only set constant values during discovery to avoid invoking cmdlets
    $script:TestEventId = 5617
    $script:TestLogName = 'Application'
    $script:DateFrom = (Get-Date).AddDays(-60)
    $script:DateTo = Get-Date
}

Describe 'Get-EVXEvent - Basic Test' {
    $Date = $script:DateFrom
    $Date1 = $script:DateTo

    BeforeAll {
        # Ensure at least two matching events exist
        try {
            $existing = Get-EVXEvent -LogName $script:TestLogName -Id $script:TestEventId -DateFrom $script:DateFrom -DateTo $script:DateTo -MaxEvents 2 -AsArray -ParallelOption Disabled -ErrorAction SilentlyContinue
        } catch { $existing = @() }
        $needed = 2
        $have = if ($existing) { [int]$existing.Count } else { 0 }
        for ($i = $have; $i -lt $needed; $i++) {
            Write-EVXEntry -LogName $script:TestLogName -ProviderName 'PSEventViewer.Tests' -EventId $script:TestEventId -Message "PSEventViewer test event #$($i+1) (ensures deterministic tests)" -EventLogEntryType Information -Category 0 -ErrorAction SilentlyContinue | Out-Null
            Start-Sleep -Milliseconds 250
        }
        $script:Events = Get-EVXEvent -Machine $Env:COMPUTERNAME -DateFrom $Date -DateTo $Date1 -ID $script:TestEventId -LogName $script:TestLogName -AsArray # -Verbose
    }

    $PSDefaultParameterValues = @{
        "It:TestCases" = @{ Date = $Date; Date1 = $Date1; Events = $Events }
    }

    It 'Should have GatheredLogName, GatheredFrom fields properly filled in' {
        $script:Events[0].GatheredFrom | Should -Be $Env:COMPUTERNAME
        $script:Events[0].GatheredLogName | Should -Be $script:TestLogName
    }
    It 'Should have more then 1 event' {
        $script:Events.Count | Should -BeGreaterOrEqual 1
    }
    It 'Should return an Array' {
        $script:Events -is [Array] | Should -Be $true
    }
    It 'Should return proper Level' {
        $script:Events[0].LevelDisplayName | Should -Be 'Information'
    }
    It 'Should return proper LogName' {
        $script:Events[0].LogName | Should -Be $script:TestLogName
    }
    It 'Should return proper ID (EventID)' {
        $script:Events[0].ID | Should -Be $script:TestEventId
    }
}
Describe 'Get-EVXEvent - MaxEvents Test' {
    $Date = $script:DateFrom
    $Date1 = $script:DateTo

    BeforeAll {
        # Ensure at least one matching event exists
        try {
            $existing = Get-EVXEvent -LogName $script:TestLogName -Id $script:TestEventId -DateFrom $script:DateFrom -DateTo $script:DateTo -MaxEvents 1 -AsArray -ParallelOption Disabled -ErrorAction SilentlyContinue
        } catch { $existing = @() }
        if (-not $existing -or $existing.Count -lt 1) {
            Write-EVXEntry -LogName $script:TestLogName -ProviderName 'PSEventViewer.Tests' -EventId $script:TestEventId -Message "PSEventViewer test event (ensures deterministic tests)" -EventLogEntryType Information -Category 0 -ErrorAction SilentlyContinue | Out-Null
            Start-Sleep -Milliseconds 250
        }
        $script:Events1 = Get-EVXEvent -Machine $Env:COMPUTERNAME -DateFrom $Date -DateTo $Date1 -ID $script:TestEventId -LogName $script:TestLogName -MaxEvents 1 -AsArray
    }

    $PSDefaultParameterValues = @{
        "It:TestCases" = @{ Date = $Date; Date1 = $Date1; Events = $Events }
    }

    It 'Should have GatheredLogName, GatheredFrom fields properly filled in' {
        $script:Events1[0].GatheredFrom | Should -Be $Env:COMPUTERNAME
        $script:Events1[0].GatheredLogName | Should -Be $script:TestLogName
    }
    It 'Should have exactly 1 event' {
        $script:Events1.Count | Should -BeExactly 1
    }
    It 'Should return an Array' {
        $script:Events1 -is [Array] | Should -Be $true
    }
    It 'Should return proper Level' {
        $script:Events1[0].LevelDisplayName | Should -Be 'Information'
    }
    It 'Should return proper LogName' {
        $script:Events1[0].LogName | Should -Be $script:TestLogName
    }
    It 'Should return proper ID (EventID)' {
        $script:Events1[0].ID | Should -Be $script:TestEventId
    }
}

Describe 'Get-EVXEvent - MaxEvents on 3 servers' {
    $Date = $script:DateFrom
    $Date1 = $script:DateTo

    BeforeAll {
        # Ensure at least one matching event exists per machine (same machine used thrice)
        try {
            $existing = Get-EVXEvent -LogName $script:TestLogName -Id $script:TestEventId -DateFrom $script:DateFrom -DateTo $script:DateTo -MaxEvents 1 -AsArray -ParallelOption Disabled -ErrorAction SilentlyContinue
        } catch { $existing = @() }
        if (-not $existing -or $existing.Count -lt 1) {
            Write-EVXEntry -LogName $script:TestLogName -ProviderName 'PSEventViewer.Tests' -EventId $script:TestEventId -Message "PSEventViewer test event (ensures deterministic tests)" -EventLogEntryType Information -Category 0 -ErrorAction SilentlyContinue | Out-Null
            Start-Sleep -Milliseconds 250
        }
        $script:Events3 = Get-EVXEvent -Machine $Env:COMPUTERNAME, $Env:COMPUTERNAME, $Env:COMPUTERNAME -DateFrom $Date -DateTo $Date1 -ID $script:TestEventId -LogName $script:TestLogName -MaxEvents 1 -AsArray
    }

    $PSDefaultParameterValues = @{
        "It:TestCases" = @{ Date = $Date; Date1 = $Date1; Events = $Events }
    }

    It 'Should have GatheredLogName, GatheredFrom fields properly filled in' {
        $script:Events3[0].GatheredFrom | Should -Be $Env:COMPUTERNAME
        $script:Events3[0].GatheredLogName | Should -Be $script:TestLogName
        $script:Events3[1].GatheredFrom | Should -Be $Env:COMPUTERNAME
        $script:Events3[1].GatheredLogName | Should -Be $script:TestLogName
        $script:Events3[2].GatheredFrom | Should -Be $Env:COMPUTERNAME
        $script:Events3[2].GatheredLogName | Should -Be $script:TestLogName
    }
    It 'Should have exactly 1 event' {
        $script:Events3.Count | Should -BeExactly 3
    }
    It 'Should return an Array' {
        $script:Events3 -is [Array] | Should -Be $true
    }
    It 'Should return proper Level' {
        $script:Events3[0].LevelDisplayName | Should -Be 'Information'
        $script:Events3[1].LevelDisplayName | Should -Be 'Information'
        $script:Events3[2].LevelDisplayName | Should -Be 'Information'
    }
    It 'Should return proper LogName' {
        $script:Events3[0].LogName | Should -Be $script:TestLogName
        $script:Events3[1].LogName | Should -Be $script:TestLogName
        $script:Events3[2].LogName | Should -Be $script:TestLogName
    }
    It 'Should return proper ID (EventID)' {
        $script:Events3[0].ID | Should -Be $script:TestEventId
        $script:Events3[1].ID | Should -Be $script:TestEventId
        $script:Events3[2].ID | Should -Be $script:TestEventId
    }
}

Describe 'Get-EVXEvent - Read events from path (oldest / newest)' {
    $FilePath = [System.IO.Path]::Combine($PSScriptRoot, 'Logs', 'Active Directory Web Services.evtx')

    $PSDefaultParameterValues = @{
        "It:TestCases" = @{ FilePath = $FilePath; }
    }

    It 'Should read 1 oldest event' {

        $Events = Get-EVXEvent -Path $FilePath -Oldest -MaxEvents 1 #-Verbose
        $Events.Count | Should -Be 1
        $Events[0].Id | Should -Be 1000
        $Events[0].GatheredFrom | Should -Be $FilePath
    }

    It 'Should read 1 newest event' {

        $EventsNewest = Get-EVXEvent -Path $FilePath -MaxEvents 1 -Expand
        $EventsNewest.Count | Should -Be 1
        $EventsNewest[0].Id | Should -Be 1200
        $EventsNewest[0].GatheredFrom | Should -Be $FilePath

        $EventsNewest[0].NoNameA0 | Should -Be 'GC'
        $EventsNewest[0].NoNameA1 | Should -Be 3268
        $EventsNewest[0].NoNameA2 | Should -Be 3269
    }
}

Describe 'Get-EVXEvent - Read events with NamedDataFilter' {
    $FilePath = [System.IO.Path]::Combine($PSScriptRoot, 'Logs', 'NamedFilterExamples.evtx')

    $PSDefaultParameterValues = @{
        "It:TestCases" = @{ FilePath = $FilePath; }
    }

    It 'Using -Path should not fail' {
        Get-EVXEvent -Path $FilePath -Oldest -MaxEvents 1 -DisableParallel -ErrorVariable err
        $err | Should -BeNullOrEmpty
    }

    It 'named exclude filter' {
        $ret = Get-EVXEvent -Path $FilePath -Id 7040 -NamedDataExcludeFilter @{ param4 = ('BITS', 'TrustedInstaller') } -MaxEvents 1 -AsArray -Expand
        $ret | Should -HaveCount 1
        ( [datetime] $ret.TimeCreated ) | Should -Be ( [datetime] "2019-08-30T06:57:44.037957100Z" )
        $ret.param4 | Should -Be 'NgcCtnrSvc'

    }
    It 'named include filter' {
        $ret = Get-EVXEvent -Path $FilePath -Id 7040 -NamedDataFilter @{ param4 = ('BITS', 'TrustedInstaller') } -oldest -MaxEvents 1 -AsArray -Expand
        $ret | Should -HaveCount 1
        ( [datetime] $ret.TimeCreated ) | Should -Be ( [datetime] "2019-08-30T06:50:13.213617700Z" )
        $ret.param4 | Should -Be 'BITS'

    }
}

Describe 'Get-EVXEvent - MessageRegex' {
    It 'Supports filtering by message regex' {
        $FilePath = [System.IO.Path]::Combine($PSScriptRoot, 'Logs', 'Active Directory Web Services.evtx')
        $events   = Get-EVXEvent -Path $FilePath -MaxEvents 1 -MessageRegex '.*'
        $events.Count | Should -Be 1
    }
}

Describe 'Get-EVXEvent - Parameter validation' {
    It 'Fails when NumberOfThreads is less than 1' {
        { Get-EVXEvent -LogName 'Application' -NumberOfThreads 0 } | Should -Throw
    }
}

Describe 'Get-EVXEvent - Positional EventId' {
    It 'Allows positional EventId without ambiguity' {
        $events = Get-EVXEvent $script:TestLogName $script:TestEventId -MaxEvents 1 -AsArray
        $events | Should -HaveCount 1
        $events[0].ID | Should -Be $script:TestEventId
    }
}
