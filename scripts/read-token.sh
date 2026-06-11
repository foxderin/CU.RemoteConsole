#!/usr/bin/env bash
set -euo pipefail

if [[ "${BASH_SOURCE[0]}" == "$0" ]]; then
  echo "read-token.sh is an internal helper and does not print tokens. Source it from another script." >&2
  exit 2
fi

if [[ -z "${GAME_DIR:-}" && -n "${CU_GAME_DIR:-}" ]]; then
  GAME_DIR="$CU_GAME_DIR"
fi

if [[ -z "${GAME_DIR:-}" && -z "${CU_REMOTE_CONSOLE_CONFIG:-}" ]]; then
  echo "Set GAME_DIR/CU_GAME_DIR or CU_REMOTE_CONSOLE_CONFIG before reading the token." >&2
  return 1
fi

CONFIG_FILE="${CU_REMOTE_CONSOLE_CONFIG:-$GAME_DIR/BepInEx/config/cu.remoteconsole.cfg}"

if [[ ! -f "$CONFIG_FILE" ]]; then
  echo "CU.RemoteConsole config not found: $CONFIG_FILE" >&2
  return 1
fi

CU_REMOTE_CONSOLE_TOKEN="$(
  awk -F'= ' '/^Token = / { value = $2 } END { print value }' "$CONFIG_FILE"
)"

if [[ -z "$CU_REMOTE_CONSOLE_TOKEN" ]]; then
  echo "CU.RemoteConsole token is missing in config: $CONFIG_FILE" >&2
  return 1
fi

export CU_REMOTE_CONSOLE_TOKEN
