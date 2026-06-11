# Security Design

Status: current security design for release `1.0.0`.

## Security Posture

The remote console is a high-privilege local administration surface. The safe default is local-only, authenticated, audited, and fail-closed.

## Defaults

| Setting | Default |
|---|---|
| Bind address | `127.0.0.1` |
| Port | `8848` |
| Require auth | `true` |
| Allow LAN | `false` |
| Allow public | `false` |
| Deny dangerous commands | `true` |
| Rate limit | enabled |
| Audit log | enabled |

## Authentication

- Use bearer token or equivalent secret passed in an HTTP header.
- Do not hard-code tokens.
- Do not accept tokens in query strings.
- Store token only in config/secret file or user-provided environment/config mechanism.
- Log only token fingerprint/id, never the full token.

Current implementation generates the bearer token into `BepInEx/config/cu.remoteconsole.cfg` when empty and does not print it to BepInEx logs.

## Browser Protection

- Reject state-changing requests without valid auth.
- Reject unexpected `Origin` for browser endpoints.
- Do not enable wildcard CORS.
- Require JSON content type for command submissions.
- Do not use cookies as the only auth mechanism.
- Do not make state-changing endpoints available via `GET`.

## Input Validation

- Cap command length.
- Cap request body size.
- Reject blank commands.
- Normalize line endings.
- Reject multi-command payloads unless explicitly designed.
- Preserve raw command only where needed for audit and execution.

## Command Policy

Use allowlist-first policy.

| Risk | Description | Default |
|---|---|---|
| `safe` | Read-only or diagnostic commands such as `help`, `log`, `clear`, `copylog`, `framerate` | allowed |
| `state-changing` | Commands that alter run/game state such as `heal`, `spawn`, `tp`, `addxp`, `timescale`, `starterkit`, `fullbright`, `noclip`, `freecam` | denied unless enabled |
| `dangerous` | Commands such as `kill`, `saveandquit`, `nukeplayerprefs`, `openfolder`, field mutation, amputation, custom-command mutation | denied by default |
| `unknown` | Any unclassified command | denied by default in MVP |

Current Steam demo build `23560597` confirms many of these names exist. Additional observed commands that should be denied by default until classified include `skiplayer`, `resetskills`, `fucklore`, `setconsoleheight`, `setconsolecolor`, `loglocale`, `unchipped`, `addliquid`, `locate`, `music`, `bind`, `repeat`, `explode`, `floodfill`, `echo`, `ui`, `playsound`, `plushies`, and `errorlogging`. `volume` was not observed in this build.

## Rate Limiting

- Limit by remote endpoint and token fingerprint.
- Limit failed auth attempts.
- Limit queue submissions.
- Return stable errors without revealing sensitive policy internals.

## Audit Log

Each event should include:

- timestamp
- event type
- remote address
- token fingerprint
- command id
- command classification
- decision
- queue id
- execution status
- error category

Do not include raw tokens or local private paths unless explicitly required and redacted.

Current implementation writes audit events to `BepInEx/config/cu.remoteconsole.audit.log` with token fingerprints, not raw tokens.

## Network Exposure

Remote access should use private network/tunnel tools:

- Tailscale
- WireGuard
- SSH tunnel
- ZeroTier

Do not recommend public port forwarding for MVP.
