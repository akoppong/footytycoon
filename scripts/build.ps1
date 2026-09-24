param([switch]$Desktop)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $root
$localSdk = Join-Path $root '.tools/dotnet/dotnet.exe'
$sdk = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_HOME = Join-Path $root '.tools/cli'
$env:APPDATA = Join-Path $root '.tools/appdata'
New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
& $sdk restore FootballTycoon.slnx --configfile NuGet.Config
if ($LASTEXITCODE -ne 0) { throw 'Dependency restore failed.' }
& $sdk build FootballTycoon.slnx --no-restore --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
& $sdk test FootballTycoon.slnx --no-build --no-restore --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
if ($Desktop) {
    & $sdk restore src/FootballTycoon.Desktop --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'Godot dependency restore failed.' }
    & $sdk build src/FootballTycoon.Desktop --no-restore --configuration Debug --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Godot build failed.' }
}
