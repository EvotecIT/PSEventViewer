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
        $Filter.AllOf(
            $Filter.Fields.EventId.In(4624, 4625),
            $Filter.Fields.Who.Contains('svc-')
        )

        $Plan = Get-EVXEvent -Filter $Filter -Explain

        $Plan.HasNativeFilter | Should -BeTrue
        $Plan.IsFullyNative | Should -BeFalse
        @($Plan.Steps | ForEach-Object { $_.Stage.ToString() }) | Should -Contain 'Native'
        @($Plan.Steps | ForEach-Object { $_.Stage.ToString() }) | Should -Contain 'Managed'
    }

    It 'retains the agreed discoverable filter for direct reuse through Get-EVXEvent -Filter' {
        $Filter = New-EVXFilter -Type ADUserLogonFailed
        $ConfigurationOutput = @($Filter.AllOf(
            $Filter.Fields.Who.In('EVOTEC\Alice', 'EVOTEC\Bob'),
            $Filter.Fields.IPAddress.MatchesSubnet('10.0.0.0/8')
        ))

        $ConfigurationOutput | Should -BeNullOrEmpty
        $Filter.Predicate.Kind.ToString() | Should -Be 'All'
        $Plan = Get-EVXEvent -Filter $Filter -TimePeriod Last7Days -Explain
        $Plan.ManagedPredicate | Should -Not -BeNullOrEmpty
        (Get-Command Get-EVXEvent).ParameterSets.Name | Should -Contain 'TypedFilter'
    }

    It 'describes typed fields and explains an inline reusable filter without reading events' {
        $Description = Get-EVXEvent -Type ADUserLogonFailed -Describe
        $Plan = New-EVXFilter -Type ADUserLogonFailed -Where {
            $_.Who -like 'EVOTEC\*'
        } -Explain

        $Description.Name | Should -Be 'ADUserLogonFailed'
        $Description.Fields.Name | Should -Contain 'Who'
        $Description.Fields.Name | Should -Contain 'IpAddress'
        $Plan.ManagedPredicate | Should -Not -BeNullOrEmpty
    }

    It 'rejects misspelled typed fields before any event query runs' {
        {
            New-EVXFilter -Type ADUserLogonFailed -Where {
                $_.DefinitelyMissing -eq 'value'
            }
        } | Should -Throw '*Available fields*'
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

    It 'accepts reversed PowerShell membership for collection fields' {
        $Included = Get-EVXEvent -Type ADUserPrivilegeUse -Where {
            'SeDebugPrivilege' -in $_.Privileges
        } -Explain
        $Excluded = Get-EVXEvent -Type ADUserPrivilegeUse -Where {
            'SeDebugPrivilege' -notin $_.Privileges
        } -Explain

        $Included.ManagedPredicate.Operator.ToString() | Should -Be 'In'
        $Excluded.ManagedPredicate.Operator.ToString() | Should -Be 'NotIn'
    }

    It 'explains custom aliases from definition metadata rather than native names' {
        $DefinitionPath = Join-Path $TestDrive 'custom-explain.json'
        @{
            Name = 'CustomExplain'
            Sources = @(@{
                    LogName = 'System'
                    EventIds = @(7040)
                })
            Fields = @(@{
                    Name = 'ServiceLabel'
                    Aliases = @('ProviderName')
                    Source = 'Data'
                    SourceName = 'param4'
                })
        } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $DefinitionPath -Encoding UTF8

        $Filter = New-EVXFilter -Definition $DefinitionPath
        $Filter.Use($Filter.Fields.ProviderName.Equal('BITS')) | Out-Null
        $Plan = Get-EVXEvent -Filter $Filter -Explain

        $Plan.NativeFilter | Should -BeNullOrEmpty
        $Plan.ManagedPredicate.Field | Should -Be 'ServiceLabel'
    }
}
