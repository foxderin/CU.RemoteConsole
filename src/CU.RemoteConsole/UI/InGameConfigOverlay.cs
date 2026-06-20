using System;
using System.Net;
using CU.RemoteConsole.Config;
using UnityEngine;

namespace CU.RemoteConsole.UI;

public sealed class InGameConfigOverlay : MonoBehaviour
{
    private RemoteConsoleHost? host;
    private Rect windowRect = new Rect(80, 80, 520, 800);
    private Vector2 scroll;
    private bool visible;
    private bool? forceChinese;
    private bool confirmDangerousSave;
    private bool regenerateToken;
    private string status = "Ready";

    private string bindAddress = "0.0.0.0";
    private string port = "8848";
    private bool requireAuth = true;
    private bool allowLan;
    private bool allowPublic;
    private bool allowStateChangingCommands;
    private bool denyDangerousCommands = true;
    private string extraAllowedCommands = string.Empty;
    private string maxCommandLength = "256";
    private string maxQueueDepth = "64";
    private string maxCommandsPerSecond = "2";
    private string maxCommandsPerFrame = "1";
    private bool auditLogEnabled = true;

    public static InGameConfigOverlay Create(RemoteConsoleHost host)
    {
        var gameObject = new GameObject("CU.RemoteConsole.ConfigOverlay");
        DontDestroyOnLoad(gameObject);
        var overlay = gameObject.AddComponent<InGameConfigOverlay>();
        overlay.host = host;
        overlay.ResetFromConfig();
        return overlay;
    }

    public void Toggle()
    {
        visible = !visible;
        confirmDangerousSave = false;
        if (visible)
        {
            ResetFromConfig();
        }
    }

    private bool mainMenuButtonInjected;
    private float nextInjectionCheck;

    private void Update()
    {
        if (mainMenuButtonInjected) return;
        if (host == null) return;
        // Throttle to once per second
        if (Time.unscaledTime < nextInjectionCheck) return;
        nextInjectionCheck = Time.unscaledTime + 1f;

        try
        {
            if (GameObject.Find("CU_WebConsoleBtn") != null)
            {
                mainMenuButtonInjected = true;
                host.Log("[CU.RemoteConsole] Main menu button already exists.");
                return;
            }
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                host.Log("[CU.RemoteConsole] Canvas not found yet, retrying...");
                return;
            }

            // Create button from scratch so no external mod dependency
            var btnObj = new GameObject("CU_WebConsoleBtn", typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.Image));
            btnObj.transform.SetParent(canvas.transform, false);

            // Add text child
            var textObj = new GameObject("Text", typeof(UnityEngine.UI.Text));
            textObj.transform.SetParent(btnObj.transform, false);
            var text = textObj.GetComponent<UnityEngine.UI.Text>();
            text.text = "Web Console";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            // Style the button
            var btn = btnObj.GetComponent<UnityEngine.UI.Button>();
            var img = btnObj.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Position at bottom-right corner
            var rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-10, 10);

            // Style text
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.sizeDelta = rt.sizeDelta;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            // Wire click — open in-game config overlay
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                try { host.ToggleConfigOverlay(); }
                catch (System.Exception ex) {
                    host.Log("[CU.RemoteConsole] Toggle overlay error: " + ex.Message);
                }
            });

            mainMenuButtonInjected = true;
            host.Log("[CU.RemoteConsole] Web Console main menu button injected.");
        }
        catch (System.Exception ex)
        {
            host.Log("[CU.RemoteConsole] Injection error: " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    private void OnGUI()
    {
        if (!visible || host == null)
        {
            return;
        }

        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "CU.RemoteConsole Config");
    }

    private void DrawWindow(int id)
    {
        if (host == null)
        {
            return;
        }

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(500), GUILayout.Height(610));

        GUILayout.BeginHorizontal();
        GUILayout.Label(T("Local in-game configuration", "本地游戏内配置"));
        if (GUILayout.Button(IsChinese() ? "English" : "中文", GUILayout.Width(90)))
        {
            forceChinese = !IsChinese();
        }
        GUILayout.EndHorizontal();
        GUILayout.Label(T("Remote API remains authenticated and policy-gated. This window is for the local player.", "远程 API 仍然需要鉴权并受策略限制。这个窗口面向本地玩家。"));
        GUILayout.Space(8);

        GUILayout.Label(T("Network", "网络"));
        bindAddress = TextRow(T("Bind address", "监听地址"), bindAddress);
        port = TextRow(T("Port", "端口"), port);
        allowLan = ToggleRow(T("Allow LAN bind", "允许局域网监听"), allowLan);
        allowPublic = ToggleRow(T("Allow public/wildcard bind", "允许公网/通配监听"), allowPublic);

        GUILayout.Space(8);
        GUILayout.Label(T("Security", "安全"));
        requireAuth = ToggleRow(T("Require bearer auth", "需要 Bearer 鉴权"), requireAuth);
        allowStateChangingCommands = ToggleRow(T("Allow state-changing commands", "允许状态修改命令"), allowStateChangingCommands);
        denyDangerousCommands = ToggleRow(T("Deny dangerous commands", "拒绝危险命令"), denyDangerousCommands);
        extraAllowedCommands = TextRow(T("Extra allowed commands", "额外允许命令"), extraAllowedCommands);
        regenerateToken = ToggleRow(T("Regenerate token on save", "保存时重新生成 token"), regenerateToken);

        GUILayout.Space(8);
        GUILayout.Label(T("Limits", "限制"));
        maxCommandLength = TextRow(T("Max command length", "最大命令长度"), maxCommandLength);
        maxQueueDepth = TextRow(T("Max queue depth", "最大队列深度"), maxQueueDepth);
        maxCommandsPerSecond = TextRow(T("Max commands / second", "每秒最大命令数"), maxCommandsPerSecond);
        maxCommandsPerFrame = TextRow(T("Max commands / frame", "每帧最大执行数"), maxCommandsPerFrame);

        GUILayout.Space(8);
        GUILayout.Label(T("Audit", "审计"));
        auditLogEnabled = ToggleRow(T("Audit log enabled", "启用审计日志"), auditLogEnabled);

        // Access info section
        GUILayout.Space(12);
        GUILayout.Label(T("Remote Access (click to open)", "远程访问（点击打开）"));

        var portStr = port.Trim();
        var token = host.Config.Token;
        var showToken = true; // Always include token in URLs for convenience

        // Collect all local IP addresses
        var addresses = new System.Collections.Generic.List<string>();
        addresses.Add("127.0.0.1");
        try
        {
            var hostEntry = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var addr in hostEntry.AddressList)
            {
                var s = addr.ToString();
                if (s.Contains('.') && !s.StartsWith("127.") && !s.StartsWith("169.254."))
                    addresses.Add(s);
            }
        }
        catch { }

        foreach (var ip in addresses)
        {
            var label = ip == "127.0.0.1" ? T("Local", "本机") : ip;
            var baseUrl = $"http://{ip}:{portStr}/";
            var fullUrl = baseUrl + (showToken ? $"?token={token}" : "");
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));
            if (GUILayout.Button(baseUrl, GUILayout.Width(360)))
            {
                Application.OpenURL(fullUrl);
            }
            GUILayout.EndHorizontal();
        }

        // WAN IP detection (best-effort via DNS)
        GUILayout.BeginHorizontal();
        GUILayout.Label(T("WAN", "公网"), GUILayout.Width(120));
        if (allowPublic)
        {
            var publicIp = bindAddress.Trim();
            if (publicIp == "0.0.0.0" || publicIp == "::")
                publicIp = "*";
            var baseUrl = $"http://{publicIp}:{portStr}/";
            var fullUrl = baseUrl + (showToken ? $"?token={token}" : "");
            if (GUILayout.Button(baseUrl, GUILayout.Width(360)))
                Application.OpenURL(fullUrl);
        }
        else
        {
            GUILayout.Label(T("Enable AllowPublic above", "启用上方公网选项"), GUILayout.Width(360));
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label(status);

        if (IsDangerous())
        {
            GUILayout.Label(T("Warning: this save changes auth, public/LAN exposure, or command policy.", "警告：本次保存会修改鉴权、公网/局域网暴露或命令策略。"));
        }

        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(confirmDangerousSave ? T("Confirm Save", "确认保存") : T("Save", "保存")))
        {
            Save();
        }

        if (GUILayout.Button(T("Reload", "重新加载")))
        {
            ResetFromConfig();
        }

        if (GUILayout.Button(T("Close", "关闭")))
        {
            visible = false;
        }
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    private string TextRow(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(190));
        var next = GUILayout.TextField(value, GUILayout.Width(260));
        GUILayout.EndHorizontal();
        return next;
    }

    private bool ToggleRow(string label, bool value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(190));
        var next = GUILayout.Toggle(value, value ? T("Enabled", "已启用") : T("Disabled", "已禁用"), GUILayout.Width(260));
        GUILayout.EndHorizontal();
        return next;
    }

    private void ResetFromConfig()
    {
        if (host == null)
        {
            return;
        }

        var config = host.Config;
        bindAddress = config.BindAddress.Value;
        port = config.Port.Value.ToString();
        requireAuth = config.RequireAuth.Value;
        allowLan = config.AllowLan.Value;
        allowPublic = config.AllowPublic.Value;
        allowStateChangingCommands = config.AllowStateChangingCommands.Value;
        denyDangerousCommands = config.DenyDangerousCommands.Value;
        extraAllowedCommands = config.ExtraAllowedCommands.Value;
        maxCommandLength = config.MaxCommandLength.Value.ToString();
        maxQueueDepth = config.MaxQueueDepth.Value.ToString();
        maxCommandsPerSecond = config.MaxCommandsPerSecond.Value.ToString();
        maxCommandsPerFrame = config.MaxCommandsPerFrame.Value.ToString();
        auditLogEnabled = config.AuditLogEnabled.Value;
        regenerateToken = false;
        confirmDangerousSave = false;
        status = T("Loaded current config.", "已加载当前配置。");
    }

    private void Save()
    {
        if (host == null)
        {
            return;
        }

        if (IsDangerous() && !confirmDangerousSave)
        {
            confirmDangerousSave = true;
            status = T("Dangerous config change pending. Press Confirm Save to apply.", "危险配置修改待确认。再次点击确认保存以应用。");
            return;
        }

        if (!TryParsePositiveInt(port, 1, 65535, out var parsedPort)
            || !TryParsePositiveInt(maxCommandLength, 1, 2048, out var parsedMaxCommandLength)
            || !TryParsePositiveInt(maxQueueDepth, 1, 1024, out var parsedMaxQueueDepth)
            || !TryParsePositiveInt(maxCommandsPerSecond, 1, 30, out var parsedMaxCommandsPerSecond)
            || !TryParsePositiveInt(maxCommandsPerFrame, 1, 16, out var parsedMaxCommandsPerFrame))
        {
            status = T("Invalid numeric value.", "数值无效。");
            return;
       }

       var config = host.Config;

        var trimmedBind = bindAddress.Trim();
        var restartHttp = !string.Equals(config.BindAddress.Value, trimmedBind, StringComparison.Ordinal)
            || config.Port.Value != parsedPort
            || config.AllowLan.Value != allowLan
            || config.AllowPublic.Value != allowPublic;

        // 预验证网络策略，避免部分写入
        if (!IsLoopback(trimmedBind) && !allowLan && !allowPublic)
        {
            status = T("Non-loopback bind address requires AllowLan or AllowPublic.", "非回环监听地址需要启用局域网或公网选项。");
            return;
        }

        if ((trimmedBind == "0.0.0.0" || trimmedBind == "::") && !allowPublic)
        {
            status = T("Wildcard bind address requires AllowPublic.", "通配监听地址需要启用公网选项。");
            return;
        }

        config.BindAddress.Value = trimmedBind;
        config.Port.Value = parsedPort;
        config.RequireAuth.Value = requireAuth;
        config.AllowLan.Value = allowLan;
        config.AllowPublic.Value = allowPublic;
        config.AllowStateChangingCommands.Value = allowStateChangingCommands;
        config.DenyDangerousCommands.Value = denyDangerousCommands;
        config.ExtraAllowedCommands.Value = extraAllowedCommands.Trim();
        config.MaxCommandLength.Value = parsedMaxCommandLength;
        config.MaxQueueDepth.Value = parsedMaxQueueDepth;
        config.MaxCommandsPerSecond.Value = parsedMaxCommandsPerSecond;
        config.MaxCommandsPerFrame.Value = parsedMaxCommandsPerFrame;
        config.AuditLogEnabled.Value = auditLogEnabled;

        if (regenerateToken)
        {
            config.RegenerateToken();
        }

        var result = host.ApplyInGameConfigChanges(restartHttp);
        regenerateToken = false;
        confirmDangerousSave = false;
        ResetFromConfig();
        status = TranslateResult(result);
    }

    private bool IsDangerous()
    {
        return !requireAuth || allowLan || allowPublic || allowStateChangingCommands || !denyDangerousCommands || !string.IsNullOrWhiteSpace(extraAllowedCommands);
    }

    private bool IsChinese()
    {
        if (forceChinese.HasValue)
        {
            return forceChinese.Value;
        }

        return Application.systemLanguage == SystemLanguage.Chinese
            || Application.systemLanguage == SystemLanguage.ChineseSimplified
            || Application.systemLanguage == SystemLanguage.ChineseTraditional;
    }

    private string T(string en, string zh)
    {
        return IsChinese() ? zh : en;
    }

    private string TranslateResult(string result)
    {
        if (result == "Saved.")
        {
            return T("Saved.", "已保存。");
        }

        if (result == "Saved. HTTP listener restarted.")
        {
            return T("Saved. HTTP listener restarted.", "已保存，HTTP 监听器已重启。");
        }

        if (result.StartsWith("Save failed: ", StringComparison.Ordinal))
        {
            return T(result, "保存失败：" + result.Substring("Save failed: ".Length));
        }

        return result;
    }

    private static bool TryParsePositiveInt(string value, int min, int max, out int parsed)
    {
        if (!int.TryParse(value.Trim(), out parsed))
        {
            return false;
        }

        return parsed >= min && parsed <= max;
    }

    private static bool IsLoopback(string value)
    {
        if (string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(value, out var address) && IPAddress.IsLoopback(address);
    }
}
