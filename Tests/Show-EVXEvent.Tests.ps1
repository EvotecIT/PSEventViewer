Describe 'Show-EVXEvent' {
    It 'keeps Type, generic LogName, Definition, and pipeline input mutually exclusive' {
        $Command = Get-Command Show-EVXEvent
        $Command.DefaultParameterSet | Should -Be 'Input'
        $Command.ParameterSets.Name | Sort-Object |
            Should -Be (@('Type', 'Log', 'Path', 'Definition', 'Input') | Sort-Object)
        ($Command.ParameterSets | Where-Object Name -EQ 'Type').Parameters.Name |
            Should -Not -Contain 'LogName'
        ($Command.ParameterSets | Where-Object Name -EQ 'Log').Parameters.Name |
            Should -Contain 'LogName'
        ($Command.ParameterSets | Where-Object Name -EQ 'Path').Parameters.Name |
            Should -Not -Contain 'MachineName'
        ($Command.ParameterSets | Where-Object Name -EQ 'Type').Parameters.Name |
            Should -Contain 'Path'
        ($Command.ParameterSets | Where-Object Name -EQ 'Definition').Parameters.Name |
            Should -Contain 'Path'
        ($Command.ParameterSets | Where-Object Name -EQ 'Definition').Parameters.Name |
            Should -Contain 'MaxEventsScanned'
    }

    It 'uses Path alone for a generic offline report' {
        $FixturePath = Join-Path $PSScriptRoot 'Logs\NamedFilterExamples.evtx'

        $Report = Show-EVXEvent -Path $FixturePath -MaxEvents 2 -PassThru

        $Report.Rows.Count | Should -Be 2
        $Report.Sections.Count | Should -Be 1
        $Report.Sections[0].Kind.ToString() | Should -Be 'Generic'
        $Report.Sections[0].Columns.Name | Should -Contain 'EventId'
    }

    It 'renders HTML, Excel, email, and the report from one supplied snapshot' {
        $Event = Get-EVXEvent -LogName System -MaxEvents 1 -ReadMode StructuredDataAndMessage |
            Select-Object -First 1
        if (-not $Event) {
            Set-ItResult -Skipped -Because 'The System event log contained no readable events.'
            return
        }
        $HtmlPath = Join-Path $TestDrive 'event-report.html'
        $ExcelPath = Join-Path $TestDrive 'event-report.xlsx'

        $Result = @($Event | Show-EVXEvent `
                -Title 'System snapshot' `
                -HtmlPath $HtmlPath `
                -ExcelPath $ExcelPath `
                -EmailPackage `
                -PassThru)

        Test-Path -LiteralPath $HtmlPath | Should -BeTrue
        Test-Path -LiteralPath $ExcelPath | Should -BeTrue
        (Get-Content -LiteralPath $HtmlPath -Raw) | Should -Match 'System snapshot'
        $Result.Count | Should -Be 4
        @($Result | Where-Object { $_ -is [EventViewerX.Reporting.EventEmailPackage] }).Count |
            Should -Be 1
        $Report = $Result | Where-Object { $_ -is [EventViewerX.Reporting.EventReport] }
        $Report.Rows.Count | Should -Be 1
    }

    It 'combines a custom definition with an offline path without a second query mode' {
        $DefinitionPath = Join-Path $TestDrive 'service-change.json'
        @{
            Name = 'ServiceStartTypeChange'
            DisplayName = 'Service start type changes'
            Sources = @(@{
                    LogName = 'System'
                    EventIds = @(7040)
                    ProviderNames = @('Service Control Manager')
                })
            Fields = @(@{
                    Name = 'ServiceName'
                    Source = 'Data'
                    SourceName = 'param1'
                })
        } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $DefinitionPath -Encoding UTF8
        $FixturePath = Join-Path $PSScriptRoot 'Logs\NamedFilterExamples.evtx'

        $Report = Show-EVXEvent -Definition $DefinitionPath -Path $FixturePath -MaxEvents 2 -PassThru

        $Report.Rows.Count | Should -Be 2
        @($Report.Rows.Type | Sort-Object -Unique) | Should -Be @('ServiceStartTypeChange')
        $Report.Coverage[0].MachineName | Should -Be 'Offline'
    }
}
