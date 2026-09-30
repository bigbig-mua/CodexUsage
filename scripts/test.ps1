#Requires -Version 5.1
[CmdletBinding()]
param(
    [ValidateSet('Domain', 'Bridge', 'Taskbar', 'TaskbarLive', 'All')][string]$Suite = 'Domain',
    [switch]$BuildOnly
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $repositoryRoot 'src'
$testRoot = Join-Path $repositoryRoot 'tests'
$outputRoot = Join-Path $repositoryRoot 'env/tests'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'The Windows x64 .NET Framework compiler was not found. See docs/BUILD.md.'
}
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$previousTemp = $env:TEMP
$previousTmp = $env:TMP
function Build-Check([string]$Name, [string[]]$Inputs, [string]$ManifestPath) {
    $destination = Join-Path $outputRoot ($Name + '.exe')
    $manifestArguments = @()
    if ($ManifestPath) { $manifestArguments = @("/win32manifest:$ManifestPath") }
    & $compiler /nologo /utf8output /codepage:65001 /langversion:5 /warnaserror+ /target:exe /platform:x64 "/out:$destination" /r:System.Web.Extensions.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll @manifestArguments @Inputs | Out-Host
    if ($LASTEXITCODE -ne 0) { throw ('Test compilation failed: ' + $Name) }
    return $destination
}
try {
    $env:TEMP = $outputRoot
    $env:TMP = $outputRoot
    if ($Suite -eq 'Domain' -or $Suite -eq 'All') {
        $inputs = @('QuotaModels.cs','QuotaParser.cs','AppSettings.cs','AppPaths.cs','InteractionState.cs','ResetFeed.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
        $domain = Build-Check 'DomainTests' (@((Join-Path $testRoot 'DomainTests.cs')) + $inputs)
        if (-not $BuildOnly) {
            & $domain
            if ($LASTEXITCODE -ne 0) { throw ('Domain checks failed with exit code ' + $LASTEXITCODE) }
        }
    }
    if ($Suite -eq 'Bridge' -or $Suite -eq 'All') {
        $fake = Build-Check 'FakeCodexServer' @((Join-Path $testRoot 'FakeCodexServer.cs'))
        $inputs = @('IQuotaSource.cs','CodexQuotaSource.cs','ProcessJob.cs','QuotaModels.cs','QuotaParser.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
        $bridge = Build-Check 'BridgeTests' (@((Join-Path $testRoot 'BridgeTests.cs')) + $inputs)
        if (-not $BuildOnly) {
            & $bridge $fake (Join-Path $outputRoot 'fake-support')
            if ($LASTEXITCODE -ne 0) { throw ('Bridge checks failed with exit code ' + $LASTEXITCODE) }
        }
    }
    if ($Suite -eq 'Taskbar' -or $Suite -eq 'All') {
        $inputs = @('TaskbarPlacement.cs','Theme.cs','WidgetRenderer.cs','UiText.cs','QuotaModels.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
        $taskbar = Build-Check 'TaskbarTests' (@((Join-Path $testRoot 'TaskbarTests.cs')) + $inputs)
        if (-not $BuildOnly) {
            & $taskbar
            if ($LASTEXITCODE -ne 0) { throw ('Taskbar checks failed with exit code ' + $LASTEXITCODE) }
        }
    }
    if ($Suite -eq 'TaskbarLive') {
        $live = Build-Check 'TaskbarWidgetSmoke' @((Join-Path $testRoot 'TaskbarWidgetSmoke.cs')) (Join-Path $sourceRoot 'app.manifest')
        if (-not $BuildOnly) {
            & $live
            if ($LASTEXITCODE -ne 0) { throw ('Live taskbar checks failed or were skipped with exit code ' + $LASTEXITCODE) }
        }
    }
    if ($BuildOnly) { Write-Output 'Test compilation completed. No tests were executed.' }
} finally {
    $env:TEMP = $previousTemp
    $env:TMP = $previousTmp
}
