# CU.RemoteConsole

[English](./README.md) | 简体中文

面向 [Casualties: Unknown](https://store.steampowered.com/app/4576490/) 的 BepInEx 5 模组，提供本地带鉴权的浏览器/API 命令控制台。

> [!WARNING]
> CU.RemoteConsole 可以执行游戏控制台命令。请保持监听 `127.0.0.1`，保持鉴权开启，不要分享自动生成的 bearer token，也不要把服务直接暴露到公网。

## 功能

- 本地网页控制台：`http://127.0.0.1:8848/`。
- Bearer token 鉴权。
- 安全命令白名单，危险命令默认拒绝。
- 基础速率限制和命令审计日志。
- 线程安全命令队列，由 Unity 主线程消费。
- 命令回执查询、最近历史和命令输出渲染。
- 只读状态、配置和策略面板。
- 按风险分组的命令目录。
- 中英文网页 UI。
- 静态 OpenAPI 契约：[`docs/api/openapi.yaml`](./docs/api/openapi.yaml)。

## 工作方式

CU.RemoteConsole 不会在 HTTP/后台线程里调用游戏对象。

```text
浏览器 / 本地工具
  -> localhost HTTP API
  -> 鉴权 + Origin 检查 + 命令策略 + 审计
  -> 有界命令队列
  -> Unity 主线程消费
  -> ConsoleBridge
  -> 游戏控制台执行器
```

## 环境要求

| 项目 | 要求 |
| --- | --- |
| 游戏 | Casualties: Unknown / Casualties: Unknown Demo |
| Mod 加载器 | BepInEx 5.4.x |
| 运行时 | Unity Mono / `netstandard2.1` 插件构建 |
| 浏览器 | 同一台机器上的现代浏览器 |

Casualties: Unknown 可能支持部分自定义内容，但 CU.RemoteConsole 是 C# 插件，需要通过 BepInEx 加载。

## 安装

1. 为游戏安装 BepInEx 5.4.x。
2. 从 Release 页面下载 `CU.RemoteConsole-v0.0.12.zip`。
3. 把发布包里的整个 `BepInEx` 文件夹复制到游戏安装目录。
4. 确认最终插件路径类似：

```text
<GameDir>\BepInEx\plugins\CU.RemoteConsole\CU.RemoteConsole.dll
```

5. 启动一次游戏。
6. 打开：

```text
http://127.0.0.1:8848/
```

7. 从以下文件复制自动生成的 bearer token：

```text
<GameDir>\BepInEx\config\cu.remoteconsole.cfg
```

不要分享或提交这个 token。

Proton / Steam Deck 备注：
如果通过 Proton 运行 Windows 版游戏时 BepInEx 没有加载，可以添加这个 Steam 启动选项：

```text
WINEDLLOVERRIDES=winhttp=n,b %command%
```

普通 Windows 安装不需要这个选项。

## 快速开始

1. 启动游戏并进入场景。
2. 打开 `http://127.0.0.1:8848/`。
3. 粘贴配置文件里的 bearer token。
4. 执行 `help`。
5. 查看输出块和最近历史。
6. 用命令目录查看哪些命令允许或拒绝。

## 网页控制台

| 区域 | 用途 |
| --- | --- |
| Command | 提交命令并渲染输出块 |
| Receipt lookup | 按 id 查询单条命令回执 |
| Recent history | 查看最近命令回执 |
| Catalog | 展示安全、会修改状态、危险、未知命令的策略 |
| Status | 展示监听器、鉴权、队列、速率限制、补丁和策略的只读状态 |

会修改状态的命令和危险命令会展示出来，但默认拒绝执行。

## API

默认本地服务：

```text
http://127.0.0.1:8848
```

端点：

| 方法 | 路径 | 用途 |
| --- | --- | --- |
| `GET` | `/health` | 无鉴权健康检查 |
| `GET` | `/api/status` | 只读运行状态 |
| `POST` | `/api/commands` | 提交命令 |
| `GET` | `/api/commands` | 最近命令回执 |
| `GET` | `/api/commands/catalog` | 命令策略目录 |
| `GET` | `/api/commands/{queueId}` | 查询命令回执 |

提交命令：

```bash
curl -H 'Authorization: Bearer <token>' \
  -H 'Content-Type: application/json' \
  -d '{"command":"help"}' \
  http://127.0.0.1:8848/api/commands
```

静态 OpenAPI 契约：

```text
docs/api/openapi.yaml
```

## 配置

BepInEx 会在首次启动后自动生成配置文件。

| 文件 | 用途 |
| --- | --- |
| `<GameDir>\BepInEx\config\cu.remoteconsole.cfg` | 监听、鉴权、策略、队列、速率限制和审计设置 |
| `<GameDir>\BepInEx\config\cu.remoteconsole.audit.log` | 命令审计日志 |

重要默认值：

| 配置项 | 默认值 |
| --- | --- |
| `Network/BindAddress` | `127.0.0.1` |
| `Network/Port` | `8848` |
| `Security/RequireAuth` | `true` |
| `Security/AllowLan` | `false` |
| `Security/AllowPublic` | `false` |
| `Security/DenyDangerousCommands` | `true` |

## 从源码构建

构建前，先把 `GAME_DIR` 或 `CU_GAME_DIR` 设为你本机的 Casualties: Unknown 安装目录。

```bash
npm install
npm run build:web
dotnet build src/CU.RemoteConsole/CU.RemoteConsole.csproj -c Release
```

常用脚本：

| 脚本 | 用途 |
| --- | --- |
| `scripts/build-plugin.sh` | 构建网页资源和 Release 插件 DLL |
| `scripts/install-local.sh` | 把构建好的 DLL 安装到本地游戏目录 |
| `scripts/smoke-test-local.sh` | 验证正在运行的本地游戏/插件实例 |
| `scripts/test-logic.sh` | 运行小型纯逻辑测试 |
| `scripts/package-release.sh` | 在 `dist/` 下生成本地发布包 |

`scripts/smoke-test-local.sh` 会在脚本内部读取 token，但不会打印 token。

## 发布包内容

发布包包含：

```text
BepInEx/plugins/CU.RemoteConsole/CU.RemoteConsole.dll
README-INSTALL.txt
VERSION
CHECKSUMS.txt
```

发布包不包含 BepInEx、游戏文件、第三方 mod、token/config 文件、`node_modules`、源码、测试或构建中间产物。

## 鸣谢

- [BepInEx](https://github.com/BepInEx/BepInEx) / [HarmonyX](https://github.com/BepInEx/HarmonyX)
- [Tailwind CSS](https://github.com/tailwindlabs/tailwindcss)
- [Newtonsoft.Json](https://www.newtonsoft.com/json)
- [Casualties: Unknown](https://store.steampowered.com/app/4576490/)

## 许可证

CU.RemoteConsole 使用 [MIT License](./LICENSE) 授权。

依赖和第三方内容说明见 [THIRD-PARTY-NOTICES.md](./THIRD-PARTY-NOTICES.md)。除非相关许可证明确允许，不要复制、修改、打包或二次分发第三方 Casualties: Unknown mod、Dev Menu 代码、资源、UI、游戏文件或 BepInEx 二进制文件。
