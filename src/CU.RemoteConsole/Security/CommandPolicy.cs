using System;
using System.Collections.Generic;
using System.Linq;
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

    private int maxCommandLength;
    private bool allowStateChangingCommands;
    private bool denyDangerousCommands;
    private HashSet<string> extraAllowedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

#if !CU_REMOTE_CONSOLE_PURE_TESTS
    public CommandPolicy(RemoteConsoleConfig config)
        : this(
            config.MaxCommandLength.Value,
            config.AllowStateChangingCommands.Value,
            config.DenyDangerousCommands.Value,
            config.ExtraAllowedCommands.Value)
    {
    }
#endif

    public CommandPolicy(int maxCommandLength, bool denyDangerousCommands)
        : this(maxCommandLength, false, denyDangerousCommands, string.Empty)
    {
    }

    public CommandPolicy(int maxCommandLength, bool allowStateChangingCommands, bool denyDangerousCommands, string extraAllowedCommands)
    {
        Update(maxCommandLength, allowStateChangingCommands, denyDangerousCommands, extraAllowedCommands);
    }

    public void Update(int maxCommandLength, bool denyDangerousCommands)
    {
        Update(maxCommandLength, allowStateChangingCommands, denyDangerousCommands, string.Join(",", extraAllowedCommands));
    }

    public void Update(int maxCommandLength, bool allowStateChangingCommands, bool denyDangerousCommands, string extraAllowedCommands)
    {
        this.maxCommandLength = Math.Max(1, Math.Min(2048, maxCommandLength));
        this.allowStateChangingCommands = allowStateChangingCommands;
        this.denyDangerousCommands = denyDangerousCommands;
        this.extraAllowedCommands = ParseExtraAllowedCommands(extraAllowedCommands);
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

        if (classification == CommandClassification.StateChanging && allowStateChangingCommands)
        {
            return PolicyDecision.Accept(normalized, classification);
        }

        if (classification == CommandClassification.Dangerous && denyDangerousCommands)
        {
            return PolicyDecision.Reject("dangerous_command_denied", classification, name);
        }

        if (classification == CommandClassification.Dangerous && !denyDangerousCommands)
        {
            return PolicyDecision.Accept(normalized, classification);
        }

        if (extraAllowedCommands.Contains(name))
        {
            return PolicyDecision.Accept(normalized, classification);
        }

        return PolicyDecision.Reject("command_not_allowlisted", classification, name);
    }

    public IReadOnlyList<CommandCatalogEntry> GetCatalog()
    {
        var entries = new List<CommandCatalogEntry>();
        AddCatalogEntries(entries, SafeCommandNames, CommandClassification.Safe, true, "allowed");

        foreach (var name in StateChangingCommandNames)
        {
            var extraAllowed = extraAllowedCommands.Contains(name);
            entries.Add(new CommandCatalogEntry(
                name,
                CommandClassification.StateChanging,
                allowStateChangingCommands || extraAllowed,
                allowStateChangingCommands ? "allowed" : extraAllowed ? "extra_allowlisted" : "state_changing_not_enabled"));
        }

        AddCatalogEntries(entries, DangerousCommandNames, CommandClassification.Dangerous, !denyDangerousCommands, denyDangerousCommands ? "dangerous_command_denied" : "allowed");
        AddCatalogEntries(
            entries,
            extraAllowedCommands
                .Where(name => !SafeCommands.Contains(name) && !StateChangingCommands.Contains(name) && !DangerousCommands.Contains(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase),
            CommandClassification.Unknown,
            true,
            "extra_allowlisted");
        return entries;
    }

    public CommandPolicySummary GetSummary()
    {
        return new CommandPolicySummary(
            SafeCommandNames.Length,
            StateChangingCommandNames.Length,
            DangerousCommandNames.Length,
            extraAllowedCommands.Count(name => !SafeCommands.Contains(name) && !StateChangingCommands.Contains(name) && !DangerousCommands.Contains(name)),
            CountAllowedCommands(),
            maxCommandLength,
            allowStateChangingCommands,
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

    private static HashSet<string> ParseExtraAllowedCommands(string? value)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(value))
        {
            return set;
        }

        var parts = value.Split(new[] { ',', ';', '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var name = part.Trim();
            if (name.Length > 0)
            {
                set.Add(name);
            }
        }

        return set;
    }

    private int CountAllowedCommands()
    {
        var count = SafeCommandNames.Length;
        count += allowStateChangingCommands
            ? StateChangingCommandNames.Length
            : extraAllowedCommands.Count(name => StateChangingCommands.Contains(name));
        count += denyDangerousCommands ? 0 : DangerousCommandNames.Length;
        count += extraAllowedCommands.Count(name => !SafeCommands.Contains(name) && !StateChangingCommands.Contains(name) && !DangerousCommands.Contains(name));
        return count;
    }
}
