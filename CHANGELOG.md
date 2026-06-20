# Changelog

## 1.1.0 - 2026-06-12

### Added

- Added a browser-side connection endpoint field for connecting the web console to a custom local, LAN, or private tunnel API address.
- Added restricted CORS preflight support for accepted localhost browser origins.

### Changed

- Restyled the web console with a darker, denser command-panel layout.

## 1.0.0 - 2026-06-12

Initial stable source release for CU.RemoteConsole.

### Added

- Local authenticated browser/API command console for Casualties: Unknown.
- BepInEx 5 plugin entrypoint with Unity main-thread command dispatch.
- Thread-safe bounded command queue, command receipts, recent history, and captured output rendering.
- Local HTTP API for health, status, command submission, command history, command lookup, and command catalog.
- Web console with English/Chinese UI, status panel, command catalog, receipt lookup, recent history, and rendered command output.
- In-game `F8` config overlay with English/Chinese toggle.
- Runtime-editable local config for network, authentication, command policy, command allow-list, rate limits, queue limits, and audit logging.
- Conservative defaults: localhost binding, authentication enabled, LAN/public exposure disabled, state-changing commands disabled, dangerous commands denied.
- Static OpenAPI contract in `docs/api/openapi.yaml`.
- English and Chinese README/user guide documentation.
- MIT license and third-party notices.

### Notes

- BepInEx is not bundled and must be installed separately.
## 1.2.0 - 2026-06-20

### Added

- Vercel-style redesigned web UI with dark zinc theme and compact layout.
- Sidebar tab system: Status, Commands, History, and Manual panels.
- Command manual tab with localized English/Chinese reference and search.
- Command snippet save and quick-execute feature (localStorage, up to 30 items).
- Command history arrow-key navigation (↑/↓).
- Catalog hover tooltip and click-to-expand command descriptions.
- Command descriptions from the original game manual embedded in the backend API.
- Chinese localization for frontend UI and command reference data.

### Changed

- Output truncation limits raised: per-line 1000→50000 chars, total 500000 chars max.
- Bridge status indicators: "not_started" and "bridge_ready" now show as green (ok) instead of yellow (warn).
- Health endpoint response slimmed down to essential fields only.
- Token fingerprint now strips the "Bearer " prefix before hashing.

### Fixed

- CommandResponse.Complete() now respects capacity eviction.
- ConsoleBridge retries reflection lookup on failure with 30-second backoff.
- InGameConfigOverlay pre-validates network policy before writing config values.
- All JS event listeners wrapped in null-safety checks.

## 1.1.0 - 2026-06-12
