#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

SKIP_BUILD=false
if [[ "${1:-}" == "--skip-build" ]]; then
  SKIP_BUILD=true
fi

if [[ "$SKIP_BUILD" == false ]]; then
  "$ROOT/scripts/build-plugin.sh"
fi

VERSION="$(
  sed -n 's/.*PluginVersion = "\([^"]*\)".*/\1/p' "$ROOT/src/CU.RemoteConsole/RemoteConsolePlugin.cs" | tail -1
)"

if [[ -z "$VERSION" ]]; then
  echo "Could not determine plugin version." >&2
  exit 1
fi

SOURCE_DLL="$ROOT/src/CU.RemoteConsole/bin/Release/CU.RemoteConsole.dll"
if [[ ! -f "$SOURCE_DLL" ]]; then
  echo "Build output not found: $SOURCE_DLL" >&2
  exit 1
fi

DIST_ROOT="$ROOT/dist"
PACKAGE_NAME="CU.RemoteConsole-v$VERSION"
PACKAGE_DIR="$DIST_ROOT/$PACKAGE_NAME"
PLUGIN_DIR="$PACKAGE_DIR/BepInEx/plugins/CU.RemoteConsole"
ZIP_PATH="$DIST_ROOT/$PACKAGE_NAME.zip"

rm -rf "$PACKAGE_DIR" "$ZIP_PATH"
mkdir -p "$PLUGIN_DIR"

install -m 0644 "$SOURCE_DLL" "$PLUGIN_DIR/CU.RemoteConsole.dll"
printf '%s\n' "$VERSION" > "$PACKAGE_DIR/VERSION"

cat > "$PACKAGE_DIR/README-INSTALL.txt" <<EOF
CU.RemoteConsole v$VERSION

English
=======

Target:
- Casualties: Unknown / Casualties: Unknown Demo
- BepInEx 5.4.x is required and must be installed separately.

Install:
1. Install BepInEx for the game if it is not already installed.
2. Copy this package's BepInEx folder into the game install directory.
3. Start the game once.
4. Open:
   http://127.0.0.1:8848/
5. Find the generated bearer token in:
   BepInEx/config/cu.remoteconsole.cfg

Safety defaults:
- Listens on 127.0.0.1 by default.
- Requires bearer-token authentication by default.
- Dangerous commands are denied by default.
- Remote commands are queued and consumed from the Unity main thread.

This release package does not include:
- BepInEx binaries
- game files
- third-party mod files
- token/config files
- node_modules
- build intermediate files

Do not expose this service directly to the public internet.
Use a private tunnel/VPN such as Tailscale, WireGuard, SSH tunnel, or ZeroTier if remote access is needed.

Proton / Steam Deck note:
If BepInEx does not load when running the Windows build through Proton, add this Steam launch option:
WINEDLLOVERRIDES=winhttp=n,b %command%
This is not needed for normal Windows installs.

中文
====

目标：
- Casualties: Unknown / 未知伤亡 / Casualties: Unknown Demo
- 需要单独安装 BepInEx 5.4.x。

安装：
1. 如果游戏还没有安装 BepInEx，请先安装 BepInEx。
2. 把本发布包里的 BepInEx 文件夹复制到游戏安装目录。
3. 启动一次游戏。
4. 打开：
   http://127.0.0.1:8848/
5. 在以下文件中找到自动生成的 bearer token：
   BepInEx/config/cu.remoteconsole.cfg

安全默认值：
- 默认监听 127.0.0.1。
- 默认需要 bearer token 鉴权。
- 危险命令默认拒绝。
- 远程命令进入队列，并由 Unity 主线程消费。

本发布包不包含：
- BepInEx 二进制文件
- 游戏文件
- 第三方 mod 文件
- token/config 文件
- node_modules
- 构建中间产物

不要把这个服务直接暴露到公网。
如果需要远程访问，请使用 Tailscale、WireGuard、SSH tunnel 或 ZeroTier 等私有网络/VPN 方案。

Proton / Steam Deck 备注：
如果通过 Proton 运行 Windows 版游戏时 BepInEx 没有加载，可以添加这个 Steam 启动选项：
WINEDLLOVERRIDES=winhttp=n,b %command%
普通 Windows 安装不需要这个选项。
EOF

(
  cd "$PACKAGE_DIR"
  find . -type f ! -name CHECKSUMS.txt -print0 \
    | sort -z \
    | xargs -0 sha256sum \
    > CHECKSUMS.txt
)

(
  cd "$DIST_ROOT"
  zip -qr "$ZIP_PATH" "$PACKAGE_NAME"
)

echo "Created $PACKAGE_DIR"
echo "Created $ZIP_PATH"
