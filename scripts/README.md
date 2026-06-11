# Scripts

- `build-plugin.sh`: installs locked npm dependencies, builds the local Tailwind web console, and builds the Release plugin DLL.
- `install-local.sh`: builds and installs the plugin DLL into the local Casualties: Unknown BepInEx plugin directory. Use `--skip-build` to install an already-built DLL.
- `package-release.sh`: builds and creates a redistributable release folder and zip under `dist/`. Use `--skip-build` to package an already-built DLL.
- `smoke-test-local.sh`: validates a running local game/plugin instance without printing the bearer token.
- `test-logic.sh`: runs the small pure-logic test project. It does not require the game to be running.
- `read-token.sh`: internal helper for scripts. It must be sourced and does not print tokens when executed directly.

Environment overrides:

- `GAME_DIR` or `CU_GAME_DIR`: target Casualties: Unknown install directory.
- `CU_REMOTE_CONSOLE_URL`: local service URL for smoke tests.
- `CU_REMOTE_CONSOLE_CONFIG`: explicit config path for token reading.
