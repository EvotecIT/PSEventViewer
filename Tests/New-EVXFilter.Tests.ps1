Describe 'New-EVXFilter' {
    It 'returns a reusable EventFilter by default' {
        $Filter = New-EVXFilter `
            -EventId 4625 `
            -ProviderName 'Microsoft-Windows-Security-Auditing' `
            -TimePeriod PastDay

        $Filter.GetType().FullName | Should -Be 'EventViewerX.EventFilter'
        $Filter.EventIds | Should -Be 4625
        $Filter.ProviderNames | Should -Be 'Microsoft-Windows-Security-Auditing'
        $Filter.StartTime | Should -BeGreaterThan ([datetime]::Now.AddDays(-2))
    }

    It 'keeps Get-EVXFilter as a migration alias to the typed builder' {
        (Get-Alias Get-EVXFilter).ResolvedCommandName | Should -Be 'New-EVXFilter'
        (Get-EVXFilter -EventId 7040).GetType().FullName |
            Should -Be 'EventViewerX.EventFilter'
    }

    It 'compiles one native XPath expression for Get-WinEvent interop' {
        $XPath = New-EVXFilter `
            -EventId 1, 2 `
            -NamedDataFilter @{ FieldName = 'Value1' } `
            -AsXPath

        $XPath | Should -Be "(*[System[(EventID=1) or (EventID=2)]]) and (*[EventData[Data[@Name='FieldName'] = 'Value1']])"
    }

    It 'requires QueryList XML for named-data exclusions' {
        {
            New-EVXFilter `
                -NamedDataExcludeFilter @{ FieldName = 'Value1' } `
                -AsXPath
        } | Should -Throw '*QueryList XML*'
    }

    It 'emits channel QueryList XML with native suppression' {
        [xml] $FilterXml = New-EVXFilter `
            -LogName System `
            -EventId 7040 `
            -NamedDataExcludeFilter @{ param4 = 'BITS' }

        $FilterXml.QueryList.Query.Path | Should -Be 'System'
        $FilterXml.QueryList.Query.Select.'#text' | Should -Be '*[System[EventID=7040]]'
        $FilterXml.QueryList.Query.Suppress.'#text' |
            Should -Be "*[EventData[Data[@Name='param4'] = 'BITS']]"
    }

    It 'emits file QueryList XML without a channel dependency' {
        $FilePath = Join-Path $PSScriptRoot 'Logs\NamedFilterExamples.evtx'
        [xml] $FilterXml = New-EVXFilter `
            -Path $FilePath `
            -EventId 7040 `
            -NamedDataExcludeFilter @{ param4 = 'BITS' }

        $FilterXml.QueryList.Query.Path | Should -BeLike 'file://*NamedFilterExamples.evtx'
        $FilterXml.QueryList.Query.Select.Path | Should -BeLike 'file://*NamedFilterExamples.evtx'
    }

    It 'supports keyword masks and exclusions through the same compiler' {
        (New-EVXFilter -Keywords 1125899906842624 -AsXPath) |
            Should -Be '*[System[band(Keywords,1125899906842624)]]'
        (New-EVXFilter -ExcludeEventId 1, 2 -AsXPath) |
            Should -Be '*[System[(EventID!=1) and (EventID!=2)]]'
    }

    It 'reuses the typed filter directly in Get-EVXEvent' {
        $FilePath = Join-Path $PSScriptRoot 'Logs\NamedFilterExamples.evtx'
        $Filter = New-EVXFilter `
            -EventId 7040 `
            -NamedDataFilter @{ param4 = 'BITS' }

        $Events = @(Get-EVXEvent `
                -Path $FilePath `
                -Filter $Filter `
                -Oldest `
                -ReadMode StructuredData)

        $Events | Should -Not -BeNullOrEmpty
        $Events[0].Data['param4'] | Should -Be 'BITS'
    }

    It 'exposes discoverable domain fields for built-in and composite types' {
        $Leaf = New-EVXFilter -Type ADUserLogonFailed
        $Composite = New-EVXFilter -Type ActiveDirectoryAuthentication

        $Leaf.GetType().FullName | Should -Be 'PSEventViewer.PowerShellEventPredicateBuilder'
        $Leaf.Fields.PSObject.Properties.Name | Should -Contain 'Who'
        $Leaf.Fields.PSObject.Properties.Name | Should -Contain 'IpAddress'
        $Composite.Fields.PSObject.Properties.Name | Should -Contain 'Who'
        $Composite.Fields.PSObject.Properties.Name | Should -Contain 'EventId'
    }

    It 'builds and explains one shared typed predicate without querying a source' {
        $Filter = New-EVXFilter -Type ADUserLogonFailed
        $Predicate = $Filter.AllOf(
            $Filter.Fields.EventId.In(4624, 4625),
            $Filter.Fields.Who.Contains('svc-')
        )

        $Plan = Get-EVXEvent -Type ADUserLogonFailed -Where $Predicate -Explain

        $Plan.HasNativeFilter | Should -BeTrue
        $Plan.IsFullyNative | Should -BeFalse
        @($Plan.Steps | ForEach-Object { $_.Stage.ToString() }) | Should -Contain 'Native'
        @($Plan.Steps | ForEach-Object { $_.Stage.ToString() }) | Should -Contain 'Managed'
    }

    It 'accepts a restricted PowerShell expression but never executes arbitrary script' {
        $Plan = Get-EVXEvent -Type ADUserLogonFailed -Where {
            $_.Who -in @('EVOTEC\alice', 'EVOTEC\bob') -and
                $_.IpAddress -like '10.*'
        } -Explain

        $Plan.Steps.Count | Should -BeGreaterOrEqual 2
        {
            Get-EVXEvent -Type ADUserLogonFailed -Where {
                Get-Process
            } -Explain
        } | Should -Throw '*commands or pipelines*'
    }

    It 'preserves negative PowerShell operators as exact predicate negation' {
        $Plan = Get-EVXEvent -Type ADUserLogonFailed -Where {
            $_.Who -notlike 'svc-*' -and $_.Who -notmatch '^test'
        } -Explain

        $Plan.ManagedPredicate.Kind.ToString() | Should -Be 'All'
        @($Plan.ManagedPredicate.Children | ForEach-Object { $_.Kind.ToString() }) |
            Should -Be @('Not', 'Not')
    }

    It 'preserves PowerShell case-sensitive and case-insensitive comparison forms' {
        $Insensitive = Get-EVXEvent -Type ADUserLogonFailed -Where {
            $_.Who -eq 'EVOTEC\ALICE'
        } -Explain
        $Sensitive = Get-EVXEvent -Type ADUserLogonFailed -Where {
            $_.Who -ceq 'EVOTEC\ALICE'
        } -Explain

        $Insensitive.ManagedPredicate.IgnoreCase | Should -BeTrue
        $Sensitive.ManagedPredicate.IgnoreCase | Should -BeFalse
    }
}
