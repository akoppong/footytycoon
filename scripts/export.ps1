param([string]$Godot)
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
New-Item -ItemType Directory -Force $env:APPDATA,$env:LOCALAPPDATA | Out-Null
if (!$Godot) {
    $engine = Get-ChildItem -LiteralPath (Join-Path $root '.tools/godot') -Filter '*win64.exe' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($engine) { $Godot = $engine.FullName } else { $Godot = (Get-Command godot -ErrorAction Stop).Source }
}
$template = Join-Path $env:APPDATA 'Godot/export_templates/4.7.2.stable.mono/windows_release_x86_64.exe'
if (!(Test-Path -LiteralPath $template)) {
    throw "Install the official Godot 4.7.2 .NET Windows templates in $(Split-Path -Parent $template). See README."
}
& $sdk restore src/FootballTycoon.Desktop/FootballTycoon.Desktop.csproj -r win-x64 -p:Configuration=ExportRelease -p:SelfContained=true --configfile NuGet.Config
if ($LASTEXITCODE -ne 0) { throw 'Export dependency restore failed.' }
# A fresh folder ensures a failed export cannot accidentally validate an older binary.
$output = Join-Path $root ('artifacts/windows-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $output | Out-Null
$executable = Join-Path $output 'FootballTycoon.exe'
# Explicitly wait for the engine executable: PowerShell may return immediately for a GUI subsystem binary.
$exportArguments = @('--headless', '--path', ('"' + (Join-Path $root 'src/FootballTycoon.Desktop') + '"'), '--export-release', '"Windows Desktop"', ('"' + $executable + '"'))
$exportProcess = Start-Process -FilePath $Godot -ArgumentList $exportArguments -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $output 'export.log') -RedirectStandardError (Join-Path $output 'export-errors.log')
# Wait only for Godot. Start-Process -Wait also waits for reusable MSBuild descendants after export completes.
$exportProcess.WaitForExit()
if ($exportProcess.ExitCode -ne 0 -or !(Test-Path -LiteralPath $executable)) { throw "Windows export failed. Inspect $output/export.log and export-errors.log." }
# Font licenses are text files, so Godot's resource-only export does not package them automatically.
$fontLicenses = @(Get-ChildItem -LiteralPath (Join-Path $root 'src/FootballTycoon.Desktop/Assets/Fonts') -Filter '*-OFL.txt')
if ($fontLicenses.Count -ne 3) { throw 'Expected the three bundled font licenses before distributing the build.' }
$licenseDirectory = Join-Path $output 'licenses'
New-Item -ItemType Directory -Path $licenseDirectory | Out-Null
foreach ($license in $fontLicenses) { Copy-Item -LiteralPath $license.FullName -Destination $licenseDirectory }
$env:DOTNET_ROOT = ''
$env:APPDATA = Join-Path $root '.tools/export-appdata'
$env:LOCALAPPDATA = Join-Path $root '.tools/export-localappdata'
$stdout = Join-Path $output 'smoke.log'
$stderr = Join-Path $output 'smoke-errors.log'
$process = Start-Process -FilePath $executable -ArgumentList @('--headless', '--', '--smoke-test') -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
if (!$process.WaitForExit(60000)) { $process.Kill(); throw 'Export smoke test timed out.' }
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $stdout -SimpleMatch 'SMOKE PASS' -Quiet)) {
    throw "Export smoke test failed. Inspect $stdout and $stderr."
}
Write-Output "Verified Windows prototype: $executable"
Write-Output 'Keep the executable, .pck, data directory and licenses directory together when copying this build.'
