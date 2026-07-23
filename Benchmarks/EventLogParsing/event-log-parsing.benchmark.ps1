$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$hostDll = input BenchmarkHostPath (Join-Path $PSScriptRoot 'EventLogParsing.BenchmarkHost\bin\Release\net10.0-windows\EventLogParsing.BenchmarkHost.dll')
$modulePath = input PSEventViewerPath (Join-Path $repositoryRoot 'Sources\PSEventViewer\bin\Release\net10.0-windows\PSEventViewer.dll')
$baselineHostPath = input BaselineHostPath
$baselineModulePath = input BaselineModulePath
$evtxECmdPath = input EvtxECmdPath
$largeFixturePath = input LargeFixturePath
$expectedLargeCount = inputInt ExpectedLargeCount 0
$expensiveSampleCount = inputInt ExpensiveSampleCount 100000
$readmeTable = input ReadmeTable None
$smokeFixturePath = Join-Path $repositoryRoot 'Tests\Logs\NamedFilterExamples.evtx'
$powerShellRunner = Join-Path $PSScriptRoot 'Invoke-PowerShellEventLogBenchmark.ps1'
$evtxRunner = Join-Path $PSScriptRoot 'Invoke-EvtxECmdBenchmark.ps1'
$benchmarkWrapper = Join-Path $PSScriptRoot 'Invoke-EventLogParsingBenchmark.ps1'
$benchmarkSpec = Join-Path $PSScriptRoot 'event-log-parsing.benchmark.ps1'
$mainReadmePath = Join-Path $repositoryRoot 'README.md'
$pwshPath = [string] (Get-Command pwsh -ErrorAction Stop).Source
$dotnetPath = [string] (Get-Command dotnet -ErrorAction Stop).Source
if ($readmeTable -notin 'None', 'Common', 'EvtxNative') {
    throw "ReadmeTable must be None, Common, or EvtxNative. Received '$readmeTable'."
}

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

[array] $definitions = foreach ($fixture in $fixtures) {
    foreach ($mode in 'Metadata', 'Message', 'StructuredData', 'Full') {
        [pscustomobject] @{
            Name          = "$($fixture.Name)-Common-First-$mode"
            Fixture       = $fixture.Name
            FixturePath   = $fixture.Path
            ExpectedCount = 1
            MaxEvents     = 1
            Workload      = $mode
        }
        [pscustomobject] @{
            Name          = "$($fixture.Name)-Common-Scan-$mode"
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
                Name          = "$($fixture.Name)-Common-Sample-$mode"
                Fixture       = $fixture.Name
                FixturePath   = $fixture.Path
                ExpectedCount = $sampleCount
                MaxEvents     = $sampleCount
                Workload      = $mode
            }
        }
    }

    [pscustomobject] @{
        Name          = "$($fixture.Name)-Exact-Export-MetadataCsv"
        Fixture       = $fixture.Name
        FixturePath   = $fixture.Path
        ExpectedCount = $fixture.ExpectedCount
        MaxEvents     = 0
        Workload      = 'MetadataCsv'
    }
    [pscustomobject] @{
        Name          = "$($fixture.Name)-Evtx-NativeParse"
        Fixture       = $fixture.Name
        FixturePath   = $fixture.Path
        ExpectedCount = $fixture.ExpectedCount
        MaxEvents     = 0
        Workload      = 'EvtxNativeParse'
        OutputExtension = $null
    }
    [pscustomobject] @{
        Name          = "$($fixture.Name)-Evtx-ForensicCsv"
        Fixture       = $fixture.Name
        FixturePath   = $fixture.Path
        ExpectedCount = $fixture.ExpectedCount
        MaxEvents     = 0
        Workload      = 'EvtxForensicCsv'
        OutputExtension = 'csv'
    }
    [pscustomobject] @{
        Name            = "$($fixture.Name)-Evtx-FullJson"
        Fixture         = $fixture.Name
        FixturePath     = $fixture.Path
        ExpectedCount   = $fixture.ExpectedCount
        MaxEvents       = 0
        Workload        = 'EvtxFullJson'
        OutputExtension = 'json'
    }
    [pscustomobject] @{
        Name            = "$($fixture.Name)-Evtx-Xml"
        Fixture         = $fixture.Name
        FixturePath     = $fixture.Path
        ExpectedCount   = $fixture.ExpectedCount
        MaxEvents       = 0
        Workload        = 'EvtxXml'
        OutputExtension = 'xml'
    }
}

$caseDefinitions = @{}
[array] $cases = foreach ($definition in $definitions) {
    $caseDefinitions[$definition.Name] = $definition
    [pscustomobject] @{ Name = $definition.Name }
}
$commonIdentitySignatures = @{}
$exactOutputHashes = @{}

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
    metadata ComparisonContract 'Exact output, common public work, and EvtxECmd-native workflows are reported separately.'
    metadata ReadmeTable $readmeTable
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

        $run.Definition = $caseDefinitions[$case.Scenario]
        $run.ResultPath = Join-Path $run.OutputDirectory 'result.json'
        $run.StandardOutputPath = Join-Path $run.OutputDirectory 'stdout.txt'
        $outputExtension = if ($run.Definition.Workload -eq 'MetadataCsv') {
            'csv'
        } else {
            $run.Definition.OutputExtension
        }
        $run.EventOutputPath = if ($outputExtension) {
            Join-Path $run.OutputDirectory "events.$outputExtension"
        } else {
            $null
        }
        [array] $cleanupPaths = foreach ($cleanupPath in $run.ResultPath, $run.StandardOutputPath, $run.EventOutputPath) {
            if (-not [string]::IsNullOrWhiteSpace([string] $cleanupPath)) {
                $cleanupPath
            }
        }
        Remove-Item -LiteralPath $cleanupPaths -Force -ErrorAction SilentlyContinue
    }

    skip {
        param($case)

        $definition = $caseDefinitions[$case.Scenario]
        if ($definition.Workload -like 'Evtx*') {
            return $case.Engine -ne 'EvtxECmd' -or -not $evtxECmdPath
        }
        if ($definition.Workload -eq 'MetadataCsv') {
            if ($case.Engine -eq 'PSEventViewerBaseline') {
                return -not $baselineModulePath
            }
            return $case.Engine -ne 'DotNet' -and $case.Engine -ne 'PSEventViewer' -and $case.Engine -ne 'GetWinEvent'
        }
        if ($case.Engine -eq 'PropertySelector' -and $definition.Workload -ne 'Metadata') {
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

            $definition = $run.Definition
            $readMode = if ($definition.Workload -eq 'MetadataCsv') { 'Metadata' } else { $definition.Workload }
            [array] $arguments = @(
                $hostDll
                '--engine'
                'dotnet'
                '--path'
                $definition.FixturePath
                '--mode'
                $readMode
                '--result'
                $run.ResultPath
                '--max-events'
                $definition.MaxEvents
                if ($definition.Workload -eq 'MetadataCsv') {
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

            $definition = $run.Definition
            & $dotnetPath $hostDll `
                --engine propertyselector `
                --path $definition.FixturePath `
                --mode Metadata `
                --result $run.ResultPath `
                --max-events $definition.MaxEvents *> $run.StandardOutputPath
            if ($LASTEXITCODE -ne 0) {
                throw "EventLogPropertySelector benchmark host exited with code $LASTEXITCODE."
            }
        }
    }

    engine EventViewerX {
        operation Scan {
            param($case, $run)

            $definition = $run.Definition
            & $dotnetPath $hostDll `
                --engine eventviewerx `
                --path $definition.FixturePath `
                --mode $definition.Workload `
                --result $run.ResultPath `
                --max-events $definition.MaxEvents *> $run.StandardOutputPath
            if ($LASTEXITCODE -ne 0) {
                throw "EventViewerX benchmark host exited with code $LASTEXITCODE."
            }
        }
    }

    engine EventViewerXBaseline {
        operation Scan {
            param($case, $run)

            $definition = $run.Definition
            & $dotnetPath $baselineHostPath `
                --engine eventviewerx `
                --path $definition.FixturePath `
                --mode $definition.Workload `
                --result $run.ResultPath `
                --max-events $definition.MaxEvents *> $run.StandardOutputPath
            if ($LASTEXITCODE -ne 0) {
                throw "Baseline EventViewerX benchmark host exited with code $LASTEXITCODE."
            }
        }
    }

    engine PSEventViewer {
        operation Scan {
            param($case, $run)

            $definition = $run.Definition
            $readMode = if ($definition.Workload -eq 'MetadataCsv') { 'Metadata' } else { $definition.Workload }
            [array] $arguments = @(
                '-NoLogo'
                '-NoProfile'
                '-NonInteractive'
                '-File'
                $powerShellRunner
                '-Engine'
                'PSEventViewer'
                '-Path'
                $definition.FixturePath
                '-ReadMode'
                $readMode
                '-ResultPath'
                $run.ResultPath
                '-ModulePath'
                $modulePath
                '-MaxEvents'
                $definition.MaxEvents
                if ($definition.Workload -eq 'MetadataCsv') {
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

            $definition = $run.Definition
            $readMode = if ($definition.Workload -eq 'MetadataCsv') { 'Metadata' } else { $definition.Workload }
            [array] $arguments = @(
                '-NoLogo'
                '-NoProfile'
                '-NonInteractive'
                '-File'
                $powerShellRunner
                '-Engine'
                'PSEventViewer'
                '-Path'
                $definition.FixturePath
                '-ReadMode'
                $readMode
                '-ResultPath'
                $run.ResultPath
                '-ModulePath'
                $baselineModulePath
                '-MaxEvents'
                $definition.MaxEvents
                if ($definition.Workload -eq 'MetadataCsv') {
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

            $definition = $run.Definition
            $readMode = if ($definition.Workload -eq 'MetadataCsv') { 'Metadata' } else { $definition.Workload }
            [array] $arguments = @(
                '-NoLogo'
                '-NoProfile'
                '-NonInteractive'
                '-File'
                $powerShellRunner
                '-Engine'
                'GetWinEvent'
                '-Path'
                $definition.FixturePath
                '-ReadMode'
                $readMode
                '-ResultPath'
                $run.ResultPath
                '-MaxEvents'
                $definition.MaxEvents
                if ($definition.Workload -eq 'MetadataCsv') {
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

            $definition = $run.Definition
            $workload = switch ($definition.Workload) {
                'EvtxForensicCsv' { 'ForensicCsv' }
                'EvtxFullJson' { 'FullJson' }
                'EvtxXml' { 'Xml' }
                default { 'NativeParse' }
            }
            [array] $arguments = @(
                '-NoLogo'
                '-NoProfile'
                '-NonInteractive'
                '-File'
                $evtxRunner
                '-ExecutablePath'
                $evtxECmdPath
                '-Path'
                $definition.FixturePath
                '-ResultPath'
                $run.ResultPath
                '-StandardOutputPath'
                $run.StandardOutputPath
                '-Workload'
                $workload
                if ($run.EventOutputPath) {
                    '-OutputPath'
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

        $definition = $run.Definition
        assertPath $run.ResultPath
        $run.Result = Get-Content -LiteralPath $run.ResultPath -Raw | ConvertFrom-Json
        if ($definition.ExpectedCount -gt 0) {
            assertValue -Actual ([long] $run.Result.Count) -Expected ([long] $definition.ExpectedCount) -Message 'Every engine must process the expected event count.'
        } elseif ([long] $run.Result.Count -le 0) {
            throw 'The benchmark engine did not process any events.'
        }
        if ($run.Result.PSObject.Properties.Name -contains 'Errors') {
            assertValue -Actual ([long] $run.Result.Errors) -Expected ([long] 0) -Message 'The parser must not report EVTX errors.'
        }

        $requiresOutput = $definition.Workload -eq 'MetadataCsv' -or
            ($definition.Workload -like 'Evtx*' -and $definition.Workload -ne 'EvtxNativeParse')
        if ($requiresOutput -and ([long] $run.Result.OutputBytes -le 0)) {
            throw "The $($definition.Workload) benchmark did not produce a non-empty output file."
        }
        if ($requiresOutput -and
            [string]::IsNullOrWhiteSpace([string] $run.Result.OutputSha256)) {
            throw "The $($definition.Workload) benchmark did not record its output SHA-256 hash."
        }

        if ($definition.Workload -notlike 'Evtx*') {
            if ([long] $run.Result.IdSum -le 0 -or [long] $run.Result.RecordIdSum -le 0) {
                throw 'The common-work benchmark did not record non-empty event identity checks.'
            }
            if ($null -eq $run.Result.FirstRecordId -or $null -eq $run.Result.LastRecordId) {
                throw 'The common-work benchmark did not record its first and last record IDs.'
            }

            $readMode = if ($definition.Workload -eq 'MetadataCsv') { 'Metadata' } else { $definition.Workload }
            $identityKey = '{0}|{1}' -f $definition.Fixture, $definition.MaxEvents
            $identitySignature = '{0}|{1}|{2}|{3}|{4}|{5}' -f
                [long] $run.Result.Count,
                [long] $run.Result.IdSum,
                [long] $run.Result.RecordIdSum,
                [long] $run.Result.TimeTicksXor,
                [long] $run.Result.FirstRecordId,
                [long] $run.Result.LastRecordId
            if ($commonIdentitySignatures.ContainsKey($identityKey)) {
                assertValue -Actual $identitySignature -Expected $commonIdentitySignatures[$identityKey] -Message 'Common-work engines must process the same ordered event identity set.'
            } else {
                $commonIdentitySignatures[$identityKey] = $identitySignature
            }

            if ($definition.Workload -eq 'MetadataCsv') {
                $outputKey = '{0}|{1}' -f $definition.Fixture, $definition.ExpectedCount
                $outputHash = [string] $run.Result.OutputSha256
                if ($exactOutputHashes.ContainsKey($outputKey)) {
                    assertValue -Actual $outputHash -Expected $exactOutputHashes[$outputKey] -Message 'Exact-output engines must produce a byte-identical metadata CSV.'
                } else {
                    $exactOutputHashes[$outputKey] = $outputHash
                }
            }

            if ($readMode -eq 'Metadata') {
                assertValue -Actual ([long] $run.Result.MessageCharacters) -Expected ([long] 0) -Message 'Metadata mode must not format messages.'
                assertValue -Actual ([long] $run.Result.XmlCharacters) -Expected ([long] 0) -Message 'Metadata mode must not materialize XML.'
                assertValue -Actual ([long] $run.Result.PropertyCount) -Expected ([long] 0) -Message 'Metadata mode must not materialize event properties.'
            } elseif ($readMode -eq 'Message') {
                assertValue -Actual ([long] $run.Result.XmlCharacters) -Expected ([long] 0) -Message 'Message mode must not materialize XML.'
                assertValue -Actual ([long] $run.Result.PropertyCount) -Expected ([long] 0) -Message 'Message mode must not materialize event properties.'
            } elseif ($readMode -eq 'StructuredData') {
                assertValue -Actual ([long] $run.Result.MessageCharacters) -Expected ([long] 0) -Message 'StructuredData mode must not format messages.'
                if ([long] $run.Result.XmlCharacters -le 0 -or [long] $run.Result.PropertyCount -le 0) {
                    throw 'StructuredData mode did not materialize XML and event properties.'
                }
            } elseif ($readMode -eq 'Full' -and
                ([long] $run.Result.XmlCharacters -le 0 -or [long] $run.Result.PropertyCount -le 0)) {
                throw 'Full mode did not materialize XML and event properties.'
            }
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

    metric IdSum {
        param($case, $run)
        if ($run.Result.PSObject.Properties.Name -contains 'IdSum') {
            [long] $run.Result.IdSum
        } else {
            0
        }
    }

    metric RecordIdSum {
        param($case, $run)
        if ($run.Result.PSObject.Properties.Name -contains 'RecordIdSum') {
            [long] $run.Result.RecordIdSum
        } else {
            0
        }
    }

    metric FirstRecordId {
        param($case, $run)
        if ($null -ne $run.Result.FirstRecordId) {
            [long] $run.Result.FirstRecordId
        } else {
            0
        }
    }

    metric LastRecordId {
        param($case, $run)
        if ($null -ne $run.Result.LastRecordId) {
            [long] $run.Result.LastRecordId
        } else {
            0
        }
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

    metric StructuredFieldCount {
        param($case, $run)
        if ($run.Result.PSObject.Properties.Name -contains 'StructuredFieldCount') {
            [long] $run.Result.StructuredFieldCount
        } else {
            0
        }
    }

    metric MessageFieldCount {
        param($case, $run)
        if ($run.Result.PSObject.Properties.Name -contains 'MessageFieldCount') {
            [long] $run.Result.MessageFieldCount
        } else {
            0
        }
    }

    metric AttachmentBytes {
        param($case, $run)
        if ($run.Result.PSObject.Properties.Name -contains 'AttachmentBytes') {
            [long] $run.Result.AttachmentBytes
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

    comparison Engine -Baseline PSEventViewer -Metric MedianMs -TieTolerance 0.03
    if ($readmeTable -eq 'Common') {
        readme $mainReadmePath -Block 'event-log-common-benchmark' -Renderer ComparisonTable
    } elseif ($readmeTable -eq 'EvtxNative') {
        readme $mainReadmePath -Block 'event-log-evtx-native-benchmark' -Renderer SummaryTable
    }
    artifacts Json, Csv, Markdown
}
