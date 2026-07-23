$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$hostDll = input BenchmarkHostPath (Join-Path $PSScriptRoot 'EventLogParsing.BenchmarkHost\bin\Release\net10.0-windows\EventLogParsing.BenchmarkHost.dll')
$modulePath = input PSEventViewerPath (Join-Path $repositoryRoot 'Sources\PSEventViewer\bin\Release\net10.0-windows\PSEventViewer.dll')
$baselineHostPath = input BaselineHostPath
$baselineModulePath = input BaselineModulePath
$evtxECmdPath = input EvtxECmdPath
$largeFixturePath = input LargeFixturePath
$expectedLargeCount = inputInt ExpectedLargeCount 0
$expensiveSampleCount = inputInt ExpensiveSampleCount 100000
$smokeFixturePath = Join-Path $repositoryRoot 'Tests\Logs\NamedFilterExamples.evtx'
$powerShellRunner = Join-Path $PSScriptRoot 'Invoke-PowerShellEventLogBenchmark.ps1'
$evtxRunner = Join-Path $PSScriptRoot 'Invoke-EvtxECmdBenchmark.ps1'
$benchmarkWrapper = Join-Path $PSScriptRoot 'Invoke-EventLogParsingBenchmark.ps1'
$benchmarkSpec = Join-Path $PSScriptRoot 'event-log-parsing.benchmark.ps1'
$pwshPath = [string] (Get-Command pwsh -ErrorAction Stop).Source
$dotnetPath = [string] (Get-Command dotnet -ErrorAction Stop).Source

[array] $fixtures = & {
    [pscustomobject] @{
        Name          = 'Smoke'
        Path          = $smokeFixturePath
        ExpectedCount = 184
    }
    if ($largeFixturePath) {
        [pscustomobject] @{
            Name          = 'Large'
            Path          = [IO.Path]::GetFullPath($largeFixturePath)
            ExpectedCount = $expectedLargeCount
        }
    }
}

[array] $cases = foreach ($fixture in $fixtures) {
    foreach ($mode in 'Metadata', 'Message', 'StructuredData', 'Full') {
        [pscustomobject] @{
            Name          = "$($fixture.Name)-First-$mode"
            Fixture       = $fixture.Name
            FixturePath   = $fixture.Path
            ExpectedCount = 1
            MaxEvents     = 1
            Workload      = $mode
        }
        [pscustomobject] @{
            Name          = "$($fixture.Name)-Scan-$mode"
            Fixture       = $fixture.Name
            FixturePath   = $fixture.Path
            ExpectedCount = $fixture.ExpectedCount
            MaxEvents     = 0
            Workload      = $mode
        }
    }

    if ($fixture.Name -eq 'Large' -and $expensiveSampleCount -gt 0) {
        $sampleCount = if ($fixture.ExpectedCount -gt 0) {
            [Math]::Min([long] $fixture.ExpectedCount, [long] $expensiveSampleCount)
        } else {
            $expensiveSampleCount
        }
        foreach ($mode in 'Metadata', 'Message', 'StructuredData', 'Full') {
            [pscustomobject] @{
                Name          = "$($fixture.Name)-Sample-$mode"
                Fixture       = $fixture.Name
                FixturePath   = $fixture.Path
                ExpectedCount = $sampleCount
                MaxEvents     = $sampleCount
                Workload      = $mode
            }
        }
    }

    [pscustomobject] @{
        Name          = "$($fixture.Name)-Export-MetadataCsv"
        Fixture       = $fixture.Name
        FixturePath   = $fixture.Path
        ExpectedCount = $fixture.ExpectedCount
        MaxEvents     = 0
        Workload      = 'MetadataCsv'
    }
    [pscustomobject] @{
        Name          = "$($fixture.Name)-Scan-NativeParse"
        Fixture       = $fixture.Name
        FixturePath   = $fixture.Path
        ExpectedCount = $fixture.ExpectedCount
        MaxEvents     = 0
        Workload      = 'NativeParse'
    }
    [pscustomobject] @{
        Name          = "$($fixture.Name)-Export-EvtxCsv"
        Fixture       = $fixture.Name
        FixturePath   = $fixture.Path
        ExpectedCount = $fixture.ExpectedCount
        MaxEvents     = 0
        Workload      = 'EvtxCsv'
    }
}

benchmark 'event-log-parsing' -out (Join-Path $repositoryRoot 'Ignore\Benchmarks\EventLogParsing\Runs') {
    metadata RepositoryHead ([string] (git -C $repositoryRoot rev-parse HEAD))
    [array] $repositoryStatus = @(git -C $repositoryRoot status --porcelain=v1 --untracked-files=normal)
    $repositoryStatusText = if ($repositoryStatus.Count -gt 0) {
        [string] ($repositoryStatus -join "`n")
    } else {
        '<clean>'
    }
    metadata RepositoryDirty ([string] ($repositoryStatus.Count -gt 0))
    metadata RepositoryStatus $repositoryStatusText
    metadata DotNetVersion ([string] (& $dotnetPath --version))
    metadata PowerShellVersion $PSVersionTable.PSVersion.ToString()
    metadata BenchmarkHostSha256 (Get-FileHash -LiteralPath $hostDll -Algorithm SHA256).Hash
    metadata PSEventViewerSha256 (Get-FileHash -LiteralPath $modulePath -Algorithm SHA256).Hash
    metadata BenchmarkHostEventViewerXSha256 (Get-FileHash -LiteralPath (Join-Path (Split-Path -Parent $hostDll) 'EventViewerX.dll') -Algorithm SHA256).Hash
    metadata PSEventViewerEventViewerXSha256 (Get-FileHash -LiteralPath (Join-Path (Split-Path -Parent $modulePath) 'EventViewerX.dll') -Algorithm SHA256).Hash
    metadata BenchmarkScriptManifest ([string] (@(
        foreach ($scriptPath in $benchmarkWrapper, $benchmarkSpec, $powerShellRunner, $evtxRunner) {
            [ordered] @{
                Path   = $scriptPath
                Sha256 = (Get-FileHash -LiteralPath $scriptPath -Algorithm SHA256).Hash
            }
        }
    ) | ConvertTo-Json -Compress))
    metadata FixtureManifest ([string] (@(
        foreach ($fixture in $fixtures) {
            $fixtureFile = Get-Item -LiteralPath $fixture.Path
            [ordered] @{
                Name   = $fixture.Name
                Path   = $fixtureFile.FullName
                Bytes  = $fixtureFile.Length
                Sha256 = (Get-FileHash -LiteralPath $fixtureFile.FullName -Algorithm SHA256).Hash
            }
        }
    ) | ConvertTo-Json -Compress))
    if ($baselineHostPath) {
        metadata BaselineHostSha256 (Get-FileHash -LiteralPath $baselineHostPath -Algorithm SHA256).Hash
        $baselineHostEventViewerXPath = Join-Path (Split-Path -Parent $baselineHostPath) 'EventViewerX.dll'
        if (Test-Path -LiteralPath $baselineHostEventViewerXPath -PathType Leaf) {
            metadata BaselineHostEventViewerXSha256 (Get-FileHash -LiteralPath $baselineHostEventViewerXPath -Algorithm SHA256).Hash
        }
    }
    if ($baselineModulePath) {
        metadata BaselineModuleSha256 (Get-FileHash -LiteralPath $baselineModulePath -Algorithm SHA256).Hash
        $baselineModuleEventViewerXPath = Join-Path (Split-Path -Parent $baselineModulePath) 'EventViewerX.dll'
        if (Test-Path -LiteralPath $baselineModuleEventViewerXPath -PathType Leaf) {
            metadata BaselineModuleEventViewerXSha256 (Get-FileHash -LiteralPath $baselineModuleEventViewerXPath -Algorithm SHA256).Hash
        }
    }
    if ($evtxECmdPath) {
        metadata EvtxECmdVersion ([Diagnostics.FileVersionInfo]::GetVersionInfo([IO.Path]::GetFullPath($evtxECmdPath)).ProductVersion)
        metadata EvtxECmdSha256 (Get-FileHash -LiteralPath $evtxECmdPath -Algorithm SHA256).Hash
    }

    policy -Warmup 0 -Iterations 1 -Order Rotated -OutlierMode None
    profile Current -Cleanup KeepOnFailure
    caseSource $cases

    setup {
        param($case, $run)

        $run.ResultPath = Join-Path $run.OutputDirectory 'result.json'
        $run.StandardOutputPath = Join-Path $run.OutputDirectory 'stdout.txt'
        $run.EventOutputPath = Join-Path $run.OutputDirectory 'events.csv'
        Remove-Item -LiteralPath $run.ResultPath, $run.StandardOutputPath, $run.EventOutputPath -Force -ErrorAction SilentlyContinue
    }

    skip {
        param($case)

        if ($case.Workload -eq 'NativeParse' -or $case.Workload -eq 'EvtxCsv') {
            return $case.Engine -ne 'EvtxECmd' -or -not $evtxECmdPath
        }
        if ($case.Workload -eq 'MetadataCsv') {
            if ($case.Engine -eq 'PSEventViewerBaseline') {
                return -not $baselineModulePath
            }
            return $case.Engine -ne 'DotNet' -and $case.Engine -ne 'PSEventViewer' -and $case.Engine -ne 'GetWinEvent'
        }
        if ($case.Engine -eq 'PropertySelector' -and $case.Workload -ne 'Metadata') {
            return $true
        }
        if ($case.Engine -eq 'EvtxECmd') {
            return $true
        }
        if ($case.Engine -eq 'EventViewerXBaseline' -and -not $baselineHostPath) {
            return $true
        }
        if ($case.Engine -eq 'PSEventViewerBaseline' -and -not $baselineModulePath) {
            return $true
        }
        return $false
    }

    engine DotNet {
        operation Scan {
            param($case, $run)

            $readMode = if ($case.Workload -eq 'MetadataCsv') { 'Metadata' } else { $case.Workload }
            [array] $arguments = @(
                $hostDll
                '--engine'
                'dotnet'
                '--path'
                $case.FixturePath
                '--mode'
                $readMode
                '--result'
                $run.ResultPath
                '--max-events'
                $case.MaxEvents
                if ($case.Workload -eq 'MetadataCsv') {
                    '--output-path'
                    $run.EventOutputPath
                }
            )
            & $dotnetPath @arguments *> $run.StandardOutputPath
            if ($LASTEXITCODE -ne 0) {
                throw "Raw .NET benchmark host exited with code $LASTEXITCODE."
            }
        }
    }

    engine PropertySelector {
        operation Scan {
            param($case, $run)

            & $dotnetPath $hostDll `
                --engine propertyselector `
                --path $case.FixturePath `
                --mode Metadata `
                --result $run.ResultPath `
                --max-events $case.MaxEvents *> $run.StandardOutputPath
            if ($LASTEXITCODE -ne 0) {
                throw "EventLogPropertySelector benchmark host exited with code $LASTEXITCODE."
            }
        }
    }

    engine EventViewerX {
        operation Scan {
            param($case, $run)

            & $dotnetPath $hostDll `
                --engine eventviewerx `
                --path $case.FixturePath `
                --mode $case.Workload `
                --result $run.ResultPath `
                --max-events $case.MaxEvents *> $run.StandardOutputPath
            if ($LASTEXITCODE -ne 0) {
                throw "EventViewerX benchmark host exited with code $LASTEXITCODE."
            }
        }
    }

    engine EventViewerXBaseline {
        operation Scan {
            param($case, $run)

            & $dotnetPath $baselineHostPath `
                --engine eventviewerx `
                --path $case.FixturePath `
                --mode $case.Workload `
                --result $run.ResultPath `
                --max-events $case.MaxEvents *> $run.StandardOutputPath
            if ($LASTEXITCODE -ne 0) {
                throw "Baseline EventViewerX benchmark host exited with code $LASTEXITCODE."
            }
        }
    }

    engine PSEventViewer {
        operation Scan {
            param($case, $run)

            $readMode = if ($case.Workload -eq 'MetadataCsv') { 'Metadata' } else { $case.Workload }
            [array] $arguments = @(
                '-NoLogo'
                '-NoProfile'
                '-NonInteractive'
                '-File'
                $powerShellRunner
                '-Engine'
                'PSEventViewer'
                '-Path'
                $case.FixturePath
                '-ReadMode'
                $readMode
                '-ResultPath'
                $run.ResultPath
                '-ModulePath'
                $modulePath
                '-MaxEvents'
                $case.MaxEvents
                if ($case.Workload -eq 'MetadataCsv') {
                    '-CsvOutputPath'
                    $run.EventOutputPath
                }
            )
            & $pwshPath @arguments *> $run.StandardOutputPath
            if ($LASTEXITCODE -ne 0) {
                throw "PSEventViewer benchmark process exited with code $LASTEXITCODE."
            }
        }
    }

    engine PSEventViewerBaseline {
        operation Scan {
            param($case, $run)

            $readMode = if ($case.Workload -eq 'MetadataCsv') { 'Metadata' } else { $case.Workload }
            [array] $arguments = @(
                '-NoLogo'
                '-NoProfile'
                '-NonInteractive'
                '-File'
                $powerShellRunner
                '-Engine'
                'PSEventViewer'
                '-Path'
                $case.FixturePath
                '-ReadMode'
                $readMode
                '-ResultPath'
                $run.ResultPath
                '-ModulePath'
                $baselineModulePath
                '-MaxEvents'
                $case.MaxEvents
                if ($case.Workload -eq 'MetadataCsv') {
                    '-CsvOutputPath'
                    $run.EventOutputPath
                }
            )
            & $pwshPath @arguments *> $run.StandardOutputPath
            if ($LASTEXITCODE -ne 0) {
                throw "Baseline PSEventViewer benchmark process exited with code $LASTEXITCODE."
            }
        }
    }

    engine GetWinEvent {
        operation Scan {
            param($case, $run)

            $readMode = if ($case.Workload -eq 'MetadataCsv') { 'Metadata' } else { $case.Workload }
            [array] $arguments = @(
                '-NoLogo'
                '-NoProfile'
                '-NonInteractive'
                '-File'
                $powerShellRunner
                '-Engine'
                'GetWinEvent'
                '-Path'
                $case.FixturePath
                '-ReadMode'
                $readMode
                '-ResultPath'
                $run.ResultPath
                '-MaxEvents'
                $case.MaxEvents
                if ($case.Workload -eq 'MetadataCsv') {
                    '-CsvOutputPath'
                    $run.EventOutputPath
                }
            )
            & $pwshPath @arguments *> $run.StandardOutputPath
            if ($LASTEXITCODE -ne 0) {
                throw "Get-WinEvent benchmark process exited with code $LASTEXITCODE."
            }
        }
    }

    engine EvtxECmd {
        operation Scan {
            param($case, $run)

            [array] $arguments = @(
                '-NoLogo'
                '-NoProfile'
                '-NonInteractive'
                '-File'
                $evtxRunner
                '-ExecutablePath'
                $evtxECmdPath
                '-Path'
                $case.FixturePath
                '-ResultPath'
                $run.ResultPath
                '-StandardOutputPath'
                $run.StandardOutputPath
                if ($case.Workload -eq 'EvtxCsv') {
                    '-CsvOutputPath'
                    $run.EventOutputPath
                }
            )
            & $pwshPath @arguments
            if ($LASTEXITCODE -ne 0) {
                throw "EvtxECmd benchmark process exited with code $LASTEXITCODE."
            }
        }
    }

    validate {
        param($case, $run)

        assertPath $run.ResultPath
        $run.Result = Get-Content -LiteralPath $run.ResultPath -Raw | ConvertFrom-Json
        if ($case.ExpectedCount -gt 0) {
            assertValue -Actual ([long] $run.Result.Count) -Expected ([long] $case.ExpectedCount) -Message 'Every engine must process the expected event count.'
        } elseif ([long] $run.Result.Count -le 0) {
            throw 'The benchmark engine did not process any events.'
        }
        if ($run.Result.PSObject.Properties.Name -contains 'Errors') {
            assertValue -Actual ([long] $run.Result.Errors) -Expected ([long] 0) -Message 'The parser must not report EVTX errors.'
        }
        if (($case.Workload -eq 'MetadataCsv' -or $case.Workload -eq 'EvtxCsv') -and
            ([long] $run.Result.OutputBytes -le 0)) {
            throw 'The CSV benchmark did not produce a non-empty output file.'
        }
        if (($case.Workload -eq 'MetadataCsv' -or $case.Workload -eq 'EvtxCsv') -and
            [string]::IsNullOrWhiteSpace([string] $run.Result.OutputSha256)) {
            throw 'The CSV benchmark did not record its SHA-256 hash.'
        }
    }

    metric Events {
        param($case, $run)
        [long] $run.Result.Count
    }

    metric EventsPerSecond {
        param($case, $run)
        if ([double] $run.Result.ElapsedMilliseconds -le 0) {
            0
        } else {
            [math]::Round(([double] $run.Result.Count * 1000) / [double] $run.Result.ElapsedMilliseconds, 3)
        }
    }

    metric InternalMs {
        param($case, $run)
        [double] $run.Result.ElapsedMilliseconds
    }

    metric EngineAllocatedBytes {
        param($case, $run)
        [long] $run.Result.AllocatedBytes
    }

    metric EnginePeakWorkingSetBytes {
        param($case, $run)
        [long] $run.Result.PeakWorkingSetBytes
    }

    metric MessageCharacters {
        param($case, $run)
        if ($run.Result.PSObject.Properties.Name -contains 'MessageCharacters') {
            [long] $run.Result.MessageCharacters
        } else {
            0
        }
    }

    metric XmlCharacters {
        param($case, $run)
        if ($run.Result.PSObject.Properties.Name -contains 'XmlCharacters') {
            [long] $run.Result.XmlCharacters
        } else {
            0
        }
    }

    metric PropertyCount {
        param($case, $run)
        if ($run.Result.PSObject.Properties.Name -contains 'PropertyCount') {
            [long] $run.Result.PropertyCount
        } else {
            0
        }
    }

    metric OutputBytes {
        param($case, $run)
        if ($run.Result.PSObject.Properties.Name -contains 'OutputBytes') {
            [long] $run.Result.OutputBytes
        } else {
            0
        }
    }

    comparison Engine -Baseline DotNet -Metric MedianMs -TieTolerance 0.03
    artifacts Json, Csv, Markdown
}
