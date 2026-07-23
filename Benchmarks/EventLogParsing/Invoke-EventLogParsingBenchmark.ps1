<#
.SYNOPSIS
Builds and runs the reproducible EVTX parsing benchmark.

.DESCRIPTION
Builds the current EventViewerX/PSEventViewer sources, then delegates timing,
iteration order, validation, comparison, and artifacts to Invoke-BenchmarkSuite.
Large EVTX fixtures and competitor binaries remain external inputs.

.EXAMPLE
.\Invoke-EventLogParsingBenchmark.ps1 -Case Smoke-Scan-Metadata -Engine DotNet, EventViewerX, GetWinEvent, PSEventViewer

.EXAMPLE
.\Invoke-EventLogParsingBenchmark.ps1 -LargeFixturePath C:\Temp\Security.evtx -ExpectedLargeCount 1000000 -EvtxECmdPath C:\Tools\EvtxECmd.exe -Case Large-Scan-Metadata
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

    [switch] $Plan
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$hostProject = Join-Path $PSScriptRoot 'EventLogParsing.BenchmarkHost\EventLogParsing.BenchmarkHost.csproj'
$specPath = Join-Path $PSScriptRoot 'event-log-parsing.benchmark.ps1'

Import-Module PSPublishModule -MinimumVersion 3.0.76 -ErrorAction Stop

dotnet build $hostProject --configuration Release --framework net10.0-windows
if ($LASTEXITCODE -ne 0) {
    throw "The benchmark host build failed with exit code $LASTEXITCODE."
}

dotnet build (Join-Path $repositoryRoot 'Sources\PSEventViewer\PSEventViewer.csproj') --configuration Release --framework net10.0-windows
if ($LASTEXITCODE -ne 0) {
    throw "The PSEventViewer build failed with exit code $LASTEXITCODE."
}

$variables = @{}
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

Invoke-BenchmarkSuite @parameters
