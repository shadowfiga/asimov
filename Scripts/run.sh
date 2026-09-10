#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ -x "$ROOT/.dotnet/dotnet" ]]; then
  DOTNET="$ROOT/.dotnet/dotnet"
elif command -v dotnet >/dev/null 2>&1; then
  DOTNET="dotnet"
elif [[ -x "$HOME/.dotnet/dotnet" ]]; then
  DOTNET="$HOME/.dotnet/dotnet"
else
  echo "No .NET installation found. Run ./setup.sh first."
  exit 1
fi
cd "$ROOT"
exec "$DOTNET" run -- "$@"
