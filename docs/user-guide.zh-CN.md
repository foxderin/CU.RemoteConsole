# CU.RemoteConsole 用户指南

## 它能做什么

CU.RemoteConsole 为 Casualties: Unknown 提供本地浏览器命令面板。它接收带鉴权的命令请求，将命令放入队列，并通过游戏已有控制台执行器在 Unity 主线程执行被允许的命令。

## 需求

- Casualties: Unknown 或 Casualties: Unknown Demo
- 单独安装 BepInEx 5.4.x
- CU.RemoteConsole 发布包
- 同一台机器上的浏览器

## 安装

1. 把 BepInEx 5.4.x 安装到游戏目录。
2. 解压 `CU.RemoteConsole-v1.0.0.zip`。
3. 把解压后的 `BepInEx` 文件夹复制到游戏目录。
4. 启动一次游戏。
5. 打开 `http://127.0.0.1:8848/`。
6. 从 `BepInEx/config/cu.remoteconsole.cfg` 复制 bearer token 到网页控制台。

不要分享这个 token。

## 网页控制台

网页控制台包含：

- token 输入框，保存在浏览器 session storage
- 命令输入框
- 分块显示的命令输出
- 最近命令历史
- 按风险分组的命令目录
- 只读状态面板
- 语言选择

## 游戏内配置

在游戏内按 `F8` 可以打开 CU.RemoteConsole 配置窗口。窗口默认跟随系统语言，并提供中英文切换。

本地游戏内窗口可以修改网络、鉴权、命令策略、命令允许列表、限制和审计设置。公网/局域网暴露、关闭鉴权、允许状态修改/危险命令、添加额外允许命令等高风险修改需要再次点击确认保存。

远程 API 用户不能通过 HTTP 修改配置。

网页命令目录会在重新加载目录或点击 Refresh 后反映当前策略。

安全命令可以从命令目录点击填入。会修改状态的命令和危险命令默认只展示，不可直接点击执行。

## API 快速开始

提交命令：

```bash
curl -H 'Authorization: Bearer <token>' \
  -H 'Content-Type: application/json' \
  -d '{"command":"help"}' \
  http://127.0.0.1:8848/api/commands
```

查询回执：

```bash
curl -H 'Authorization: Bearer <token>' \
  http://127.0.0.1:8848/api/commands/<queueId>
```

获取最近记录：

```bash
curl -H 'Authorization: Bearer <token>' \
  http://127.0.0.1:8848/api/commands
```

OpenAPI 契约：

```text
docs/api/openapi.yaml
```

## 安全说明

- 服务默认只监听 `127.0.0.1`。
- 默认必须鉴权。
- 危险命令默认拒绝。
- 命令进入队列，并由 Unity 主线程执行。
- token 不会打印到日志。
- 不要把服务端口直接转发到公网。

如果需要远程访问，使用 Tailscale、WireGuard、SSH tunnel 或 ZeroTier 等私有网络方案。

## 排障

如果页面打不开：

- 确认游戏正在运行
- 确认 BepInEx 已加载插件
- 查看 `BepInEx/LogOutput.log`
- 确认 `8848` 端口没有被占用或阻止

Proton / Steam Deck 备注：
如果通过 Proton 运行 Windows 版游戏时 BepInEx 没有加载，可以添加这个 Steam 启动选项：

```text
WINEDLLOVERRIDES=winhttp=n,b %command%
```

普通 Windows 安装不需要这个选项。

如果鉴权失败：

- 从 `BepInEx/config/cu.remoteconsole.cfg` 读取 token
- 不要带多余空格
- 不要把 token 放在 URL 查询参数里

如果命令被拒绝：

- 查看命令目录
- 未知命令、会修改状态的命令、危险命令默认拒绝

## 卸载

删除：

```text
BepInEx/plugins/CU.RemoteConsole/
```

可选清理：

```text
BepInEx/config/cu.remoteconsole.cfg
BepInEx/config/cu.remoteconsole.audit.log
```
