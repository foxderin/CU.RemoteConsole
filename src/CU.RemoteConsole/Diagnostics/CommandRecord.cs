using System;
using System.Collections.Generic;
using CU.RemoteConsole.Console;

namespace CU.RemoteConsole.Diagnostics;

public sealed class CommandRecord
{
    public CommandRecord(CommandRequest request)
    {
        QueueId = request.QueueId;
        SubmittedAt = request.SubmittedAt;
        CommandName = request.CommandName;
        Classification = request.Classification;
        RemoteEndpoint = request.RemoteEndpoint;
        TokenFingerprint = request.TokenFingerprint;
        State = CommandExecutionState.Queued;
    }

    public string QueueId { get; }

    public DateTimeOffset SubmittedAt { get; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string CommandName { get; }

    public CommandClassification Classification { get; }

    public string RemoteEndpoint { get; }

    public string TokenFingerprint { get; }

    public CommandExecutionState State { get; private set; }

    public string BridgeStatus { get; private set; } = "queued";

    public IReadOnlyList<string> Output { get; private set; } = Array.Empty<string>();

    public long? QueueLatencyMs => CompletedAt.HasValue ? (long)(CompletedAt.Value - SubmittedAt).TotalMilliseconds : null;

    public void Complete(CommandExecutionState state, string bridgeStatus, IReadOnlyList<string> output)
    {
        State = state;
        BridgeStatus = bridgeStatus;
        Output = output;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
