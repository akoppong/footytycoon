param(
    [Parameter(Mandatory)][string]$BaselineCore,
    [Parameter(Mandatory)][string]$CandidateCore,
    [Parameter(Mandatory)][string]$CheckpointRoot,
    [Parameter(Mandatory)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$taskOutput = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $taskOutput) { throw 'Choose a new output directory; evidence is never overwritten.' }
New-Item -ItemType Directory -Path $taskOutput | Out-Null
$taskBuild = Join-Path $taskOutput 'driver'
New-Item -ItemType Directory -Path $taskBuild | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'MandateDefaultEquivalence.cs') -Destination (Join-Path $taskBuild 'Program.cs')
$taskProject = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup></Project>'
[IO.File]::WriteAllText((Join-Path $taskBuild 'Equivalence.csproj'), $taskProject)
$sdk = Join-Path $repoRoot '.tools/dotnet/dotnet.exe'
$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.tools/cli'
& $sdk build (Join-Path $taskBuild 'Equivalence.csproj') --configuration Release --nologo -m:1 -nodeReuse:false -p:UseSharedCompilation=false *> (Join-Path $taskOutput 'build.log')
if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $taskOutput 'build.log'); throw 'Equivalence driver build failed.' }
& $sdk (Join-Path $taskBuild 'bin/Release/net10.0/Equivalence.dll') ([IO.Path]::GetFullPath($BaselineCore)) ([IO.Path]::GetFullPath($CandidateCore)) ([IO.Path]::GetFullPath($CheckpointRoot)) (Join-Path $taskOutput 'results') *> (Join-Path $taskOutput 'run.log')
Get-Content (Join-Path $taskOutput 'run.log')
if ($LASTEXITCODE -ne 0) { throw 'Default equivalence failed; inspect retained evidence.' }
