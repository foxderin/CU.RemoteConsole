using System;

namespace CU.RemoteConsole.Diagnostics;

public sealed class HealthSnapshot
{
    public HealthSnapshot(
        string pluginVersion,
        bool httpListening,
        bool patchApplied,
        bool authRequired,
        string bindAddress,
        int port,
        int queueDepth,
        string bridgeLastStatus,
        DateTimeOffset? lastExecutionAt)
    {
        PluginVersion = pluginVersion;
        HttpListening = httpListening;
        PatchApplied = patchApplied;
        AuthRequired = authRequired;
        BindAddress = bindAddress;
        Port = port;
        QueueDepth = queueDepth;
        BridgeLastStatus = bridgeLastStatus;
        LastExecutionAt = lastExecutionAt;
    }

    public string PluginVersion { get; }

    public bool HttpListening { get; }

    public bool PatchApplied { get; }

    public bool AuthRequired { get; }

    public string BindAddress { get; }

    public int Port { get; }

    public int QueueDepth { get; }

    public string BridgeLastStatus { get; }

    public DateTimeOffset? LastExecutionAt { get; }
}
