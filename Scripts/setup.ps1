$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$LocalDotnet = Join-Path $Root ".dotnet"
$LocalDotnetExe = Join-Path $LocalDotnet "dotnet.exe"
$GlobalDotnet = Get-Command dotnet -ErrorAction SilentlyContinue

if (Test-Path $LocalDotnetExe) {
    $DotnetExe = $LocalDotnetExe
} elseif ($GlobalDotnet) {
    $HasNet8Sdk = (& $GlobalDotnet.Source --list-sdks) -match '^8\.'
    $HasNet8Runtime = (& $GlobalDotnet.Source --list-runtimes) -match '^Microsoft\.NETCore\.App 8\.'

    if ($HasNet8Sdk -and $HasNet8Runtime) {
        $DotnetExe = $GlobalDotnet.Source
    }
}

if (-not $DotnetExe) {
    Write-Host "No compatible .NET 8 SDK and runtime found. Installing locally into .dotnet (no admin)..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path $LocalDotnet | Out-Null
    $Installer = Join-Path $LocalDotnet "dotnet-install.ps1"
    Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile $Installer
    & powershell -NoProfile -ExecutionPolicy Bypass -File $Installer -Channel 8.0 -InstallDir $LocalDotnet -NoPath
    $DotnetExe = $LocalDotnetExe
}

Write-Host "Using .NET $(& $DotnetExe --version)" -ForegroundColor Cyan
Set-Location $Root

Write-Host "Restoring Graphite dependencies..." -ForegroundColor Cyan
& $DotnetExe restore

Write-Host "Building Graphite..." -ForegroundColor Cyan
& $DotnetExe build --no-restore

Write-Host "Done. Launching the game..." -ForegroundColor Green
& $DotnetExe run --no-build
