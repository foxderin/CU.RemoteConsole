using CU.RemoteConsole.Console;

namespace CU.RemoteConsole.Security;

public sealed class CommandCatalogEntry
{
    public CommandCatalogEntry(string name, CommandClassification classification, bool allowed, string policyReason)
    {
        Name = name;
        Classification = classification;
        Allowed = allowed;
        PolicyReason = policyReason;
    }

    public string Name { get; }

    public CommandClassification Classification { get; }

    public bool Allowed { get; }

    public string PolicyReason { get; }
}
