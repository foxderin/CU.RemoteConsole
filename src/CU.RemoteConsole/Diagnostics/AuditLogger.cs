using System;
using System.IO;
using BepInEx;
using CU.RemoteConsole.Console;

namespace CU.RemoteConsole.Diagnostics;

public sealed class AuditLogger
{
    private readonly object gate = new object();
    private bool enabled;
    private readonly string path;

    public AuditLogger(bool enabled)
    {
        this.enabled = enabled;
        path = Path.Combine(Paths.ConfigPath, "cu.remoteconsole.audit.log");
    }

    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
    }

    public void Submission(string decision, string reason, string remoteEndpoint, string tokenFingerprint, string? queueId, string commandName, CommandClassification classification)
    {
        Write($"event=submission decision={decision} reason={reason} remote={remoteEndpoint} token={tokenFingerprint} queue={queueId ?? "-"} command={Sanitize(commandName)} classification={classification}");
    }

    public void Execution(CommandRequest request, string status, long latencyMs)
    {
        Write($"event=execution status={status} remote={request.RemoteEndpoint} token={request.TokenFingerprint} queue={request.QueueId} command={Sanitize(request.CommandName)} classification={request.Classification} queueLatencyMs={latencyMs}");
    }

    public void Error(string eventName, string reason)
    {
        Write($"event={eventName} decision=error reason={Sanitize(reason)}");
    }

    public void ConfigUpdate(string remoteEndpoint, string tokenFingerprint, string changedKeys)
    {
        Write($"event=config_update remote={remoteEndpoint} token={tokenFingerprint} changed={Sanitize(changedKeys)}");
    }

    private void Write(string message)
    {
        if (!enabled)
        {
            return;
        }

        lock (gate)
        {
            File.AppendAllText(path, $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}");
        }
    }

    private static string Sanitize(string value)
    {
        return value.Replace('\r', ' ').Replace('\n', ' ');
    }
}
