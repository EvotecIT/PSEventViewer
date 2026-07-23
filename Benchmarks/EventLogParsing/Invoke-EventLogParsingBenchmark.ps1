<#
.SYNOPSIS
Builds and runs the reproducible EVTX parsing benchmark.

.DESCRIPTION
Builds the current EventViewerX/PSEventViewer sources, then delegates timing,
iteration order, validation, comparison, and artifacts to Invoke-BenchmarkSuite.
Large EVTX fixtures and competitor binaries remain external inputs.

.EXAMPLE
.\Invoke-EventLogParsingBenchmark.ps1 -Case Smoke-Common-Scan-Metadata -Engine DotNet, EventViewerX, GetWinEvent, PSEventViewer

.EXAMPLE
.\Invoke-EventLogParsingBenchmark.ps1 -LargeFixturePath C:\Temp\Security.evtx -ExpectedLargeCount 1000000 -EvtxECmdPath C:\Tools\EvtxECmd.exe -Case Large-Evtx-FullJson

.EXAMPLE
.\Invoke-EventLogParsingBenchmark.ps1 -LargeFixturePath C:\Temp\Security.evtx -ExpectedLargeCount 1000000 -ReadmeTable Common -IterationCount 3
#>
[CmdletBinding()]
param(
    [string[]] $Case,

    [string[]] $Engine,

    [string] $LargeFixturePath,

    [ValidateRange(0, [long]::MaxValue)]
    [long] $ExpectedLargeCount,

    [ValidateRange(1, [int]::MaxValue)]
    [int] $ExpensiveSampleCount = 100000,

    [string] $EvtxECmdPath,

    [string] $BaselineHostPath,

    [string] $BaselineModulePath,

    [string] $OutputRoot,

    [ValidateRange(0, [int]::MaxValue)]
    [int] $WarmupCount = 0,

    [ValidateRange(1, [int]::MaxValue)]
    [int] $IterationCount = 1,

    [ValidateSet('None', 'Common', 'EvtxNative')]
    [string] $ReadmeTable = 'None',

    [switch] $Plan
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$hostProject = Join-Path $PSScriptRoot 'EventLogParsing.BenchmarkHost\EventLogParsing.BenchmarkHost.csproj'
$specPath = Join-Path $PSScriptRoot 'event-log-parsing.benchmark.ps1'

Import-Module PSPublishModule -MinimumVersion 3.0.76 -ErrorAction Stop

if ($ReadmeTable -ne 'None') {
    if (-not $LargeFixturePath -or $ExpectedLargeCount -le 0) {
        throw 'ReadmeTable requires LargeFixturePath and a positive ExpectedLargeCount.'
    }
    if ($Case -or $Engine) {
        throw 'ReadmeTable owns its curated Case and Engine matrix. Do not combine it with Case or Engine.'
    }

    if ($ReadmeTable -eq 'Common') {
        if (-not $Plan -and $IterationCount -lt 3) {
            throw 'The public common-work table requires at least three iterations.'
        }
        $Case = @(
            'Large-Common-Scan-Metadata'
            'Large-Common-Sample-Message'
            'Large-Common-Sample-StructuredData'
            'Large-Common-Sample-Full'
            'Large-Exact-Export-MetadataCsv'
        )
        $Engine = 'DotNet', 'EventViewerX', 'PSEventViewer', 'GetWinEvent'
    } else {
        if (-not $EvtxECmdPath) {
            throw 'ReadmeTable EvtxNative requires EvtxECmdPath.'
        }
        $Case = @(
            'Large-Evtx-NativeParse'
            'Large-Evtx-ForensicCsv'
            'Large-Evtx-FullJson'
            'Large-Evtx-Xml'
        )
        $Engine = 'EvtxECmd'
    }
}

dotnet build $hostProject --configuration Release --framework net10.0-windows
if ($LASTEXITCODE -ne 0) {
    throw "The benchmark host build failed with exit code $LASTEXITCODE."
}

dotnet build (Join-Path $repositoryRoot 'Sources\PSEventViewer\PSEventViewer.csproj') --configuration Release --framework net10.0-windows
if ($LASTEXITCODE -ne 0) {
    throw "The PSEventViewer build failed with exit code $LASTEXITCODE."
}

$variables = @{
    ReadmeTable = $ReadmeTable
}
if ($LargeFixturePath) {
    $variables.LargeFixturePath = [IO.Path]::GetFullPath($LargeFixturePath)
    $variables.ExpectedLargeCount = $ExpectedLargeCount
    $variables.ExpensiveSampleCount = $ExpensiveSampleCount
}
if ($EvtxECmdPath) {
    $variables.EvtxECmdPath = [IO.Path]::GetFullPath($EvtxECmdPath)
}
if ($BaselineHostPath) {
    $variables.BaselineHostPath = [IO.Path]::GetFullPath($BaselineHostPath)
}
if ($BaselineModulePath) {
    $variables.BaselineModulePath = [IO.Path]::GetFullPath($BaselineModulePath)
}

$parameters = @{
    Path           = $specPath
    Variable       = $variables
    WarmupCount    = $WarmupCount
    IterationCount = $IterationCount
    Plan           = $Plan
}
if ($Case) {
    $parameters.Case = $Case
}
if ($Engine) {
    $parameters.Engine = $Engine
}
if ($OutputRoot) {
    $parameters.OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
}

$benchmarkResult = Invoke-BenchmarkSuite @parameters
if (-not $Plan) {
    [array] $failedSamples = foreach ($sample in $benchmarkResult.Samples) {
        if ($sample.Status -eq 'Failed') {
            $sample
        }
    }
    if ($failedSamples.Count -gt 0) {
        [array] $failureSummary = foreach ($sample in $failedSamples) {
            '{0}/{1}/iteration-{2}: {3}' -f $sample.Scenario, $sample.Engine, $sample.Iteration, $sample.Reason
        }
        throw "The benchmark completed with $($failedSamples.Count) failed sample(s):`n$($failureSummary -join "`n")"
    }
}

$benchmarkResult
