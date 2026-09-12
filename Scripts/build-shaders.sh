#!/usr/bin/env bash
set -euo pipefail
SHADER_PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$SHADER_PROJECT_ROOT"
if [[ "$(uname -s)" != MINGW* && "$(uname -s)" != MSYS* && -z "${MGFXC_WINE_PATH:-}" ]]; then
  echo "Shader rebuilding requires Wine and MGFXC_WINE_PATH on macOS/Linux. See README.md."
  exit 1
fi
dotnet tool restore
for shader_source in Content/Shaders/*.fx; do
  dotnet tool run mgfxc "$shader_source" "${shader_source%.fx}.mgfxo" /Profile:OpenGL
done
