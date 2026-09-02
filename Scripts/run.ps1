$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Local = Join-Path $Root ".dotnet\dotnet.exe"
$DotnetExe = if (Test-Path $Local) { $Local } else { "dotnet" }
Set-Location $Root
& $DotnetExe run
