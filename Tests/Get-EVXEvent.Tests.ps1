BeforeAll {
    $script:TestEventId = 5617
    $script:TestLogName = 'Application'
    $script:DateFrom = (Get-Date).AddDays(-60)
    $script:DateTo = Get-Date

    # Ensure at least one matching event exists on the local machine so tests are deterministic
    try {
        $existing = Get-EVXEvent -LogName $script:TestLogName -Id $script:TestEventId -DateFrom $script:DateFrom -DateTo $script:DateTo -MaxEvents 1 -AsArray -ParallelOption Disabled -ErrorAction SilentlyContinue
    } catch {
        $existing = @()
    }
    $needed = 2
    $have = if ($existing) { [int]$existing.Count } else { 0 }
    for ($i = $have; $i -lt $needed; $i++) {
        Write-EVXEntry -LogName $script:TestLogName -ProviderName 'PSEventViewer.Tests' -EventId $script:TestEventId -Message "PSEventViewer test event #$($i+1) (ensures deterministic tests)" -EventLogEntryType Information -Category 0 -ErrorAction SilentlyContinue | Out-Null
        Start-Sleep -Milliseconds 500
    }
}

Describe 'Get-EVXEvent - Basic Test' {
    $Date = $script:DateFrom
    $Date1 = $script:DateTo

    $Events = Get-EVXEvent -Machine $Env:COMPUTERNAME -DateFrom $Date -DateTo $Date1 -ID $script:TestEventId -LogName $script:TestLogName # -Verbose

    $PSDefaultParameterValues = @{
        "It:TestCases" = @{ Date = $Date; Date1 = $Date1; Events = $Events }
    }

    It 'Should have GatheredLogName, GatheredFrom fields properly filled in' {
        $Events[0].GatheredFrom | Should -Be $Env:COMPUTERNAME
        $Events[0].GatheredLogName | Should -Be $script:TestLogName
    }
    It 'Should have more then 1 event' {
        $Events.Count | Should -BeGreaterOrEqual 1
    }
    It 'Should return an Array' {
        $Events -is [Array] | Should -Be $true
    }
    It 'Should return proper Level' {
        $Events[0].LevelDisplayName | Should -Be 'Information'
    }
    It 'Should return proper LogName' {
        $Events[0].LogName | Should -Be $script:TestLogName
    }
    It 'Should return proper ID (EventID)' {
        $Events[0].ID | Should -Be $script:TestEventId
    }
}
Describe 'Get-EVXEvent - MaxEvents Test' {
    $Date = $script:DateFrom
    $Date1 = $script:DateTo

    $Events = Get-EVXEvent -Machine $Env:COMPUTERNAME -DateFrom $Date -DateTo $Date1 -ID $script:TestEventId -LogName $script:TestLogName -MaxEvents 1 -AsArray

    $PSDefaultParameterValues = @{
        "It:TestCases" = @{ Date = $Date; Date1 = $Date1; Events = $Events }
    }

    It 'Should have GatheredLogName, GatheredFrom fields properly filled in' {
        $Events[0].GatheredFrom | Should -Be $Env:COMPUTERNAME
        $Events[0].GatheredLogName | Should -Be $script:TestLogName
    }
    It 'Should have exactly 1 event' {
        $Events.Count | Should -BeExactly 1
    }
    It 'Should return an Array' {
        $Events -is [Array] | Should -Be $true
    }
    It 'Should return proper Level' {
        $Events[0].LevelDisplayName | Should -Be 'Information'
    }
    It 'Should return proper LogName' {
        $Events[0].LogName | Should -Be $script:TestLogName
    }
    It 'Should return proper ID (EventID)' {
        $Events[0].ID | Should -Be $script:TestEventId
    }
}

Describe 'Get-EVXEvent - MaxEvents on 3 servers' {
    $Date = $script:DateFrom
    $Date1 = $script:DateTo

    $Events = Get-EVXEvent -Machine $Env:COMPUTERNAME, $Env:COMPUTERNAME, $Env:COMPUTERNAME -DateFrom $Date -DateTo $Date1 -ID $script:TestEventId -LogName $script:TestLogName -MaxEvents 1 -AsArray

    $PSDefaultParameterValues = @{
        "It:TestCases" = @{ Date = $Date; Date1 = $Date1; Events = $Events }
    }

    It 'Should have GatheredLogName, GatheredFrom fields properly filled in' {
        $Events[0].GatheredFrom | Should -Be $Env:COMPUTERNAME
        $Events[0].GatheredLogName | Should -Be $script:TestLogName
        $Events[1].GatheredFrom | Should -Be $Env:COMPUTERNAME
        $Events[1].GatheredLogName | Should -Be $script:TestLogName
        $Events[2].GatheredFrom | Should -Be $Env:COMPUTERNAME
        $Events[2].GatheredLogName | Should -Be $script:TestLogName
    }
    It 'Should have exactly 1 event' {
        $Events.Count | Should -BeExactly 3
    }
    It 'Should return an Array' {
        $Events -is [Array] | Should -Be $true
    }
    It 'Should return proper Level' {
        $Events[0].LevelDisplayName | Should -Be 'Information'
        $Events[1].LevelDisplayName | Should -Be 'Information'
        $Events[2].LevelDisplayName | Should -Be 'Information'
    }
    It 'Should return proper LogName' {
        $Events[0].LogName | Should -Be $script:TestLogName
        $Events[1].LogName | Should -Be $script:TestLogName
        $Events[2].LogName | Should -Be $script:TestLogName
    }
    It 'Should return proper ID (EventID)' {
        $Events[0].ID | Should -Be $script:TestEventId
        $Events[1].ID | Should -Be $script:TestEventId
        $Events[2].ID | Should -Be $script:TestEventId
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
