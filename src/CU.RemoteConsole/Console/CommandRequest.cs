using System;

namespace CU.RemoteConsole.Console;

public sealed class CommandRequest
{
    public CommandRequest(
        string queueId,
        DateTimeOffset submittedAt,
        string command,
        string commandName,
        string remoteEndpoint,
        string tokenFingerprint,
        CommandClassification classification)
    {
        QueueId = queueId;
        SubmittedAt = submittedAt;
        Command = command;
        CommandName = commandName;
        RemoteEndpoint = remoteEndpoint;
        TokenFingerprint = tokenFingerprint;
        Classification = classification;
    }

    public string QueueId { get; }

    public DateTimeOffset SubmittedAt { get; }

    public string Command { get; }

    public string CommandName { get; }

    public string RemoteEndpoint { get; }

    public string TokenFingerprint { get; }

    public CommandClassification Classification { get; }
}
