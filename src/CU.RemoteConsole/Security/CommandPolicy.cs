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
    private static readonly Dictionary<string, string> CommandDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["addliquid"] = "Add a dose of liquid to the held container (mL).",
        ["addxp"] = "Grant experience to a skill (Strength, Resilience, or Intelligence).",
        ["alert"] = "Show a notification box on screen.",
        ["amputate"] = "Instantly remove a specified limb.",
        ["bind"] = "Bind a console command to a key.",
        ["clear"] = "Clear all text from the console log.",
        ["coagulate"] = "Set all limb bleed rates to 0.",
        ["copylog"] = "Copy all console log text to the clipboard.",
        ["echo"] = "Toggle printing command echo in the console.",
        ["errorlogging"] = "Toggle real-time error log printing.",
        ["explode"] = "Generate a fully customizable explosion.",
        ["framerate"] = "Cap the game's maximum frame rate (FPS).",
        ["freecam"] = "Toggle free camera mode.",
        ["fullbright"] = "Toggle debug fullbright lighting mode.",
        ["heal"] = "Fully reset player health, medical state, and limbs.",
        ["help"] = "Display available console commands.",
        ["kill"] = "Instantly kill the player by zeroing brain integrity.",
        ["locate"] = "Teleport the player to a specified object in the current level.",
        ["log"] = "Add a custom log event to the console.",
        ["loglocale"] = "Return the localized display name of an object.",
        ["music"] = "Control background music playback.",
        ["noclip"] = "Toggle collision-free flight mode.",
        ["nukeplayerprefs"] = "Reset all player preferences to defaults.",
        ["openfolder"] = "Open a game folder in the system file manager.",
        ["playsound"] = "Play a game sound effect by ID.",
        ["plushies"] = "Spawn all 15 plushie types in a horizontal line.",
        ["repeat"] = "Repeatedly execute a command with a delay between runs.",
        ["resetskills"] = "Reset all skill levels and experience to zero.",
        ["saveandquit"] = "Save player data and exit to the main menu.",
        ["setbodyfield"] = "Modify player-wide body state data field.",
        ["setconsolecolor"] = "Change console UI text or background color.",
        ["setconsoleheight"] = "Change the console height ratio on screen.",
        ["setlimbfield"] = "Modify a specific limb health state field.",
        ["skiplayer"] = "Switch to a different level index.",
        ["spawn"] = "Spawn items, enemies, or objects at a position.",
        ["spawncategory"] = "Spawn all items from a category drop pool.",
        ["starterkit"] = "Add a semi-random basic survival kit to the inventory.",
        ["talk"] = "Make the player character speak specified text.",
        ["timescale"] = "Control the in-game time passage speed.",
        ["tp"] = "Teleport the player to a specified position.",
        ["ui"] = "Toggle or interact with UI elements.",
        ["unchipped"] = "Toggle Unchipped mode on or off.",
        ["volume"] = "Set the game music and SFX volume (0 to 1).",
        ["pixelate"] = "Toggle the pixelation visual filter.",
        ["fucklore"] = "Skip the opening story sequence.",
        ["addcustomcommand"] = "Create a custom macro command from existing commands.",
        ["removecustomcommand"] = "Remove a previously created custom command.",
    };


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
            CommandDescriptions.TryGetValue(name, out var desc);
            entries.Add(new CommandCatalogEntry(
                name,
                CommandClassification.StateChanging,
                allowStateChangingCommands || extraAllowed,
                allowStateChangingCommands ? "allowed" : extraAllowed ? "extra_allowlisted" : "state_changing_not_enabled",
                desc));
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
            CommandDescriptions.TryGetValue(name, out var desc);
            entries.Add(new CommandCatalogEntry(name, classification, allowed, policyReason, desc));
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
