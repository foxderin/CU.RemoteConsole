using System;
using System.Collections.Generic;
#if !CU_REMOTE_CONSOLE_PURE_TESTS
using CU.RemoteConsole.Config;
#endif
using CU.RemoteConsole.Console;

namespace CU.RemoteConsole.Security;

public sealed class CommandPolicy
{
    private static readonly string[] SafeCommandNames =
    {
        "help",
        "log",
        "clear",
        "copylog",
        "framerate"
    };

    private static readonly string[] StateChangingCommandNames =
    {
        "heal",
        "coagulate",
        "spawn",
        "spawncategory",
        "tp",
        "addxp",
        "timescale",
        "starterkit",
        "fullbright",
        "noclip",
        "freecam",
        "talk",
        "alert",
        "playsound",
        "ui",
        "echo"
    };

    private static readonly string[] DangerousCommandNames =
    {
        "kill",
        "saveandquit",
        "nukeplayerprefs",
        "openfolder",
        "setbodyfield",
        "setlimbfield",
        "amputate",
        "addcustomcommand",
        "removecustomcommand",
        "skiplayer",
        "resetskills",
        "fucklore",
        "unchipped",
        "addliquid",
        "locate",
        "music",
        "bind",
        "repeat",
        "explode",
        "floodfill",
        "plushies",
        "errorlogging"
    };

    private static readonly HashSet<string> SafeCommands = new HashSet<string>(SafeCommandNames, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> StateChangingCommands = new HashSet<string>(StateChangingCommandNames, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> DangerousCommands = new HashSet<string>(DangerousCommandNames, StringComparer.OrdinalIgnoreCase);

    private readonly int maxCommandLength;
    private readonly bool denyDangerousCommands;

#if !CU_REMOTE_CONSOLE_PURE_TESTS
    public CommandPolicy(RemoteConsoleConfig config)
        : this(config.MaxCommandLength.Value, config.DenyDangerousCommands.Value)
    {
    }
#endif

    public CommandPolicy(int maxCommandLength, bool denyDangerousCommands)
    {
        this.maxCommandLength = maxCommandLength;
        this.denyDangerousCommands = denyDangerousCommands;
    }

    public PolicyDecision Evaluate(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return PolicyDecision.Reject("empty_command", CommandClassification.Unknown);
        }

        var normalized = command.Trim();
        if (normalized.Length > maxCommandLength)
        {
            return PolicyDecision.Reject("command_too_long", CommandClassification.Unknown);
        }

        if (normalized.IndexOf('\0') >= 0 || normalized.IndexOf('\r') >= 0 || normalized.IndexOf('\n') >= 0)
        {
            return PolicyDecision.Reject("invalid_command_characters", CommandClassification.Unknown);
        }

        var name = GetCommandName(normalized);
        var classification = Classify(name);

        if (classification == CommandClassification.Safe)
        {
            return PolicyDecision.Accept(normalized, classification);
        }

        if (classification == CommandClassification.Dangerous && denyDangerousCommands)
        {
            return PolicyDecision.Reject("dangerous_command_denied", classification, name);
        }

        return PolicyDecision.Reject("command_not_allowlisted", classification, name);
    }

    public IReadOnlyList<CommandCatalogEntry> GetCatalog()
    {
        var entries = new List<CommandCatalogEntry>();
        AddCatalogEntries(entries, SafeCommandNames, CommandClassification.Safe, true, "allowed");
        AddCatalogEntries(entries, StateChangingCommandNames, CommandClassification.StateChanging, false, "state_changing_not_enabled");
        AddCatalogEntries(entries, DangerousCommandNames, CommandClassification.Dangerous, false, denyDangerousCommands ? "dangerous_command_denied" : "command_not_allowlisted");
        return entries;
    }

    public CommandPolicySummary GetSummary()
    {
        return new CommandPolicySummary(
            SafeCommandNames.Length,
            StateChangingCommandNames.Length,
            DangerousCommandNames.Length,
            0,
            SafeCommandNames.Length,
            maxCommandLength,
            denyDangerousCommands);
    }

    private static void AddCatalogEntries(List<CommandCatalogEntry> entries, IEnumerable<string> names, CommandClassification classification, bool allowed, string policyReason)
    {
        foreach (var name in names)
        {
            entries.Add(new CommandCatalogEntry(name, classification, allowed, policyReason));
        }
    }

    private static string GetCommandName(string command)
    {
        var index = command.IndexOf(' ');
        return index < 0 ? command : command.Substring(0, index);
    }

    private static CommandClassification Classify(string name)
    {
        if (SafeCommands.Contains(name))
        {
            return CommandClassification.Safe;
        }

        if (DangerousCommands.Contains(name))
        {
            return CommandClassification.Dangerous;
        }

        if (StateChangingCommands.Contains(name))
        {
            return CommandClassification.StateChanging;
        }

        return CommandClassification.Unknown;
    }
}
