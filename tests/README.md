# Tests

This folder intentionally contains only small pure-logic tests.

Current scope:

- command policy allow/deny behavior
- command catalog policy metadata
- bearer token validation
- rate limiting
- bounded command queue behavior

Out of scope:

- Unity runtime tests
- BepInEx startup tests
- browser DOM/UI tests
- live game automation

Use:

```bash
scripts/test-logic.sh
```
