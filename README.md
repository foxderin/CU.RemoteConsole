# CU.RemoteConsole

**English** | [中文](#中文)

CU.RemoteConsole is a local-first remote command panel for **Casualties: Unknown / 未知伤亡**.

It lets a browser or external tool submit game console commands to a local BepInEx plugin. The plugin authenticates, validates, audits, queues the command, and executes game-facing calls only from the Unity main thread through the `ConsoleBridge` boundary.

## Current Version

`0.0.12`

Validated target:

- Casualties: Unknown Demo, Steam app id `4576510`
- Steam build id `23560597`
- Unity `2022.3.62f3`
- BepInEx `5.4.23.5`

## Features

- Local web console at `http://127.0.0.1:8848/`
- Bearer-token authentication
- Safe command allowlist and dangerous-command denylist
- Basic rate limiting
- Command audit log
- Thread-safe command queue
- Unity main-thread execution via `ConsoleBridge`
- Command receipt lookup and recent history
- Captured command output rendering
- Read-only status/config/policy panel
- Command catalog grouped by risk
- English/Chinese web UI
- Static OpenAPI contract at [docs/api/openapi.yaml](docs/api/openapi.yaml)

## Safety Defaults

- Bind address: `127.0.0.1`
- Port: `8848`
- Authentication: required
- LAN access: disabled unless explicitly configured
- Public internet exposure: not supported by default
- Dangerous commands: denied by default
- Remote commands: queued and consumed on the Unity main thread

Do not expose this service directly to the public internet. Use a private tunnel or VPN such as Tailscale, WireGuard, SSH tunnel, or ZeroTier if remote access is needed.

## Install From Release Package

1. Install BepInEx 5.4.x for the game.
2. Download `CU.RemoteConsole-v0.0.12.zip` from the project release page.
3. Copy the package's `BepInEx` folder into the game install directory.
4. Start the game.
5. Open `http://127.0.0.1:8848/`.
6. Find the generated bearer token in `BepInEx/config/cu.remoteconsole.cfg`.

Do not share or commit the token.

Proton / Steam Deck note:
If BepInEx does not load when running the Windows build through Proton, add this Steam launch option:

```text
WINEDLLOVERRIDES=winhttp=n,b %command%
```

This is not needed for normal Windows installs.

## Build From Source

Set `GAME_DIR` or `CU_GAME_DIR` to your local Casualties: Unknown install directory before building the plugin.

```bash
npm install
npm run build:web
dotnet build src/CU.RemoteConsole/CU.RemoteConsole.csproj -c Release
```

Useful scripts:

```bash
scripts/build-plugin.sh
scripts/install-local.sh
scripts/smoke-test-local.sh
scripts/test-logic.sh
scripts/package-release.sh
```

`scripts/smoke-test-local.sh` reads the configured token internally and does not print it.

## Release Package

Create a local release package:

```bash
scripts/package-release.sh
```

Output:

```text
dist/CU.RemoteConsole-v0.0.12/
├─ BepInEx/plugins/CU.RemoteConsole/CU.RemoteConsole.dll
├─ README-INSTALL.txt
├─ VERSION
└─ CHECKSUMS.txt
```

The release package does not include BepInEx, game files, third-party mods, token/config files, `node_modules`, or build intermediates.

## API

Default server:

```text
http://127.0.0.1:8848
```

Endpoints:

- `GET /health`
- `GET /api/status`
- `POST /api/commands`
- `GET /api/commands`
- `GET /api/commands/catalog`
- `GET /api/commands/{queueId}`

Example:

```bash
curl -H 'Authorization: Bearer <token>' \
  -H 'Content-Type: application/json' \
  -d '{"command":"help"}' \
  http://127.0.0.1:8848/api/commands
```

OpenAPI:

```text
docs/api/openapi.yaml
```

The OpenAPI file is static documentation only. The plugin does not host Swagger UI and does not dynamically generate OpenAPI at runtime.

## Documentation

- [English user guide](docs/user-guide.en.md)
- [中文用户指南](docs/user-guide.zh-CN.md)
- [Security design](docs/security.md)

## Repository Contents

This source release repository intentionally includes only:

- plugin source under `src/`
- embedded web console source under `web/src/`
- minimal build, install, smoke-test, logic-test, and package scripts under `scripts/`
- small pure-logic tests under `tests/`
- user-facing docs and OpenAPI under `docs/`

It intentionally excludes development research notes, reverse-engineering scratch output, local game files, BepInEx binaries, generated release archives, `node_modules`, `bin/obj`, tokens, local config, and logs.

## License And Third-Party Content

No project license has been selected yet. Do not copy, modify, bundle, or redistribute third-party Casualties: Unknown mods, Dev Menu code, resources, UI, game files, or BepInEx binaries unless the relevant license explicitly permits it.

---

## 中文

CU.RemoteConsole 是面向 **Casualties: Unknown / 未知伤亡** 的本地优先远程命令面板。

它允许浏览器或外部工具把游戏控制台命令提交给本地 BepInEx 插件。插件会完成鉴权、校验、审计、排队，并且只在 Unity 主线程通过 `ConsoleBridge` 边界调用游戏侧命令执行器。

## 当前版本

`0.0.12`

已验证目标：

- Casualties: Unknown Demo，Steam app id `4576510`
- Steam build id `23560597`
- Unity `2022.3.62f3`
- BepInEx `5.4.23.5`

## 功能

- 本地网页控制台：`http://127.0.0.1:8848/`
- Bearer token 鉴权
- 安全命令白名单和危险命令默认拒绝
- 基础速率限制
- 命令审计日志
- 线程安全命令队列
- 通过 `ConsoleBridge` 在 Unity 主线程执行
- 命令回执查询和最近历史
- 命令输出捕获与渲染
- 只读状态、配置和策略面板
- 按风险分组的命令目录
- 中英文网页 UI
- 静态 OpenAPI 契约：[docs/api/openapi.yaml](docs/api/openapi.yaml)

## 安全默认值

- 监听地址：`127.0.0.1`
- 端口：`8848`
- 鉴权：默认开启
- 局域网访问：除非显式配置，否则关闭
- 公网暴露：默认不支持
- 危险命令：默认拒绝
- 远程命令：进入队列，并由 Unity 主线程消费

不要把这个服务直接暴露到公网。如果需要远程访问，优先使用 Tailscale、WireGuard、SSH tunnel 或 ZeroTier 等私有网络方案。

## 从发布包安装

1. 为游戏安装 BepInEx 5.4.x。
2. 从项目 Release 页面下载 `CU.RemoteConsole-v0.0.12.zip`。
3. 把发布包里的 `BepInEx` 文件夹复制到游戏安装目录。
4. 启动游戏。
5. 打开 `http://127.0.0.1:8848/`。
6. 在 `BepInEx/config/cu.remoteconsole.cfg` 中找到自动生成的 bearer token。

不要分享或提交这个 token。

Proton / Steam Deck 备注：
如果通过 Proton 运行 Windows 版游戏时 BepInEx 没有加载，可以添加这个 Steam 启动选项：

```text
WINEDLLOVERRIDES=winhttp=n,b %command%
```

普通 Windows 安装不需要这个选项。

## 从源码构建

构建插件前，先把 `GAME_DIR` 或 `CU_GAME_DIR` 设为你本机的 Casualties: Unknown 安装目录。

```bash
npm install
npm run build:web
dotnet build src/CU.RemoteConsole/CU.RemoteConsole.csproj -c Release
```

常用脚本：

```bash
scripts/build-plugin.sh
scripts/install-local.sh
scripts/smoke-test-local.sh
scripts/test-logic.sh
scripts/package-release.sh
```

`scripts/smoke-test-local.sh` 会在脚本内部读取 token，但不会打印 token。

## 发布包

生成本地发布包：

```bash
scripts/package-release.sh
```

输出：

```text
dist/CU.RemoteConsole-v0.0.12/
├─ BepInEx/plugins/CU.RemoteConsole/CU.RemoteConsole.dll
├─ README-INSTALL.txt
├─ VERSION
└─ CHECKSUMS.txt
```

发布包不包含 BepInEx、游戏文件、第三方 mod、token/config 文件、`node_modules` 或构建中间产物。

## API

默认服务：

```text
http://127.0.0.1:8848
```

端点：

- `GET /health`
- `GET /api/status`
- `POST /api/commands`
- `GET /api/commands`
- `GET /api/commands/catalog`
- `GET /api/commands/{queueId}`

示例：

```bash
curl -H 'Authorization: Bearer <token>' \
  -H 'Content-Type: application/json' \
  -d '{"command":"help"}' \
  http://127.0.0.1:8848/api/commands
```

OpenAPI：

```text
docs/api/openapi.yaml
```

OpenAPI 文件仅作为静态文档。插件不会托管 Swagger UI，也不会在运行时动态生成 OpenAPI。

## 文档

- [English user guide](docs/user-guide.en.md)
- [中文用户指南](docs/user-guide.zh-CN.md)
- [安全设计](docs/security.md)

## 仓库内容

这个源码发布仓库只包含：

- `src/` 下的插件源码
- `web/src/` 下的嵌入式网页控制台源码
- `scripts/` 下的最小构建、安装、冒烟测试、逻辑测试和打包脚本
- `tests/` 下的小型纯逻辑测试
- `docs/` 下的用户文档和 OpenAPI

它刻意不包含开发调研笔记、逆向临时输出、本地游戏文件、BepInEx 二进制文件、生成的发布压缩包、`node_modules`、`bin/obj`、token、本地配置和日志。

## 许可证和第三方内容

本项目尚未选择许可证。除非相关许可证明确允许，不要复制、修改、打包或二次分发第三方 Casualties: Unknown mod、Dev Menu 代码、资源、UI、游戏文件或 BepInEx 二进制文件。
