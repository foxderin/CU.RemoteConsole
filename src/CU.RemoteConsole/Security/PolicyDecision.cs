using CU.RemoteConsole.Console;

namespace CU.RemoteConsole.Security;

public sealed class PolicyDecision
{
    private PolicyDecision(bool allowed, string? command, string commandName, string reason, CommandClassification classification)
    {
        Allowed = allowed;
        Command = command;
        CommandName = commandName;
        Reason = reason;
        Classification = classification;
    }

    public bool Allowed { get; }

    public string? Command { get; }

    public string CommandName { get; }

    public string Reason { get; }

    public CommandClassification Classification { get; }

    public static PolicyDecision Accept(string command, CommandClassification classification)
    {
        return new PolicyDecision(true, command, GetCommandName(command), "allowed", classification);
    }

    public static PolicyDecision Reject(string reason, CommandClassification classification)
    {
        return Reject(reason, classification, "-");
    }

    public static PolicyDecision Reject(string reason, CommandClassification classification, string commandName)
    {
        return new PolicyDecision(false, null, commandName, reason, classification);
    }

    private static string GetCommandName(string command)
    {
        var index = command.IndexOf(' ');
        return index < 0 ? command : command.Substring(0, index);
    }
}
