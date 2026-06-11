using System;
using System.Net;
using System.Security.Cryptography;
using BepInEx.Configuration;
using UnityEngine;

namespace CU.RemoteConsole.Config;

public sealed class RemoteConsoleConfig
{
    private readonly ConfigFile configFile;
    private readonly ConfigEntry<string> tokenEntry;

    private RemoteConsoleConfig(
        ConfigFile configFile,
        ConfigEntry<string> bindAddress,
        ConfigEntry<int> port,
        ConfigEntry<bool> requireAuth,
        ConfigEntry<string> token,
        ConfigEntry<bool> allowLan,
        ConfigEntry<bool> allowPublic,
        ConfigEntry<bool> allowStateChangingCommands,
        ConfigEntry<bool> denyDangerousCommands,
        ConfigEntry<string> extraAllowedCommands,
        ConfigEntry<int> maxCommandLength,
        ConfigEntry<int> maxQueueDepth,
        ConfigEntry<int> maxCommandsPerSecond,
        ConfigEntry<int> maxCommandsPerFrame,
        ConfigEntry<bool> auditLogEnabled,
        ConfigEntry<KeyCode> configWindowKey)
    {
        this.configFile = configFile;
        BindAddress = bindAddress;
        Port = port;
        RequireAuth = requireAuth;
        tokenEntry = token;
        AllowLan = allowLan;
        AllowPublic = allowPublic;
        AllowStateChangingCommands = allowStateChangingCommands;
        DenyDangerousCommands = denyDangerousCommands;
        ExtraAllowedCommands = extraAllowedCommands;
        MaxCommandLength = maxCommandLength;
        MaxQueueDepth = maxQueueDepth;
        MaxCommandsPerSecond = maxCommandsPerSecond;
        MaxCommandsPerFrame = maxCommandsPerFrame;
        AuditLogEnabled = auditLogEnabled;
        ConfigWindowKey = configWindowKey;
    }

    public ConfigEntry<string> BindAddress { get; }

    public ConfigEntry<int> Port { get; }

    public ConfigEntry<bool> RequireAuth { get; }

    public ConfigEntry<bool> AllowLan { get; }

    public ConfigEntry<bool> AllowPublic { get; }

    public ConfigEntry<bool> AllowStateChangingCommands { get; }

    public ConfigEntry<bool> DenyDangerousCommands { get; }

    public ConfigEntry<string> ExtraAllowedCommands { get; }

    public ConfigEntry<int> MaxCommandLength { get; }

    public ConfigEntry<int> MaxQueueDepth { get; }

    public ConfigEntry<int> MaxCommandsPerSecond { get; }

    public ConfigEntry<int> MaxCommandsPerFrame { get; }

    public ConfigEntry<bool> AuditLogEnabled { get; }

    public ConfigEntry<KeyCode> ConfigWindowKey { get; }

    public string Token => tokenEntry.Value;

    public void RegenerateToken()
    {
        tokenEntry.Value = GenerateToken();
    }

    public void Save()
    {
        ClampLimits();
        ValidateNetworkPolicy();
        configFile.Save();
    }

    public static RemoteConsoleConfig Load(ConfigFile configFile)
    {
        var loaded = new RemoteConsoleConfig(
            configFile,
            configFile.Bind("Network", "BindAddress", "127.0.0.1", "Address to bind the local control server to. Keep 127.0.0.1 unless explicitly reviewed."),
            configFile.Bind("Network", "Port", 8848, "Local control server port."),
            configFile.Bind("Security", "RequireAuth", true, "Require bearer token authentication."),
            configFile.Bind("Security", "Token", "", "Bearer token. Generated automatically when empty. Do not share this value."),
            configFile.Bind("Security", "AllowLan", false, "Allow non-loopback LAN bind addresses. Not recommended for MVP."),
            configFile.Bind("Security", "AllowPublic", false, "Allow public bind addresses. Must remain false for MVP."),
            configFile.Bind("Security", "AllowStateChangingCommands", false, "Allow known state-changing commands through the remote command policy."),
            configFile.Bind("Security", "DenyDangerousCommands", true, "Deny dangerous commands regardless of other policy."),
            configFile.Bind("Security", "ExtraAllowedCommands", "", "Comma/space separated additional command names allowed by policy."),
            configFile.Bind("Limits", "MaxCommandLength", 256, "Maximum command length in characters."),
            configFile.Bind("Limits", "MaxQueueDepth", 64, "Maximum queued command count."),
            configFile.Bind("Limits", "MaxCommandsPerSecond", 2, "Maximum accepted command submissions per token/source per second."),
            configFile.Bind("Limits", "MaxCommandsPerFrame", 1, "Maximum commands executed by Unity Update per frame."),
            configFile.Bind("Audit", "AuditLogEnabled", true, "Enable audit log file output."),
            configFile.Bind("UI", "ConfigWindowKey", KeyCode.F8, "Hotkey for the in-game CU.RemoteConsole config window."));

        loaded.EnsureToken();
        loaded.ClampLimits();
        loaded.ValidateNetworkPolicy();
        return loaded;
    }

    private void EnsureToken()
    {
        if (!string.IsNullOrWhiteSpace(tokenEntry.Value))
        {
            return;
        }

        tokenEntry.Value = GenerateToken();
        configFile.Save();
    }

    private void ClampLimits()
    {
        if (Port.Value < 1 || Port.Value > 65535)
        {
            Port.Value = 8848;
        }

        if (MaxCommandLength.Value < 1 || MaxCommandLength.Value > 2048)
        {
            MaxCommandLength.Value = 256;
        }

        if (MaxQueueDepth.Value < 1 || MaxQueueDepth.Value > 1024)
        {
            MaxQueueDepth.Value = 64;
        }

        if (MaxCommandsPerSecond.Value < 1 || MaxCommandsPerSecond.Value > 30)
        {
            MaxCommandsPerSecond.Value = 2;
        }

        if (MaxCommandsPerFrame.Value < 1 || MaxCommandsPerFrame.Value > 16)
        {
            MaxCommandsPerFrame.Value = 1;
        }
    }

    private void ValidateNetworkPolicy()
    {
        if (IsLoopback(BindAddress.Value))
        {
            return;
        }

        if (!AllowLan.Value && !AllowPublic.Value)
        {
            throw new InvalidOperationException("Non-loopback bind address requires AllowLan=true or AllowPublic=true.");
        }

        if ((BindAddress.Value == "0.0.0.0" || BindAddress.Value == "::") && !AllowPublic.Value)
        {
            throw new InvalidOperationException("Wildcard bind addresses require AllowPublic=true.");
        }
    }

    private static bool IsLoopback(string value)
    {
        if (string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(value, out var address) && IPAddress.IsLoopback(address);
    }

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
