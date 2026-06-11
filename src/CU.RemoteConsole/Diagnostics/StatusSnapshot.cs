using System;
using CU.RemoteConsole.Security;

namespace CU.RemoteConsole.Diagnostics;

public sealed class StatusSnapshot
{
    public StatusSnapshot(
        HealthSnapshot health,
        int maxQueueDepth,
        int maxCommandsPerSecond,
        int maxCommandsPerFrame,
        bool allowLan,
        bool allowPublic,
        bool allowStateChangingCommands,
        bool auditLogEnabled,
        CommandPolicySummary policy)
    {
        Health = health;
        MaxQueueDepth = maxQueueDepth;
        MaxCommandsPerSecond = maxCommandsPerSecond;
        MaxCommandsPerFrame = maxCommandsPerFrame;
        AllowLan = allowLan;
        AllowPublic = allowPublic;
        AllowStateChangingCommands = allowStateChangingCommands;
        AuditLogEnabled = auditLogEnabled;
        Policy = policy;
        GeneratedAt = DateTimeOffset.UtcNow;
    }

    public HealthSnapshot Health { get; }

    public int MaxQueueDepth { get; }

    public int MaxCommandsPerSecond { get; }

    public int MaxCommandsPerFrame { get; }

    public bool AllowLan { get; }

    public bool AllowPublic { get; }

    public bool AllowStateChangingCommands { get; }

    public bool AuditLogEnabled { get; }

    public CommandPolicySummary Policy { get; }

    public DateTimeOffset GeneratedAt { get; }
}
