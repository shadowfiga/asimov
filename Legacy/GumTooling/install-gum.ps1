$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$ToolsRoot = Join-Path $Root ".tools"
$InstallRoot = Join-Path $ToolsRoot "Gum"
$Archive = Join-Path $ToolsRoot "Gum.zip"
$Release = "Release_September_02_2026"
$DownloadUrl = "https://github.com/vchelaru/Gum/releases/download/$Release/Gum.zip"

if (Test-Path (Join-Path $InstallRoot "Gum.exe")) {
    Write-Host "Gum is already installed at $InstallRoot" -ForegroundColor Green
    exit 0
}

Write-Host "Installing Gum $Release locally..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $ToolsRoot | Out-Null
Invoke-WebRequest $DownloadUrl -OutFile $Archive

try {
    if (Test-Path $InstallRoot) {
        Remove-Item -LiteralPath $InstallRoot -Recurse -Force
    }
    Expand-Archive -LiteralPath $Archive -DestinationPath $InstallRoot
} finally {
    if (Test-Path $Archive) {
        Remove-Item -LiteralPath $Archive -Force
    }
}

$GumExe = Join-Path $InstallRoot "Gum.exe"
if (-not (Test-Path $GumExe)) {
    throw "Gum.exe was not found after extracting $DownloadUrl"
}

Write-Host "Gum installed at $GumExe" -ForegroundColor Green
