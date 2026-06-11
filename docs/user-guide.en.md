# CU.RemoteConsole User Guide

## What It Does

CU.RemoteConsole provides a local browser command panel for Casualties: Unknown. It accepts authenticated command requests, queues them, and executes allowed commands from the Unity main thread through the game's existing console executor.

## Requirements

- Casualties: Unknown or Casualties: Unknown Demo
- BepInEx 5.4.x installed separately
- CU.RemoteConsole release package
- A browser on the same machine

## Install

1. Install BepInEx 5.4.x into the game folder.
2. Extract `CU.RemoteConsole-v0.0.12.zip`.
3. Copy the extracted `BepInEx` folder into the game folder.
4. Start the game once.
5. Open `http://127.0.0.1:8848/`.
6. Copy the bearer token from `BepInEx/config/cu.remoteconsole.cfg` into the web console.

Do not share the token.

## Web Console

The web console includes:

- token input stored in browser session storage
- command input
- command output blocks
- recent command history
- command catalog grouped by risk
- read-only status panel
- language selector

Safe commands can be clicked from the command catalog. State-changing and dangerous commands are visible but disabled by default.

## API Quick Start

Submit a command:

```bash
curl -H 'Authorization: Bearer <token>' \
  -H 'Content-Type: application/json' \
  -d '{"command":"help"}' \
  http://127.0.0.1:8848/api/commands
```

Query a receipt:

```bash
curl -H 'Authorization: Bearer <token>' \
  http://127.0.0.1:8848/api/commands/<queueId>
```

Get recent records:

```bash
curl -H 'Authorization: Bearer <token>' \
  http://127.0.0.1:8848/api/commands
```

OpenAPI contract:

```text
docs/api/openapi.yaml
```

## Security Notes

- The service listens on `127.0.0.1` by default.
- Authentication is required by default.
- Dangerous commands are denied by default.
- Commands are queued and executed from the Unity main thread.
- The token is not printed to logs.
- Do not port-forward this service to the public internet.

For remote access, use a private network or tunnel such as Tailscale, WireGuard, SSH tunnel, or ZeroTier.

## Troubleshooting

If the page does not open:

- confirm the game is running
- confirm BepInEx loaded the plugin
- check `BepInEx/LogOutput.log`
- confirm port `8848` is not blocked or already used

Proton / Steam Deck note:
If BepInEx does not load when running the Windows build through Proton, add this Steam launch option:

```text
WINEDLLOVERRIDES=winhttp=n,b %command%
```

This is not needed for normal Windows installs.

If authentication fails:

- read the token from `BepInEx/config/cu.remoteconsole.cfg`
- do not include extra spaces
- do not put the token in a URL query string

If a command is denied:

- check the command catalog
- unknown, state-changing, and dangerous commands are denied by default

## Uninstall

Remove:

```text
BepInEx/plugins/CU.RemoteConsole/
```

Optional cleanup:

```text
BepInEx/config/cu.remoteconsole.cfg
BepInEx/config/cu.remoteconsole.audit.log
```
