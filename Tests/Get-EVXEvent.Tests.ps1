Describe 'Get-EVXEvent - Basic Test (EVTX sample)' {
    $FilePath = [System.IO.Path]::Combine($PSScriptRoot, 'Logs', 'Active Directory Web Services.evtx')

    $Events = Get-EVXEvent -Path $FilePath -MaxEvents 3

    $PSDefaultParameterValues = @{
        "It:TestCases" = @{ Date = $Date; Date1 = $Date1; Events = $Events }
    }

    It 'Should have GatheredFrom filled in' {
        $Events[0].GatheredFrom | Should -Not -BeNullOrEmpty
    }
    It 'Should have more then 1 event' {
        $Events.Count | Should -BeGreaterOrEqual 1
    }
    It 'Should return an Array' {
        $Events -is [Array] | Should -Be $true
    }
    It 'Should return proper LogName' {
        $Events[0].LogName | Should -Be 'Active Directory Web Services'
    }
}
Describe 'Get-EVXEvent - MaxEvents Test (EVTX sample)' {
    $FilePath = [System.IO.Path]::Combine($PSScriptRoot, 'Logs', 'Active Directory Web Services.evtx')

    $Events = Get-EVXEvent -Path $FilePath -MaxEvents 1

    $PSDefaultParameterValues = @{
        "It:TestCases" = @{ Date = $Date; Date1 = $Date1; Events = $Events }
    }

    It 'Should have GatheredFrom filled in' {
        $Events[0].GatheredFrom | Should -Not -BeNullOrEmpty
        $Events[0].LogName | Should -Be 'Active Directory Web Services'
    }
    It 'Should have exactly 1 event' {
        $Events.Count | Should -BeExactly 1
    }
    It 'Should return proper LogName' {
        $Events[0].LogName | Should -Be 'Active Directory Web Services'
    }
}

Describe 'Get-EVXEvent - MaxEvents on sample file' {
    It 'Should return up to requested events' {
        $FilePath = [System.IO.Path]::Combine($PSScriptRoot, 'Logs', 'Active Directory Web Services.evtx')
        $Events = Get-EVXEvent -Path $FilePath -MaxEvents 3
        $Events.Count | Should -BeGreaterThan 0
        $Events.Count | Should -BeLessOrEqual 3
    }
    It 'Should have GatheredFrom set to file path' {
        $FilePath = [System.IO.Path]::Combine($PSScriptRoot, 'Logs', 'Active Directory Web Services.evtx')
        $Events = Get-EVXEvent -Path $FilePath -MaxEvents 3
        ($Events | Select-Object -First 1).GatheredFrom | Should -Not -BeNullOrEmpty
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
        $FilePath = [System.IO.Path]::Combine($PSScriptRoot, 'Logs', 'Active Directory Web Services.evtx')
        $events = Get-EVXEvent -Path $FilePath -Id 1200 -MaxEvents 1
        $events | Should -Not -BeNullOrEmpty
        $events[0].ID | Should -Be 1200
    }
}
