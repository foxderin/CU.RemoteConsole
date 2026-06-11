#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

dotnet run --project tests/CU.RemoteConsole.Tests/CU.RemoteConsole.Tests.csproj -c Release
