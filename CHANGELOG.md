# Changelog

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
