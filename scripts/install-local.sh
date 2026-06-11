#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ -z "${GAME_DIR:-}" && -n "${CU_GAME_DIR:-}" ]]; then
  GAME_DIR="$CU_GAME_DIR"
fi

if [[ -z "${GAME_DIR:-}" ]]; then
  echo "Set GAME_DIR or CU_GAME_DIR to the Casualties: Unknown install directory." >&2
  exit 1
fi

PLUGIN_DIR="$GAME_DIR/BepInEx/plugins/CU.RemoteConsole"
SOURCE_DLL="$ROOT/src/CU.RemoteConsole/bin/Release/CU.RemoteConsole.dll"

SKIP_BUILD=false
if [[ "${1:-}" == "--skip-build" ]]; then
  SKIP_BUILD=true
fi

if [[ "$SKIP_BUILD" == false ]]; then
  "$ROOT/scripts/build-plugin.sh"
fi

if [[ ! -f "$SOURCE_DLL" ]]; then
  echo "Build output not found: $SOURCE_DLL" >&2
  exit 1
fi

mkdir -p "$PLUGIN_DIR"
install -m 0644 "$SOURCE_DLL" "$PLUGIN_DIR/CU.RemoteConsole.dll"

echo "Installed CU.RemoteConsole.dll to $PLUGIN_DIR"
echo "Restart the game to load the installed assembly."
