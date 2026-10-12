[CmdletBinding()]
param(
    [ValidateRange(1, [int]::MaxValue)]
    [int]$Runs = 1000,
    [string]$State = (Join-Path $PSScriptRoot 'Prelude/Simulator/examples/intro.json'),
    [Nullable[int]]$Seed,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'simulation-results'),
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$statePath = (Resolve-Path -LiteralPath $State).Path
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$runName = 'simulation-{0}-{1}' -f (Get-Date -Format 'yyyyMMdd-HHmmss-fff'), ([guid]::NewGuid().ToString('N').Substring(0, 8))
$logPath = Join-Path $outputPath "$runName.log"
$resultsPath = Join-Path $outputPath "$runName.json"
$projectPath = Join-Path $PSScriptRoot 'Prelude/Simulator/Simulator.csproj'
$simulatorArguments = @('--state', $statePath, '--runs', "$Runs", '--output', $resultsPath)
if ($null -ne $Seed) { $simulatorArguments += @('--seed', "$Seed") }

Write-Host "Console log: $logPath"
Write-Host "Results JSON: $resultsPath"
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
