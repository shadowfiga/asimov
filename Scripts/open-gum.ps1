$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$GumExe = Join-Path $Root ".tools\Gum\Gum.exe"
$GumProject = Join-Path $Root "Content\GumProject\Graphite.gumx"

if (-not (Test-Path $GumExe)) {
    throw "Gum is not installed. Run .\Scripts\install-gum.ps1 first."
}
if (-not (Test-Path $GumProject)) {
    throw "The Graphite Gum project was not found: $GumProject"
}

Start-Process -FilePath $GumExe -ArgumentList @($GumProject)
