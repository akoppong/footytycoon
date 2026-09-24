param([switch]$Templates)
# Installs the pinned official Godot .NET editor (and optionally the Windows export templates) into the
# .tools locations that scripts/run.ps1 and scripts/export.ps1 already search. Every download is verified
# against a SHA-512 pinned here, copied from the release's SHA512-SUMS.txt. Existing installs are reused.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$version = '4.7.2'
$release = "https://github.com/godotengine/godot-builds/releases/download/$version-stable"
$editorZip = "Godot_v$version-stable_mono_win64.zip"
$editorSha512 = '79229fd112b0c9cbeab82363a4ef7be18ea70f1caf86bf912789335b136fbe7e01db0053a33461438c5da1c680c17bbb10040bd09bedc51221cd4423d0367757'
$templatesTpz = "Godot_v$version-stable_mono_export_templates.tpz"
$templatesSha512 = 'bb5c41d72370ed743660361f6228006f808ab04ca33abdc545d740b044f3fe057f32ae8cb7873a1bc86ddcd82ae683b9f6dfdfe4179852f2c0f1acde2ff6bd5a'
$downloads = Join-Path ([IO.Path]::GetTempPath()) 'godot-downloads'
New-Item -ItemType Directory -Force $downloads | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-Verified([string]$name, [string]$sha512) {
    $path = Join-Path $downloads $name
    Invoke-WebRequest -Uri "$release/$name" -OutFile $path
    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA512).Hash
    if ($actual -ne $sha512) { Remove-Item -LiteralPath $path; throw "$name failed SHA-512 verification (got $actual)." }
    return $path
}

$editorDir = Join-Path $root '.tools/godot'
$console = Get-ChildItem -LiteralPath $editorDir -Filter '*console.exe' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if ($console) { Write-Output "Godot editor already installed: $($console.FullName)" }
else {
    $zip = Get-Verified $editorZip $editorSha512
    [IO.Compression.ZipFile]::ExtractToDirectory($zip, $editorDir)
    Remove-Item -LiteralPath $zip
    Write-Output "Installed Godot $version .NET editor under $editorDir"
}

if ($Templates) {
    $templateDir = Join-Path $root ".tools/appdata/Godot/export_templates/$version.stable.mono"
    $needed = @('windows_release_x86_64.exe', 'windows_debug_x86_64.exe', 'version.txt')
    if (@($needed | Where-Object { !(Test-Path -LiteralPath (Join-Path $templateDir $_)) }).Count -eq 0) {
        Write-Output "Export templates already installed: $templateDir"
    }
    else {
        # The archive holds every platform (~1.2 GB); only the three Windows files the export needs are kept.
        $tpz = Get-Verified $templatesTpz $templatesSha512
        New-Item -ItemType Directory -Force $templateDir | Out-Null
        $archive = [IO.Compression.ZipFile]::OpenRead($tpz)
        try {
            foreach ($file in $needed) {
                $entry = $archive.Entries | Where-Object { $_.FullName -eq "templates/$file" } | Select-Object -First 1
                if (!$entry) { throw "Template archive is missing templates/$file." }
                [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $templateDir $file), $true)
            }
        }
        finally { $archive.Dispose() }
        Remove-Item -LiteralPath $tpz
        Write-Output "Installed Windows export templates under $templateDir"
    }
}
