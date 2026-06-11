namespace CU.RemoteConsole.Security;

public sealed class CommandPolicySummary
{
    public CommandPolicySummary(
        int safeCount,
        int stateChangingCount,
        int dangerousCount,
        int unknownCount,
        int allowedCount,
        int maxCommandLength,
        bool allowStateChangingCommands,
        bool denyDangerousCommands)
    {
        SafeCount = safeCount;
        StateChangingCount = stateChangingCount;
        DangerousCount = dangerousCount;
        UnknownCount = unknownCount;
        AllowedCount = allowedCount;
        MaxCommandLength = maxCommandLength;
        AllowStateChangingCommands = allowStateChangingCommands;
        DenyDangerousCommands = denyDangerousCommands;
    }

    public int SafeCount { get; }

    public int StateChangingCount { get; }

    public int DangerousCount { get; }

    public int UnknownCount { get; }

    public int AllowedCount { get; }

    public int MaxCommandLength { get; }

    public bool AllowStateChangingCommands { get; }

    public bool DenyDangerousCommands { get; }
}
