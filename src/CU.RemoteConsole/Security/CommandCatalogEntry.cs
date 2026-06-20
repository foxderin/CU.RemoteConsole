using CU.RemoteConsole.Console;

namespace CU.RemoteConsole.Security;

public sealed class CommandCatalogEntry
{
    public CommandCatalogEntry(string name, CommandClassification classification, bool allowed, string policyReason, string? description = null)
    {
        Name = name;
        Classification = classification;
        Allowed = allowed;
        PolicyReason = policyReason;
        Description = description ?? string.Empty;
    }

    public string Name { get; }

    public CommandClassification Classification { get; }

    public bool Allowed { get; }

    public string PolicyReason { get; }

    public string Description { get; }
}
