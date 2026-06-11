using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using CU.RemoteConsole.Config;
using CU.RemoteConsole.Console;
using CU.RemoteConsole.Diagnostics;
using CU.RemoteConsole.Security;
using CU.RemoteConsole.Threading;
using Newtonsoft.Json.Linq;

namespace CU.RemoteConsole.Web;

public sealed class LocalHttpServer : IDisposable
{
    private readonly RemoteConsoleConfig config;
    private readonly Authenticator authenticator;
    private readonly CommandPolicy policy;
    private readonly RateLimiter rateLimiter;
    private readonly CommandQueue queue;
    private readonly CommandHistory commandHistory;
    private readonly AuditLogger auditLogger;
    private readonly Func<HealthSnapshot> healthSnapshot;
    private readonly Func<StatusSnapshot> statusSnapshot;
    private readonly HttpListener listener;
    private Thread? worker;
    private volatile bool stopping;

    public LocalHttpServer(
        RemoteConsoleConfig config,
        Authenticator authenticator,
        CommandPolicy policy,
        RateLimiter rateLimiter,
        CommandQueue queue,
        CommandHistory commandHistory,
        AuditLogger auditLogger,
        Func<HealthSnapshot> healthSnapshot,
        Func<StatusSnapshot> statusSnapshot)
    {
        this.config = config;
        this.authenticator = authenticator;
        this.policy = policy;
        this.rateLimiter = rateLimiter;
        this.queue = queue;
        this.commandHistory = commandHistory;
        this.auditLogger = auditLogger;
        this.healthSnapshot = healthSnapshot;
        this.statusSnapshot = statusSnapshot;
        listener = new HttpListener();
        listener.Prefixes.Add($"http://{config.BindAddress.Value}:{config.Port.Value}/");
    }

    public string Prefix => $"http://{config.BindAddress.Value}:{config.Port.Value}/";

    public void Start()
    {
        if (!HttpListener.IsSupported)
        {
            throw new InvalidOperationException("HttpListener is not supported by this runtime.");
        }

        listener.Start();
        worker = new Thread(Run)
        {
            IsBackground = true,
            Name = "CU.RemoteConsole.Http"
        };
        worker.Start();
    }

    public void Dispose()
    {
        stopping = true;
        try
        {
            listener.Stop();
        }
        catch
        {
            // Listener may already be stopped by runtime shutdown.
        }

        try
        {
            listener.Close();
        }
        catch
        {
            // Nothing useful to do during plugin unload.
        }

        if (worker != null && worker.IsAlive)
        {
            worker.Join(TimeSpan.FromSeconds(2));
        }
    }

    private void Run()
    {
        while (!stopping)
        {
            try
            {
                var context = listener.GetContext();
                Handle(context);
            }
            catch (HttpListenerException)
            {
                if (!stopping)
                {
                    auditLogger.Error("http_listener", "listener_exception");
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                auditLogger.Error("http_listener", ex.GetType().Name);
            }
        }
    }

    private void Handle(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        response.Headers["Cache-Control"] = "no-store";

        try
        {
            if (request.HttpMethod == "GET" && request.Url != null && request.Url.AbsolutePath == "/health")
            {
                WriteJson(response, 200, SerializeHealth(healthSnapshot()));
                return;
            }

            if (request.HttpMethod == "GET" && request.Url != null && request.Url.AbsolutePath == "/")
            {
                WriteHtml(response, 200, EmbeddedWebConsole.Html);
                return;
            }

            if (request.HttpMethod == "GET" && request.Url != null && request.Url.AbsolutePath == "/api/status")
            {
                HandleStatus(context);
                return;
            }

            if (request.HttpMethod == "POST" && request.Url != null && request.Url.AbsolutePath == "/api/commands")
            {
                HandleCommand(context);
                return;
            }

            if (request.HttpMethod == "GET" && request.Url != null && request.Url.AbsolutePath == "/api/commands")
            {
                HandleRecentCommands(context);
                return;
            }

            if (request.HttpMethod == "GET" && request.Url != null && request.Url.AbsolutePath == "/api/commands/catalog")
            {
                HandleCommandCatalog(context);
                return;
            }

            if (request.HttpMethod == "GET" && request.Url != null && request.Url.AbsolutePath.StartsWith("/api/commands/", StringComparison.Ordinal))
            {
                HandleCommandStatus(context);
                return;
            }

            WriteJson(response, 404, "{\"error\":\"not_found\"}");
        }
        finally
        {
            response.Close();
        }
    }

    private void HandleCommand(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var remote = request.RemoteEndPoint?.ToString() ?? "unknown";
        var authorization = request.Headers["Authorization"];
        var fingerprint = authenticator.Fingerprint(authorization);

        if (!rateLimiter.TryAcquire(remote + ":" + fingerprint))
        {
            auditLogger.Submission("rejected", "rate_limited", remote, fingerprint, null, "-", CommandClassification.Unknown);
            WriteJson(response, 429, "{\"error\":\"rate_limited\"}");
            return;
        }

        if (!IsAllowedOrigin(request.Headers["Origin"]))
        {
            auditLogger.Submission("rejected", "invalid_origin", remote, fingerprint, null, "-", CommandClassification.Unknown);
            WriteJson(response, 403, "{\"error\":\"invalid_origin\"}");
            return;
        }

        if (config.RequireAuth.Value && !authenticator.Validate(authorization))
        {
            auditLogger.Submission("rejected", "invalid_auth", remote, fingerprint, null, "-", CommandClassification.Unknown);
            WriteJson(response, 401, "{\"error\":\"invalid_auth\"}");
            return;
        }

        if (request.ContentLength64 > 4096)
        {
            auditLogger.Submission("rejected", "request_too_large", remote, fingerprint, null, "-", CommandClassification.Unknown);
            WriteJson(response, 413, "{\"error\":\"request_too_large\"}");
            return;
        }

        var contentType = request.ContentType ?? string.Empty;
        if (contentType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) < 0)
        {
            auditLogger.Submission("rejected", "unsupported_content_type", remote, fingerprint, null, "-", CommandClassification.Unknown);
            WriteJson(response, 415, "{\"error\":\"unsupported_content_type\"}");
            return;
        }

        string body;
        try
        {
            body = ReadLimited(request.InputStream, 4096);
        }
        catch
        {
            auditLogger.Submission("rejected", "request_too_large", remote, fingerprint, null, "-", CommandClassification.Unknown);
            WriteJson(response, 413, "{\"error\":\"request_too_large\"}");
            return;
        }

        var command = ParseCommand(body);
        var decision = policy.Evaluate(command);
        if (!decision.Allowed)
        {
            auditLogger.Submission("rejected", decision.Reason, remote, fingerprint, null, decision.CommandName, decision.Classification);
            WriteJson(response, 403, "{\"error\":\"" + Escape(decision.Reason) + "\",\"classification\":\"" + decision.Classification + "\"}");
            return;
        }

        var queueId = Guid.NewGuid().ToString("N");
        var commandRequest = new CommandRequest(queueId, DateTimeOffset.UtcNow, decision.Command!, decision.CommandName, remote, fingerprint, decision.Classification);

        if (!queue.TryEnqueue(commandRequest))
        {
            auditLogger.Submission("rejected", "queue_full", remote, fingerprint, queueId, decision.CommandName, decision.Classification);
            WriteJson(response, 503, "{\"error\":\"queue_full\"}");
            return;
        }

        commandHistory.Add(commandRequest);
        auditLogger.Submission("accepted", "allowed", remote, fingerprint, queueId, decision.CommandName, decision.Classification);
        WriteJson(response, 202, "{\"accepted\":true,\"queueId\":\"" + queueId + "\",\"classification\":\"" + decision.Classification + "\"}");
    }

    private void HandleCommandStatus(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var authorization = request.Headers["Authorization"];

        if (config.RequireAuth.Value && !authenticator.Validate(authorization))
        {
            WriteJson(response, 401, "{\"error\":\"invalid_auth\"}");
            return;
        }

        var queueId = request.Url!.AbsolutePath.Substring("/api/commands/".Length);
        if (string.IsNullOrWhiteSpace(queueId) || !commandHistory.TryGet(queueId, out var record) || record == null)
        {
            WriteJson(response, 404, "{\"error\":\"not_found\"}");
            return;
        }

        WriteJson(response, 200, SerializeCommandRecord(record));
    }

    private void HandleRecentCommands(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var authorization = request.Headers["Authorization"];

        if (config.RequireAuth.Value && !authenticator.Validate(authorization))
        {
            WriteJson(response, 401, "{\"error\":\"invalid_auth\"}");
            return;
        }

        WriteJson(response, 200, SerializeCommandRecords(commandHistory.Recent(20)));
    }

    private void HandleCommandCatalog(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var authorization = request.Headers["Authorization"];

        if (config.RequireAuth.Value && !authenticator.Validate(authorization))
        {
            WriteJson(response, 401, "{\"error\":\"invalid_auth\"}");
            return;
        }

        WriteJson(response, 200, SerializeCommandCatalog(policy.GetCatalog()));
    }

    private void HandleStatus(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var authorization = request.Headers["Authorization"];

        if (config.RequireAuth.Value && !authenticator.Validate(authorization))
        {
            WriteJson(response, 401, "{\"error\":\"invalid_auth\"}");
            return;
        }

        WriteJson(response, 200, SerializeStatus(statusSnapshot()));
    }

    private static string ReadLimited(Stream stream, int maxBytes)
    {
        using (var memory = new MemoryStream())
        {
            var buffer = new byte[1024];
            int read;
            var total = 0;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total > maxBytes)
                {
                    throw new InvalidOperationException("request_too_large");
                }

                memory.Write(buffer, 0, read);
            }

            return Encoding.UTF8.GetString(memory.ToArray());
        }
    }

    private static string? ParseCommand(string body)
    {
        try
        {
            var json = JObject.Parse(body);
            return json.Value<string>("command");
        }
        catch
        {
            return null;
        }
    }

    private static bool IsAllowedOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return true;
        }

        return origin.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase)
            || origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteJson(HttpListenerResponse response, int statusCode, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes, 0, bytes.Length);
    }

    private static string SerializeHealth(HealthSnapshot snapshot)
    {
        return "{"
            + "\"ok\":true,"
            + "\"service\":\"CU.RemoteConsole\","
            + "\"pluginVersion\":\"" + Escape(snapshot.PluginVersion) + "\","
            + "\"httpListening\":" + Bool(snapshot.HttpListening) + ","
            + "\"patchApplied\":" + Bool(snapshot.PatchApplied) + ","
            + "\"authRequired\":" + Bool(snapshot.AuthRequired) + ","
            + "\"bindAddress\":\"" + Escape(snapshot.BindAddress) + "\","
            + "\"port\":" + snapshot.Port + ","
            + "\"queueDepth\":" + snapshot.QueueDepth + ","
            + "\"bridgeLastStatus\":\"" + Escape(snapshot.BridgeLastStatus) + "\","
            + "\"lastExecutionAt\":" + NullableDate(snapshot.LastExecutionAt)
            + "}";
    }

    private static string SerializeCommandRecord(CommandRecord record)
    {
        return "{"
            + "\"queueId\":\"" + Escape(record.QueueId) + "\","
            + "\"state\":\"" + record.State + "\","
            + "\"commandName\":\"" + Escape(record.CommandName) + "\","
            + "\"classification\":\"" + record.Classification + "\","
            + "\"bridgeStatus\":\"" + Escape(record.BridgeStatus) + "\","
            + "\"submittedAt\":\"" + record.SubmittedAt.ToString("O") + "\","
            + "\"completedAt\":" + NullableDate(record.CompletedAt) + ","
            + "\"queueLatencyMs\":" + (record.QueueLatencyMs.HasValue ? record.QueueLatencyMs.Value.ToString() : "null") + ","
            + "\"outputLineCount\":" + record.Output.Count + ","
            + "\"outputTruncated\":" + Bool(IsOutputTruncated(record.Output)) + ","
            + "\"output\":" + SerializeStringArray(record.Output)
            + "}";
    }

    private static string SerializeCommandRecords(IReadOnlyList<CommandRecord> records)
    {
        var builder = new StringBuilder();
        builder.Append("{\"items\":[");
        for (var i = 0; i < records.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(SerializeCommandRecord(records[i]));
        }

        builder.Append("]}");
        return builder.ToString();
    }

    private static string SerializeCommandCatalog(IReadOnlyList<CommandCatalogEntry> entries)
    {
        var builder = new StringBuilder();
        builder.Append("{\"items\":[");
        for (var i = 0; i < entries.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            var entry = entries[i];
            builder.Append('{')
                .Append("\"name\":\"").Append(Escape(entry.Name)).Append("\",")
                .Append("\"classification\":\"").Append(entry.Classification).Append("\",")
                .Append("\"allowed\":").Append(Bool(entry.Allowed)).Append(',')
                .Append("\"policyReason\":\"").Append(Escape(entry.PolicyReason)).Append("\"")
                .Append('}');
        }

        builder.Append("]}");
        return builder.ToString();
    }

    private static string SerializeStatus(StatusSnapshot snapshot)
    {
        return "{"
            + "\"generatedAt\":\"" + snapshot.GeneratedAt.ToString("O") + "\","
            + "\"pluginVersion\":\"" + Escape(snapshot.Health.PluginVersion) + "\","
            + "\"network\":{"
                + "\"bindAddress\":\"" + Escape(snapshot.Health.BindAddress) + "\","
                + "\"port\":" + snapshot.Health.Port + ","
                + "\"httpListening\":" + Bool(snapshot.Health.HttpListening) + ","
                + "\"allowLan\":" + Bool(snapshot.AllowLan) + ","
                + "\"allowPublic\":" + Bool(snapshot.AllowPublic)
            + "},"
            + "\"security\":{"
                + "\"authRequired\":" + Bool(snapshot.Health.AuthRequired) + ","
                + "\"denyDangerousCommands\":" + Bool(snapshot.Policy.DenyDangerousCommands) + ","
                + "\"auditLogEnabled\":" + Bool(snapshot.AuditLogEnabled)
            + "},"
            + "\"limits\":{"
                + "\"maxCommandLength\":" + snapshot.Policy.MaxCommandLength + ","
                + "\"maxQueueDepth\":" + snapshot.MaxQueueDepth + ","
                + "\"maxCommandsPerSecond\":" + snapshot.MaxCommandsPerSecond + ","
                + "\"maxCommandsPerFrame\":" + snapshot.MaxCommandsPerFrame
            + "},"
            + "\"runtime\":{"
                + "\"queueDepth\":" + snapshot.Health.QueueDepth + ","
                + "\"patchApplied\":" + Bool(snapshot.Health.PatchApplied) + ","
                + "\"bridgeLastStatus\":\"" + Escape(snapshot.Health.BridgeLastStatus) + "\","
                + "\"lastExecutionAt\":" + NullableDate(snapshot.Health.LastExecutionAt)
            + "},"
            + "\"policy\":{"
                + "\"safeCount\":" + snapshot.Policy.SafeCount + ","
                + "\"stateChangingCount\":" + snapshot.Policy.StateChangingCount + ","
                + "\"dangerousCount\":" + snapshot.Policy.DangerousCount + ","
                + "\"unknownCount\":" + snapshot.Policy.UnknownCount + ","
                + "\"allowedCount\":" + snapshot.Policy.AllowedCount
            + "}"
            + "}";
    }

    private static string NullableDate(DateTimeOffset? value)
    {
        return value.HasValue ? "\"" + value.Value.ToString("O") + "\"" : "null";
    }

    private static string Bool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string SerializeStringArray(IReadOnlyList<string> values)
    {
        var builder = new StringBuilder();
        builder.Append('[');
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append('"').Append(Escape(LimitOutput(values[i]))).Append('"');
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string LimitOutput(string value)
    {
        if (value.Length <= 1000)
        {
            return value;
        }

        return value.Substring(0, 1000) + "...";
    }

    private static bool IsOutputTruncated(IReadOnlyList<string> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i].Length > 1000)
            {
                return true;
            }
        }

        return false;
    }

    private static void WriteHtml(HttpListenerResponse response, int statusCode, string html)
    {
        var bytes = Encoding.UTF8.GetBytes(html);
        response.StatusCode = statusCode;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes, 0, bytes.Length);
    }

    private static string Escape(string value)
    {
        var builder = new StringBuilder();
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        builder.Append("\\u").Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

}
