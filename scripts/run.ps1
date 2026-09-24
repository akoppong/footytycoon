param([string]$Godot, [switch]$SmokeTest, [switch]$Headless)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $root
$localSdk = Join-Path $root '.tools/dotnet/dotnet.exe'
$sdk = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_ROOT = Split-Path -Parent $sdk
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_HOME = Join-Path $root '.tools/cli'
$env:APPDATA = Join-Path $root '.tools/appdata'
$env:LOCALAPPDATA = Join-Path $root '.tools/localappdata'
New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
New-Item -ItemType Directory -Force $env:LOCALAPPDATA | Out-Null
if (!$Godot) {
    $engine = Get-ChildItem -LiteralPath (Join-Path $root '.tools/godot') -Filter '*console.exe' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($engine) { $Godot = $engine.FullName } else { $Godot = (Get-Command godot -ErrorAction Stop).Source }
}
& $sdk build src/FootballTycoon.Desktop --configuration Debug --nologo
if ($LASTEXITCODE -ne 0) { throw 'Desktop build failed.' }
$arguments = @('--path', (Join-Path $root 'src/FootballTycoon.Desktop'))
if ($Headless) { $arguments += '--headless' }
if ($SmokeTest) {
    $env:FT_SMOKE_OUTPUT = Join-Path $root 'artifacts/screenshots'
    $arguments += @('--resolution', '1280x720', '--', '--smoke-test')
}
& $Godot @arguments
if ($LASTEXITCODE -ne 0) { throw 'Godot exited with an error.' }
