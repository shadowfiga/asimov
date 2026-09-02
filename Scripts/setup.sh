#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOCAL_DOTNET="$ROOT/.dotnet"

say() { printf '\n\033[1;36m%s\033[0m\n' "$1"; }

if [[ -x "$LOCAL_DOTNET/dotnet" ]]; then
  DOTNET="$LOCAL_DOTNET/dotnet"
elif command -v dotnet >/dev/null 2>&1 \
  && dotnet --list-sdks | grep -q '^8\.' \
  && dotnet --list-runtimes | grep -q '^Microsoft\.NETCore\.App 8\.'; then
  DOTNET="$(command -v dotnet)"
else
  say "No compatible .NET 8 SDK and runtime found. Installing locally into .dotnet (no sudo/admin)..."
  mkdir -p "$LOCAL_DOTNET"
  INSTALLER="$ROOT/.dotnet-install.sh"
  if command -v curl >/dev/null 2>&1; then
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$INSTALLER"
  elif command -v wget >/dev/null 2>&1; then
    wget -q https://dot.net/v1/dotnet-install.sh -O "$INSTALLER"
  else
    echo "Need curl or wget to bootstrap .NET." >&2
    exit 1
  fi
  bash "$INSTALLER" --channel 8.0 --install-dir "$LOCAL_DOTNET" --no-path
  DOTNET="$LOCAL_DOTNET/dotnet"
fi

say "Using: $($DOTNET --version)"
cd "$ROOT"

say "Restoring Graphite dependencies..."
"$DOTNET" restore

say "Building Graphite..."
"$DOTNET" build --no-restore

say "Done. Launching the game..."
exec "$DOTNET" run --no-build
