[CmdletBinding()]
param(
    [int]$Runs,
    [string]$State = (Join-Path $PSScriptRoot '../Simulator/examples/intro.json'),
    [Nullable[int]]$Seed,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\.temp\simulator-results'),
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
if ($PSBoundParameters.ContainsKey('Runs') -and $Runs -le 0) {
    throw 'Runs must be a positive integer.'
}
$statePath = (Resolve-Path -LiteralPath $State).Path
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$runName = 'simulation-{0}-{1}' -f (Get-Date -Format 'yyyyMMdd-HHmmss-fff'), ([guid]::NewGuid().ToString('N').Substring(0, 8))
$logPath = Join-Path $outputPath "$runName.log"
$resultsPath = Join-Path $outputPath "$runName.json"
$reportPath = Join-Path $outputPath "$runName.md"
$projectPath = Join-Path $PSScriptRoot '..\Simulator\Simulator.csproj'
$simulatorArguments = @('--state', $statePath, '--output', $resultsPath, '--report', $reportPath)
if ($PSBoundParameters.ContainsKey('Runs')) { $simulatorArguments += @('--runs', "$Runs") }
if ($null -ne $Seed) { $simulatorArguments += @('--seed', "$Seed") }

Write-Host "Console log: $logPath"
Write-Host "Results JSON: $resultsPath"
Write-Host "Summary Markdown: $reportPath"
if (!$NoBuild) {
    & dotnet build $projectPath --disable-build-servers -m:1 --verbosity quiet 2>&1 |
    Tee-Object -FilePath $logPath
    if ($LASTEXITCODE -ne 0) { throw "Simulator build failed. See $logPath" }
}
& dotnet run --project $projectPath --no-build --no-launch-profile -- @simulatorArguments 2>&1 |
Tee-Object -FilePath $logPath -Append
$simulatorExitCode = $LASTEXITCODE
if ($simulatorExitCode -ne 0) {
    throw "Simulator exited with code $simulatorExitCode. See $logPath"
}
