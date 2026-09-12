$ErrorActionPreference = 'Stop'
$shaderProjectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $shaderProjectRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Shader compiler restore failed.' }
    foreach ($shaderSource in Get-ChildItem Content/Shaders/*.fx) {
        $shaderOutput = [System.IO.Path]::ChangeExtension($shaderSource.FullName, '.mgfxo')
        dotnet tool run mgfxc $shaderSource.FullName $shaderOutput /Profile:OpenGL
        if ($LASTEXITCODE -ne 0) { throw "Shader compilation failed: $($shaderSource.Name)" }
    }
} finally {
    Pop-Location
}
