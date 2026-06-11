using System;
using CU.RemoteConsole.Config;
using UnityEngine;

namespace CU.RemoteConsole.UI;

public sealed class InGameConfigOverlay : MonoBehaviour
{
    private RemoteConsoleHost? host;
    private Rect windowRect = new Rect(80, 80, 520, 690);
    private Vector2 scroll;
    private bool visible;
    private bool confirmDangerousSave;
    private bool regenerateToken;
    private string status = "Ready";

    private string bindAddress = "127.0.0.1";
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

    private void Update()
    {
        if (host == null)
        {
            return;
        }

        if (Input.GetKeyDown(host.Config.ConfigWindowKey.Value))
        {
            visible = !visible;
            confirmDangerousSave = false;
            if (visible)
            {
                ResetFromConfig();
            }
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

        GUILayout.Label("Local in-game configuration");
        GUILayout.Label("Remote API remains authenticated and policy-gated. This window is for the local player.");
        GUILayout.Space(8);

        GUILayout.Label("Network");
        bindAddress = TextRow("Bind address", bindAddress);
        port = TextRow("Port", port);
        allowLan = ToggleRow("Allow LAN bind", allowLan);
        allowPublic = ToggleRow("Allow public/wildcard bind", allowPublic);

        GUILayout.Space(8);
        GUILayout.Label("Security");
        requireAuth = ToggleRow("Require bearer auth", requireAuth);
        allowStateChangingCommands = ToggleRow("Allow state-changing commands", allowStateChangingCommands);
        denyDangerousCommands = ToggleRow("Deny dangerous commands", denyDangerousCommands);
        extraAllowedCommands = TextRow("Extra allowed commands", extraAllowedCommands);
        regenerateToken = ToggleRow("Regenerate token on save", regenerateToken);

        GUILayout.Space(8);
        GUILayout.Label("Limits");
        maxCommandLength = TextRow("Max command length", maxCommandLength);
        maxQueueDepth = TextRow("Max queue depth", maxQueueDepth);
        maxCommandsPerSecond = TextRow("Max commands / second", maxCommandsPerSecond);
        maxCommandsPerFrame = TextRow("Max commands / frame", maxCommandsPerFrame);

        GUILayout.Space(8);
        GUILayout.Label("Audit");
        auditLogEnabled = ToggleRow("Audit log enabled", auditLogEnabled);

        GUILayout.Space(8);
        GUILayout.Label("Hotkey: " + host.Config.ConfigWindowKey.Value);
        GUILayout.Label(status);

        if (IsDangerous())
        {
            GUILayout.Label("Warning: this save changes auth, public/LAN exposure, or dangerous-command policy.");
        }

        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(confirmDangerousSave ? "Confirm Save" : "Save"))
        {
            Save();
        }

        if (GUILayout.Button("Reload"))
        {
            ResetFromConfig();
        }

        if (GUILayout.Button("Close"))
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
        var next = GUILayout.Toggle(value, value ? "Enabled" : "Disabled", GUILayout.Width(260));
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
        status = "Loaded current config.";
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
            status = "Dangerous config change pending. Press Confirm Save to apply.";
            return;
        }

        if (!TryParsePositiveInt(port, 1, 65535, out var parsedPort)
            || !TryParsePositiveInt(maxCommandLength, 1, 2048, out var parsedMaxCommandLength)
            || !TryParsePositiveInt(maxQueueDepth, 1, 1024, out var parsedMaxQueueDepth)
            || !TryParsePositiveInt(maxCommandsPerSecond, 1, 30, out var parsedMaxCommandsPerSecond)
            || !TryParsePositiveInt(maxCommandsPerFrame, 1, 16, out var parsedMaxCommandsPerFrame))
        {
            status = "Invalid numeric value.";
            return;
        }

        var config = host.Config;
        var restartHttp = !string.Equals(config.BindAddress.Value, bindAddress.Trim(), StringComparison.Ordinal)
            || config.Port.Value != parsedPort
            || config.AllowLan.Value != allowLan
            || config.AllowPublic.Value != allowPublic;

        config.BindAddress.Value = bindAddress.Trim();
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
        status = result;
    }

    private bool IsDangerous()
    {
        return !requireAuth || allowLan || allowPublic || allowStateChangingCommands || !denyDangerousCommands || !string.IsNullOrWhiteSpace(extraAllowedCommands);
    }

    private static bool TryParsePositiveInt(string value, int min, int max, out int parsed)
    {
        if (!int.TryParse(value.Trim(), out parsed))
        {
            return false;
        }

        return parsed >= min && parsed <= max;
    }
}
