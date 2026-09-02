#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ -x "$ROOT/.dotnet/dotnet" ]]; then DOTNET="$ROOT/.dotnet/dotnet"; else DOTNET="dotnet"; fi
cd "$ROOT"
exec "$DOTNET" run
