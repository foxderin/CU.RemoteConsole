# CU.RemoteConsole

English | [简体中文](./README.zh-CN.md)

A BepInEx 5 mod for [Casualties: Unknown](https://store.steampowered.com/app/4576490/) that exposes a local authenticated browser/API command console.

> [!WARNING]
> CU.RemoteConsole can execute game console commands. Keep it bound to `127.0.0.1`, keep authentication enabled, do not share the generated bearer token, and do not expose the service directly to the public internet.

## Features

- Local web console at `http://127.0.0.1:8848/`.
- Bearer-token authentication.
- Safe command allowlist and dangerous-command denial by default.
- Basic rate limiting and command audit logging.
- Thread-safe command queue consumed from the Unity main thread.
- Command receipt lookup, recent history, and captured output rendering.
- Read-only status/config/policy panel.
- Browser-side connection endpoint override for custom local/LAN/tunnel addresses.
- In-game config overlay opened with `F8`, with English/Chinese toggle.
- Command catalog grouped by risk.
- English/Chinese web UI.
- Static OpenAPI contract in [`docs/api/openapi.yaml`](./docs/api/openapi.yaml).

## How It Works

CU.RemoteConsole does not call game objects from HTTP/background threads.

```text
Browser / local tool
  -> localhost HTTP API
  -> auth + origin check + command policy + audit
  -> bounded command queue
  -> Unity main-thread drain
  -> ConsoleBridge
  -> game console executor
```

## Requirements

| Item | Requirement |
| --- | --- |
| Game | Casualties: Unknown / Casualties: Unknown Demo |
| Mod loader | BepInEx 5.4.x |
| Runtime | Unity Mono / `netstandard2.1` plugin build |
| Browser | Any modern browser on the same machine |

Casualties: Unknown may support some custom content, but CU.RemoteConsole is a C# plugin and requires BepInEx to load.

## Installation

1. Install BepInEx 5.4.x for the game.
2. Download `CU.RemoteConsole-v1.1.0.zip` from the release page.
3. Copy the whole `BepInEx` folder from the release package into the game install directory.
4. Confirm the final plugin path looks like:

```text
<GameDir>\BepInEx\plugins\CU.RemoteConsole\CU.RemoteConsole.dll
```

5. Start the game once.
6. Open:

```text
http://127.0.0.1:8848/
```

7. Copy the generated bearer token from:

```text
<GameDir>\BepInEx\config\cu.remoteconsole.cfg
```

Do not share or commit the token.

Proton / Steam Deck note:
If BepInEx does not load when running the Windows build through Proton, add this Steam launch option:

```text
WINEDLLOVERRIDES=winhttp=n,b %command%
```

This is not needed for normal Windows installs.

## Quick Start

1. Start the game and enter a scene.
2. Open `http://127.0.0.1:8848/`.
3. Paste the bearer token from the generated config file.
4. Run `help`.
5. Check the output block and recent history.
6. Use the command catalog to see which commands are allowed or denied.

## Web Console

The browser console includes:

| Area | Purpose |
| --- | --- |
| Command | Submit a command and render its output block |
| Connection endpoint | Override the API base URL when the browser should connect to another local/LAN/tunnel address |
| Receipt lookup | Query one queued/executed command by id |
| Recent history | Review recent command receipts |
| Catalog | Show safe, state-changing, dangerous, and unknown command policy |
| Status | Show read-only listener, auth, queue, rate limit, patch, and policy metadata |

State-changing and dangerous commands are visible for transparency but denied by default.

The connection endpoint field is stored in browser local storage. Leave it empty to use the same origin as the page. Set it to a full endpoint such as `http://127.0.0.1:8848` only when the web page needs to talk to a different allowed address.

## API

Default local server:

```text
http://127.0.0.1:8848
```

Endpoints:

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/health` | unauthenticated health check |
| `GET` | `/api/status` | read-only runtime status |
| `POST` | `/api/commands` | submit a command |
| `GET` | `/api/commands` | recent command receipts |
| `GET` | `/api/commands/catalog` | command policy catalog |
| `GET` | `/api/commands/{queueId}` | command receipt lookup |

Submit a command:

```bash
curl -H 'Authorization: Bearer <token>' \
  -H 'Content-Type: application/json' \
  -d '{"command":"help"}' \
  http://127.0.0.1:8848/api/commands
```

See the static OpenAPI contract:

```text
docs/api/openapi.yaml
```

## Configuration

BepInEx generates the config file automatically after the first launch.

Press `F8` in game to open the CU.RemoteConsole config window. The window defaults to the system language and includes an English/Chinese toggle. A local player using this window can edit network, authentication, command-policy, command allow-list, limit, and audit settings. Risky changes such as public/LAN exposure, disabling auth, allowing state-changing/dangerous commands, or adding extra allowed commands require a second confirmation click.

Remote API users cannot change config through HTTP.

| File | Purpose |
| --- | --- |
| `<GameDir>\BepInEx\config\cu.remoteconsole.cfg` | listener, auth, policy, queue, rate limit, and audit settings |
| `<GameDir>\BepInEx\config\cu.remoteconsole.audit.log` | command audit log |

Important defaults:

| Entry | Default |
| --- | --- |
| `Network/BindAddress` | `127.0.0.1` |
| `Network/Port` | `8848` |
| `Security/RequireAuth` | `true` |
| `Security/AllowLan` | `false` |
| `Security/AllowPublic` | `false` |
| `Security/AllowStateChangingCommands` | `false` |
| `Security/DenyDangerousCommands` | `true` |
| `Security/ExtraAllowedCommands` | empty |

## Build From Source

Set `GAME_DIR` or `CU_GAME_DIR` to your local Casualties: Unknown install directory before building.

```bash
npm install
npm run build:web
dotnet build src/CU.RemoteConsole/CU.RemoteConsole.csproj -c Release
```

Useful scripts:

| Script | Purpose |
| --- | --- |
| `scripts/build-plugin.sh` | build web assets and Release plugin DLL |
| `scripts/install-local.sh` | install the built DLL into a local game directory |
| `scripts/smoke-test-local.sh` | validate a running local game/plugin instance |
| `scripts/test-logic.sh` | run small pure-logic tests |
| `scripts/package-release.sh` | create a local release package under `dist/` |

`scripts/smoke-test-local.sh` reads the configured token internally and does not print it.

## Package Contents

The release package contains:

```text
BepInEx/plugins/CU.RemoteConsole/CU.RemoteConsole.dll
README-INSTALL.txt
VERSION
CHECKSUMS.txt
```

The release package does not include BepInEx, game files, third-party mods, token/config files, `node_modules`, source code, tests, or build intermediates.

## Credits

- [BepInEx](https://github.com/BepInEx/BepInEx) / [HarmonyX](https://github.com/BepInEx/HarmonyX)
- [Tailwind CSS](https://github.com/tailwindlabs/tailwindcss)
- [Newtonsoft.Json](https://www.newtonsoft.com/json)
- [Casualties: Unknown](https://store.steampowered.com/app/4576490/)

## License

CU.RemoteConsole is licensed under the [MIT License](./LICENSE).

See [THIRD-PARTY-NOTICES.md](./THIRD-PARTY-NOTICES.md) for dependency and third-party content notes. Do not copy, modify, bundle, or redistribute third-party Casualties: Unknown mods, Dev Menu code, resources, UI, game files, or BepInEx binaries unless the relevant license explicitly permits it.
