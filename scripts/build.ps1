#Requires -Version 5.1
[CmdletBinding()]
param(
    [ValidatePattern('^CodexUsage(?:-[A-Za-z0-9]+)*\.exe$')]
    [string]$OutputName = 'CodexUsage.exe'
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $repositoryRoot 'src'
$buildRoot = Join-Path $repositoryRoot 'env/build'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$icon = Join-Path $repositoryRoot 'assets/app.ico'
$manifest = Join-Path $sourceRoot 'app.manifest'
$license = Join-Path $repositoryRoot 'LICENSE'
$notices = Join-Path $repositoryRoot 'THIRD_PARTY_NOTICES.md'
$programText = Get-Content -LiteralPath (Join-Path $sourceRoot 'Program.cs') -Raw
$assemblyVersion = [regex]::Match($programText, 'AssemblyVersion\("([0-9.]+)"\)').Groups[1].Value
$fileVersion = [regex]::Match($programText, 'AssemblyFileVersion\("([0-9.]+)"\)').Groups[1].Value
[xml]$manifestXml = Get-Content -LiteralPath $manifest -Raw
$identity = $manifestXml.SelectSingleNode("/*[local-name()='assembly']/*[local-name()='assemblyIdentity']")
if (!$assemblyVersion -or $assemblyVersion -ne $fileVersion -or !$identity -or $identity.version -ne $assemblyVersion -or $identity.name -ne 'CodexUsage') {
    throw 'Program, file and manifest versions must match, and the manifest name must be CodexUsage.'
}
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'The Windows x64 .NET Framework compiler was not found. See docs/BUILD.md.'
}
foreach ($requiredInput in @($icon, $manifest, $license, $notices)) {
    if (-not (Test-Path -LiteralPath $requiredInput -PathType Leaf)) { throw ('Missing build input: ' + $requiredInput) }
}
$sources = @(Get-ChildItem -LiteralPath $sourceRoot -Filter '*.cs' -File | Sort-Object Name | ForEach-Object FullName)
if ($sources.Count -eq 0) { throw 'No C# sources were found in src/.' }
New-Item -ItemType Directory -Path $buildRoot -Force | Out-Null
$executable = Join-Path $buildRoot $OutputName
$checksumFile = if ($OutputName -eq 'CodexUsage.exe') { 'SHA256SUMS.txt' } else { [IO.Path]::GetFileNameWithoutExtension($OutputName) + '.SHA256SUMS.txt' }
$previousTemp = $env:TEMP
$previousTmp = $env:TMP
try {
    $env:TEMP = $buildRoot
    $env:TMP = $buildRoot
    & $compiler /nologo /codepage:65001 /langversion:5 /warnaserror+ /target:winexe /platform:x64 /optimize+ /debug- /main:CodexQuotaLite.Program "/out:$executable" "/win32manifest:$manifest" "/win32icon:$icon" "/resource:$license,CodexUsage.LICENSE.txt" "/resource:$notices,CodexUsage.THIRD_PARTY_NOTICES.txt" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll @sources
    if ($LASTEXITCODE -ne 0) { throw ('Build failed with compiler exit code ' + $LASTEXITCODE) }
    $checksum = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash
    [IO.File]::WriteAllText((Join-Path $buildRoot $checksumFile), ($checksum + '  ' + $OutputName + [Environment]::NewLine), [Text.Encoding]::ASCII)
    Get-Item -LiteralPath $executable | Select-Object FullName, Length
} finally {
    $env:TEMP = $previousTemp
    $env:TMP = $previousTmp
}
