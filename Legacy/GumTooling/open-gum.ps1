$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$GumExe = Join-Path $Root ".tools\Gum\Gum.exe"
$GumProject = Join-Path $Root "Content\GumProject\Graphite.gumx"
$LocalDotnetRoot = Join-Path $Root ".dotnet"
$DesktopRuntimeRoot = Join-Path $LocalDotnetRoot "shared\Microsoft.WindowsDesktop.App"

if (-not (Test-Path $GumExe)) {
    throw "Gum is not installed. Run .\Scripts\install-gum.ps1 first."
}
if (-not (Test-Path $GumProject)) {
    throw "The Graphite Gum project was not found: $GumProject"
}
if (-not (Test-Path $DesktopRuntimeRoot)) {
    throw "The project-local .NET Desktop runtime was not found. Run .\setup.cmd first."
}

$PreviousDotnetRoot = [Environment]::GetEnvironmentVariable("DOTNET_ROOT", "Process")
$PreviousDotnetRootX64 = [Environment]::GetEnvironmentVariable("DOTNET_ROOT_X64", "Process")

try {
    # Gum is a framework-dependent desktop app. Point its host at Graphite's
    # project-local .NET 8 installation instead of the machine-wide .NET 10 host.
    $env:DOTNET_ROOT = $LocalDotnetRoot
    $env:DOTNET_ROOT_X64 = $LocalDotnetRoot
    Start-Process -FilePath $GumExe -ArgumentList @($GumProject)
}
finally {
    if ($null -eq $PreviousDotnetRoot) {
        Remove-Item Env:DOTNET_ROOT -ErrorAction SilentlyContinue
    }
    else {
        $env:DOTNET_ROOT = $PreviousDotnetRoot
    }

    if ($null -eq $PreviousDotnetRootX64) {
        Remove-Item Env:DOTNET_ROOT_X64 -ErrorAction SilentlyContinue
    }
    else {
        $env:DOTNET_ROOT_X64 = $PreviousDotnetRootX64
    }
}
