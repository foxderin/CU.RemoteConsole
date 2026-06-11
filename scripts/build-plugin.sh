#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if [[ ! -f package-lock.json ]]; then
  echo "package-lock.json is required for the local Tailwind build." >&2
  exit 1
fi

npm ci
npm run build:web

if [[ -z "${GAME_DIR:-}" && -z "${CU_GAME_DIR:-}" ]]; then
  echo "Set GAME_DIR or CU_GAME_DIR to the Casualties: Unknown install directory before building the plugin." >&2
  exit 1
fi

dotnet build src/CU.RemoteConsole/CU.RemoteConsole.csproj -c Release

echo "Built src/CU.RemoteConsole/bin/Release/CU.RemoteConsole.dll"
