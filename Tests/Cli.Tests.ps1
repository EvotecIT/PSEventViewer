Describe 'evx portable host' {
    BeforeAll {
        $script:CliPath = Join-Path $PSScriptRoot '..\Sources\EventViewerX.Cli\bin\Debug\net10.0-windows\evx.exe'
        $script:FixturePath = Join-Path $PSScriptRoot 'Logs\NamedFilterExamples.evtx'
        $script:SmtpProfilePath = Join-Path $PSScriptRoot 'Fixtures\SmtpProfile.DryRun.json'
    }

    It 'ships the complete built-in type catalog' {
        $Definitions = @(& $script:CliPath types | ForEach-Object { $_ | ConvertFrom-Json })

        $LASTEXITCODE | Should -Be 0
        $Definitions.Count | Should -Be 90
        @($Definitions | Where-Object { -not $_.IsComposite }).Count | Should -Be 80
        @($Definitions | Where-Object IsComposite).Count | Should -Be 10
    }

    It 'queries offline files without PowerShell module startup' {
        $Rows = @(& $script:CliPath query --path $script:FixturePath --max 3 |
                ForEach-Object { $_ | ConvertFrom-Json })

        $LASTEXITCODE | Should -Be 0
        $Rows.Count | Should -Be 3
        $Rows[0].Type | Should -Be 'Generic'
        $Rows[0].SourceLog | Should -Be 'System'
        $Rows[0].Message | Should -Not -BeNullOrEmpty
    }

    It 'renders HTML and Excel from one query and composes a Mailozaurr delivery' {
        $HtmlPath = Join-Path $TestDrive 'events.html'
        $ExcelPath = Join-Path $TestDrive 'events.xlsx'
        $EmailPath = Join-Path $TestDrive 'events-email.html'
        $Output = @(& $script:CliPath report `
                --path $script:FixturePath `
                --max 3 `
                --html $HtmlPath `
                --drawer-placement Top `
                --excel $ExcelPath `
                --email-html $EmailPath `
                --mail-profile $script:SmtpProfilePath)

        $LASTEXITCODE | Should -Be 0
        Test-Path -LiteralPath $HtmlPath | Should -BeTrue
        (Get-Content -LiteralPath $HtmlPath -Raw) | Should -Match 'data-hfx-monitoring-record-drawer-placement="top"'
        Test-Path -LiteralPath $ExcelPath | Should -BeTrue
        Test-Path -LiteralPath $EmailPath | Should -BeTrue
        $Delivery = $Output[-1] | ConvertFrom-Json
        $Delivery.DryRun | Should -BeTrue
        $Delivery.Delivered | Should -BeFalse
    }

    It 'rejects an unknown HTML drawer placement' {
        $PreviousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $Output = & $script:CliPath report --path $script:FixturePath --html (Join-Path $TestDrive 'invalid.html') --drawer-placement Bottom 2>&1
        } finally {
            $ErrorActionPreference = $PreviousErrorActionPreference
        }

        $LASTEXITCODE | Should -Be 1
        [string] $Output | Should -Match 'Auto, Top, or Right'
    }

    It 'rejects ambiguous query sources' {
        $PreviousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $Output = & $script:CliPath query --path $script:FixturePath --log System 2>&1
        } finally {
            $ErrorActionPreference = $PreviousErrorActionPreference
        }

        $LASTEXITCODE | Should -Be 1
        [string] $Output | Should -Match 'standalone --path'
    }

    It 'rejects unknown options instead of silently running an unbounded query' {
        $PreviousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $Output = & $script:CliPath query --path $script:FixturePath --max-events 1 2>&1
        } finally {
            $ErrorActionPreference = $PreviousErrorActionPreference
        }

        $LASTEXITCODE | Should -Be 1
        [string] $Output | Should -Match "Unknown option\(s\): --max-events"
    }

    It 'rejects positional arguments instead of silently treating them as subcommands' {
        $PreviousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $Output = & $script:CliPath query ignored --path $script:FixturePath --max 1 2>&1
        } finally {
            $ErrorActionPreference = $PreviousErrorActionPreference
        }

        $LASTEXITCODE | Should -Be 1
        [string] $Output | Should -Match "Unexpected argument 'ignored'"
    }

    It 'applies a custom definition to an offline file' {
        $DefinitionPath = Join-Path $TestDrive 'service-change.json'
        @{
            Name = 'ServiceStartTypeChange'
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

        $Rows = @(& $script:CliPath query --definition $DefinitionPath --path $script:FixturePath --max 2 |
                ForEach-Object { $_ | ConvertFrom-Json })

        $LASTEXITCODE | Should -Be 0
        $Rows.Count | Should -Be 2
        @($Rows.Type | Sort-Object -Unique) | Should -Be @('ServiceStartTypeChange')
    }

    It 'removes an already absent collector subscription idempotently' {
        $Name = 'EVX-Cli-Absent-' + [guid]::NewGuid().ToString('N')
        $Result = & $script:CliPath collector remove --name $Name |
            ConvertFrom-Json

        $LASTEXITCODE | Should -Be 0
        $Result.SubscriptionName | Should -Be $Name
        $Result.Success | Should -BeTrue
        $Result.Changed | Should -BeFalse
        $Result.Before | Should -BeNullOrEmpty
        $Result.After | Should -BeNullOrEmpty
    }
}
