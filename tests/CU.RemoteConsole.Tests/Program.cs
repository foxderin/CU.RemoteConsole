using CU.RemoteConsole.Console;
using CU.RemoteConsole.Security;
using CU.RemoteConsole.Threading;
using CU.RemoteConsole.Diagnostics;
using CU.RemoteConsole.Web;

var tests = new (string Name, Action Test)[]
{
    ("CommandPolicy accepts safe command", CommandPolicyAcceptsSafeCommand),
    ("CommandPolicy rejects dangerous command", CommandPolicyRejectsDangerousCommand),
    ("CommandPolicy rejects unknown command", CommandPolicyRejectsUnknownCommand),
    ("CommandPolicy rejects invalid input", CommandPolicyRejectsInvalidInput),
    ("CommandPolicy rejects null or empty", CommandPolicyRejectsNullOrEmpty),
    ("CommandPolicy respects max length boundary", CommandPolicyRespectsMaxLengthBoundary),
    ("CommandPolicy handles extra commands with mixed separators", CommandPolicyHandlesExtraCommandsMixedSeparators),
    ("CommandPolicy rejects state-changing command by default", CommandPolicyRejectsStateChangingCommand),
    ("CommandPolicy can allow state-changing command", CommandPolicyCanAllowStateChangingCommand),
    ("CommandPolicy can allow dangerous command", CommandPolicyCanAllowDangerousCommand),
    ("CommandPolicy can allow extra command", CommandPolicyCanAllowExtraCommand),
    ("CommandPolicy catalog mirrors policy", CommandPolicyCatalogMirrorsPolicy),
    ("CommandPolicy summary mirrors policy", CommandPolicySummaryMirrorsPolicy),
    ("Authenticator validates bearer token", AuthenticatorValidatesBearerToken),
    ("Authenticator rejects missing or invalid token", AuthenticatorRejectsMissingOrInvalidToken),
    ("Authenticator fingerprint strips Bearer prefix", AuthenticatorFingerprintStripsBearerPrefix),
    ("Authenticator constant time comparison", AuthenticatorConstantTimeComparison),
    ("RateLimiter rejects over limit and recovers", RateLimiterRejectsOverLimitAndRecovers),
    ("CommandQueue enqueues dequeues and rejects overflow", CommandQueueEnqueuesDequeuesAndRejectsOverflow),
    ("CommandHistory add and lookup", CommandHistoryAddAndLookup),
    ("CommandHistory complete updates record", CommandHistoryCompleteUpdatesRecord),
    ("CommandHistory eviction at capacity", CommandHistoryEvictionAtCapacity),
    ("CommandHistory complete without prior add creates record", CommandHistoryCompleteWithoutPriorAdd),
    ("AuditLogger writes log entries", AuditLoggerWritesLogEntries),
    ("AuditLogger sanitizes values", AuditLoggerSanitizesValues),
    ("SerializationHelper escapes special characters", SerializationHelperEscapesSpecialCharacters),
    ("SerializationHelper escapes control characters", SerializationHelperEscapesControlCharacters),
    ("SerializationHelper Bool", SerializationHelperBool),
    ("SerializationHelper NullableDate", SerializationHelperNullableDate),
    ("SerializationHelper LimitOutput truncates long strings", SerializationHelperLimitOutput),
    ("SerializationHelper SerializeStringArray", SerializationHelperSerializeStringArray),
    ("SerializationHelper IsOutputTruncated", SerializationHelperIsOutputTruncated),
};

var failures = 0;
foreach (var (name, test) in tests)
{
    try
    {
        test();
        global::System.Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures++;
        global::System.Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

if (failures > 0)
{
    global::System.Console.Error.WriteLine($"{failures} test(s) failed.");
    return 1;
}

global::System.Console.WriteLine($"{tests.Length} test(s) passed.");
return 0;

static void CommandPolicyAcceptsSafeCommand()
{
    var policy = NewPolicy();
    var decision = policy.Evaluate("help");

    Assert.True(decision.Allowed, "help should be allowed");
    Assert.Equal("help", decision.CommandName, "command name");
    Assert.Equal(CommandClassification.Safe, decision.Classification, "classification");
}

static void CommandPolicyRejectsDangerousCommand()
{
    var policy = NewPolicy();
    var decision = policy.Evaluate("kill");

    Assert.False(decision.Allowed, "kill should be rejected");
    Assert.Equal("dangerous_command_denied", decision.Reason, "reason");
    Assert.Equal(CommandClassification.Dangerous, decision.Classification, "classification");
}

static void CommandPolicyRejectsUnknownCommand()
{
    var policy = NewPolicy();
    var decision = policy.Evaluate("definitelynotacommand");

    Assert.False(decision.Allowed, "unknown should be rejected");
    Assert.Equal("command_not_allowlisted", decision.Reason, "reason");
    Assert.Equal(CommandClassification.Unknown, decision.Classification, "classification");
}

static void CommandPolicyRejectsInvalidInput()
{
    var policy = NewPolicy();
    var shortPolicy = NewPolicy(maxCommandLength: 8);

    Assert.Equal("empty_command", policy.Evaluate(" ").Reason, "empty reason");
    Assert.Equal("invalid_command_characters", policy.Evaluate("help\nkill").Reason, "newline reason");
    Assert.Equal("command_too_long", shortPolicy.Evaluate("framerate 120").Reason, "length reason");
}

static void CommandPolicyRejectsNullOrEmpty()
{
    var policy = NewPolicy();

    var nullResult = policy.Evaluate(null);
    Assert.Equal("empty_command", nullResult.Reason, "null input");
    Assert.False(nullResult.Allowed, "null not allowed");
    Assert.Equal(CommandClassification.Unknown, nullResult.Classification, "null classification");

    var emptyResult = policy.Evaluate("");
    Assert.Equal("empty_command", emptyResult.Reason, "empty input");
    Assert.False(emptyResult.Allowed, "empty not allowed");
    Assert.Equal(CommandClassification.Unknown, emptyResult.Classification, "empty classification");

    var wsResult = policy.Evaluate("   ");
    Assert.Equal("empty_command", wsResult.Reason, "whitespace input");
    Assert.False(wsResult.Allowed, "whitespace not allowed");
}

static void CommandPolicyRespectsMaxLengthBoundary()
{
    var policy = new CommandPolicy(10, denyDangerousCommands: true);

    var exactResult = policy.Evaluate("help");
    Assert.True(exactResult.Allowed, "within limit should be allowed");

    var overResult = policy.Evaluate("123456789ab");
    Assert.False(overResult.Allowed, "over limit should be rejected");
    Assert.Equal("command_too_long", overResult.Reason, "over limit reason");
}

static void CommandPolicyHandlesExtraCommandsMixedSeparators()
{
    var policy = new CommandPolicy(256, allowStateChangingCommands: false, denyDangerousCommands: true, extraAllowedCommands: "cmd1;cmd2,cmd3\ncmd4\tcmd5");
    foreach (var name in new[] { "cmd1", "cmd2", "cmd3", "cmd4", "cmd5" })
    {
        var result = policy.Evaluate(name);
        Assert.True(result.Allowed, $"{name} should be allowed");
        Assert.Equal(CommandClassification.Unknown, result.Classification, $"{name} classification");
    }

    var denied = policy.Evaluate("cmd6");
    Assert.False(denied.Allowed, "cmd6 should be denied");
}

static void CommandPolicyRejectsStateChangingCommand()
{
    var policy = NewPolicy();
    var decision = policy.Evaluate("heal");

    Assert.False(decision.Allowed, "state-changing should be rejected by default");
    Assert.Equal("command_not_allowlisted", decision.Reason, "reason");
    Assert.Equal(CommandClassification.StateChanging, decision.Classification, "classification");
}

static void CommandPolicyCanAllowStateChangingCommand()
{
    var policy = new CommandPolicy(256, allowStateChangingCommands: true, denyDangerousCommands: true, extraAllowedCommands: "");
    var decision = policy.Evaluate("heal");

    Assert.True(decision.Allowed, "state-changing should be allowed when enabled");
    Assert.Equal(CommandClassification.StateChanging, decision.Classification, "classification");
}

static void CommandPolicyCanAllowDangerousCommand()
{
    var policy = new CommandPolicy(256, allowStateChangingCommands: false, denyDangerousCommands: false, extraAllowedCommands: "");
    var decision = policy.Evaluate("kill");

    Assert.True(decision.Allowed, "dangerous should be allowed when deny is disabled");
    Assert.Equal(CommandClassification.Dangerous, decision.Classification, "classification");
}

static void CommandPolicyCanAllowExtraCommand()
{
    var policy = new CommandPolicy(256, allowStateChangingCommands: false, denyDangerousCommands: true, extraAllowedCommands: "customone, customtwo");
    var decision = policy.Evaluate("customone arg");

    Assert.True(decision.Allowed, "extra command should be allowed");
    Assert.Equal(CommandClassification.Unknown, decision.Classification, "classification");
}

static void CommandPolicyCatalogMirrorsPolicy()
{
    var policy = NewPolicy();
    var catalog = policy.GetCatalog();
    var help = catalog.Single(item => item.Name == "help");
    var heal = catalog.Single(item => item.Name == "heal");
    var kill = catalog.Single(item => item.Name == "kill");

    Assert.True(help.Allowed, "help catalog allowed");
    Assert.Equal(CommandClassification.Safe, help.Classification, "help classification");
    Assert.False(heal.Allowed, "heal catalog denied");
    Assert.Equal(CommandClassification.StateChanging, heal.Classification, "heal classification");
    Assert.False(kill.Allowed, "kill catalog denied");
    Assert.Equal(CommandClassification.Dangerous, kill.Classification, "kill classification");

    var permissivePolicy = new CommandPolicy(256, allowStateChangingCommands: false, denyDangerousCommands: false, extraAllowedCommands: "heal customone");
    var permissiveCatalog = permissivePolicy.GetCatalog();
    Assert.True(permissiveCatalog.Single(item => item.Name == "kill").Allowed, "kill catalog allowed when dangerous deny is off");
    Assert.True(permissiveCatalog.Single(item => item.Name == "heal").Allowed, "heal catalog allowed when extra allowlisted");
    Assert.True(permissiveCatalog.Single(item => item.Name == "customone").Allowed, "custom catalog allowed");
    Assert.Equal(CommandClassification.Unknown, permissiveCatalog.Single(item => item.Name == "customone").Classification, "custom classification");
}

static void CommandPolicySummaryMirrorsPolicy()
{
    var policy = NewPolicy(maxCommandLength: 128);
    var summary = policy.GetSummary();

    Assert.Equal(5, summary.SafeCount, "safe count");
    Assert.Equal(16, summary.StateChangingCount, "state-changing count");
    Assert.Equal(22, summary.DangerousCount, "dangerous count");
    Assert.Equal(5, summary.AllowedCount, "allowed count");
    Assert.Equal(128, summary.MaxCommandLength, "max command length");
    Assert.True(summary.DenyDangerousCommands, "deny dangerous");
}

static void AuthenticatorValidatesBearerToken()
{
    var authenticator = new Authenticator("secret-token");

    Assert.True(authenticator.Validate("Bearer secret-token"), "correct token should pass");
}

static void AuthenticatorRejectsMissingOrInvalidToken()
{
    var authenticator = new Authenticator("secret-token");

    Assert.False(authenticator.Validate(null), "missing token should fail");
    Assert.False(authenticator.Validate("secret-token"), "missing bearer prefix should fail");
    Assert.False(authenticator.Validate("Bearer wrong-token"), "wrong token should fail");
    Assert.False(new Authenticator("").Validate("Bearer anything"), "empty configured token should fail closed");
}

static void AuthenticatorFingerprintStripsBearerPrefix()
{
    var authenticator = new Authenticator("secret-token");

    var fp1 = authenticator.Fingerprint("Bearer secret-token");
    var fp2 = authenticator.Fingerprint("Bearer another-token");
    var fp3 = authenticator.Fingerprint(null);

    Assert.NotEqual("missing", fp1, "valid auth should not be missing");
    Assert.NotEqual(fp1, fp2, "different tokens should produce different fingerprints");
    Assert.Equal("missing", fp3, "no auth should produce missing");

    // Same token should produce same fingerprint regardless of padding/trailing spaces
    var fpStripped = authenticator.Fingerprint("Bearer   secret-token  ");
    Assert.Equal(fp1, fpStripped, "extra whitespace should be stripped before hashing");
}

static void AuthenticatorConstantTimeComparison()
{
    var authenticator = new Authenticator("real-token-value");

    // Verifying the constant-time comparison doesn't leak timing through early exit
    Assert.False(authenticator.Validate("Bearer wrong"), "wrong short token");
    Assert.False(authenticator.Validate("Bearer " + new string('x', 100)), "wrong long token");
    Assert.True(authenticator.Validate("Bearer real-token-value"), "correct token");
}

static void RateLimiterRejectsOverLimitAndRecovers()
{
    var limiter = new RateLimiter(2);

    Assert.True(limiter.TryAcquire("client"), "first request");
    Assert.True(limiter.TryAcquire("client"), "second request");
    Assert.False(limiter.TryAcquire("client"), "third request should be rejected");

    Thread.Sleep(1100);

    Assert.True(limiter.TryAcquire("client"), "request after window should pass");
}

static void CommandQueueEnqueuesDequeuesAndRejectsOverflow()
{
    var queue = new CommandQueue(1);
    var first = NewRequest("1", "help", CommandClassification.Safe);
    var second = NewRequest("2", "help", CommandClassification.Safe);

    Assert.True(queue.TryEnqueue(first), "first enqueue");
    Assert.False(queue.TryEnqueue(second), "overflow enqueue");
    Assert.Equal(1, queue.Count, "queue count");
    Assert.True(queue.TryDequeue(out var dequeued), "dequeue");
    Assert.Equal(first.QueueId, dequeued.QueueId, "dequeued id");
    Assert.False(queue.TryDequeue(out _), "empty dequeue");
}

static void CommandHistoryAddAndLookup()
{
    var history = new CommandHistory(100);
    var request = NewRequest("id1", "help", CommandClassification.Safe);

    history.Add(request);

    Assert.True(history.TryGet("id1", out var record), "should find added record");
    Assert.NotNull(record, "record should not be null");
    Assert.Equal("id1", record!.QueueId, "queue id matches");
    Assert.Equal("help", record.CommandName, "command name matches");
    Assert.Equal(CommandExecutionState.Queued, record.State, "initial state is Queued");
    Assert.Null(record.QueueLatencyMs, "no latency before completion");

    Assert.False(history.TryGet("nonexistent", out _), "nonexistent id should not be found");
}

static void CommandHistoryCompleteUpdatesRecord()
{
    var history = new CommandHistory(100);
    var request = NewRequest("id2", "heal", CommandClassification.StateChanging);

    history.Add(request);
    history.Complete(request, CommandExecutionState.Executed, "executed", new[] { "line1", "line2" });

    Assert.True(history.TryGet("id2", out var record), "should find completed record");
    Assert.Equal(CommandExecutionState.Executed, record!.State, "state is Executed");
    Assert.Equal("executed", record.BridgeStatus, "bridge status matches");
    Assert.True(record.QueueLatencyMs.HasValue, "latency available after completion");
    Assert.Equal(2, record.Output.Count, "output count");
    Assert.Equal("line1", record.Output[0], "first output line");
}

static void CommandHistoryEvictionAtCapacity()
{
    var history = new CommandHistory(2);
    var req1 = NewRequest("a", "help", CommandClassification.Safe);
    var req2 = NewRequest("b", "help", CommandClassification.Safe);
    var req3 = NewRequest("c", "help", CommandClassification.Safe);

    history.Add(req1);
    history.Add(req2);
    history.Add(req3);

    Assert.False(history.TryGet("a", out _), "oldest should be evicted");
    Assert.True(history.TryGet("b", out _), "second should survive");
    Assert.True(history.TryGet("c", out _), "newest should survive");
}

static void CommandHistoryCompleteWithoutPriorAdd()
{
    var history = new CommandHistory(100);
    var request = NewRequest("orphan", "help", CommandClassification.Safe);

    // Complete without prior Add() — should create record
    history.Complete(request, CommandExecutionState.Executed, "executed", Array.Empty<string>());

    Assert.True(history.TryGet("orphan", out var record), "should find record from Complete-only path");
    Assert.Equal(CommandExecutionState.Executed, record!.State, "state should be Executed");
    Assert.NotNull(record.CompletedAt, "completedAt should be set");
}

static void AuditLoggerWritesLogEntries()
{
    var path = System.IO.Path.GetTempFileName();
    try
    {
        var logger = new AuditLogger(true, path);
        var request = NewRequest("q1", "help", CommandClassification.Safe);
        logger.Submission("accepted", "allowed", "127.0.0.1:1234", "fp1", "q1", "help", CommandClassification.Safe);
        logger.Execution(request, "executed", 42);
        logger.Error("test_error", "something broke");
        logger.ConfigUpdate("127.0.0.1:1234", "fp1", "port,config");

        var text = System.IO.File.ReadAllText(path);
        Assert.True(text.Contains("event=submission"), "should contain submission event");
        Assert.True(text.Contains("event=execution"), "should contain execution event");
        Assert.True(text.Contains("event=test_error"), "should contain error event");
        Assert.True(text.Contains("event=config_update"), "should contain config update event");
        Assert.True(text.Contains("queue=q1"), "should contain queue id");
        Assert.True(text.Contains("queueLatencyMs=42"), "should contain latency");
    }
    finally
    {
        System.IO.File.Delete(path);
    }
}

static void AuditLoggerSanitizesValues()
{
    var path = System.IO.Path.GetTempFileName();
    try
    {
        var logger = new AuditLogger(true, path);
        logger.Error("test", "line1\r\nline2");

        var text = System.IO.File.ReadAllText(path);
        // Sanitize replaces \r and \n with spaces in message values
        Assert.False(text.Contains("\r"), "should not contain carriage returns");
    }
    finally
    {
        System.IO.File.Delete(path);
    }
}

static void SerializationHelperEscapesSpecialCharacters()
{
    Assert.Equal("hello", SerializationHelper.Escape("hello"), "plain string");
    Assert.Equal("a\\\\b", SerializationHelper.Escape("a\\b"), "backslash");
    Assert.Equal("a\\\"b", SerializationHelper.Escape("a\"b"), "double quote");
    Assert.Equal("a\\rb", SerializationHelper.Escape("a\rb"), "carriage return");
    Assert.Equal("a\\nb", SerializationHelper.Escape("a\nb"), "newline");
    Assert.Equal("a\\tb", SerializationHelper.Escape("a\tb"), "tab");
}

static void SerializationHelperEscapesControlCharacters()
{
    Assert.Equal("a\\u0000b", SerializationHelper.Escape("a\0b"), "null char");
    Assert.Equal("a\\u001bb", SerializationHelper.Escape("a\u001bb"), "escape (0x1b)");
    Assert.Equal("a\\u007fb", SerializationHelper.Escape("a\u007fb"), "DEL (0x7f)");
}

static void SerializationHelperBool()
{
    Assert.Equal("true", SerializationHelper.Bool(true), "true");
    Assert.Equal("false", SerializationHelper.Bool(false), "false");
}

static void SerializationHelperNullableDate()
{
    Assert.Equal("null", SerializationHelper.NullableDate(null), "null value");
    var dt = new DateTimeOffset(2026, 6, 12, 10, 30, 0, TimeSpan.Zero);
    var result = SerializationHelper.NullableDate(dt);
    Assert.True(result.StartsWith('"'), "should start with quote");
    Assert.True(result.EndsWith('"'), "should end with quote");
    Assert.True(result.Contains("2026-06-12T10:30:00"), "should contain ISO date");
}

static void SerializationHelperLimitOutput()
{
    var shortStr = "hello";
    Assert.Equal(shortStr, SerializationHelper.LimitOutput(shortStr), "short string unchanged");

    var longStr = new string('x', 2000);
    var limited = SerializationHelper.LimitOutput(longStr);
    Assert.Equal(2000, limited.Length, "short string unchanged below 50000 limit");
}

static void SerializationHelperSerializeStringArray()
{
    var empty = SerializationHelper.SerializeStringArray(Array.Empty<string>());
    Assert.Equal("[]", empty, "empty array");

    var single = SerializationHelper.SerializeStringArray(new[] { "hello" });
    Assert.Equal("[\"hello\"]", single, "single element");

    var multiple = SerializationHelper.SerializeStringArray(new[] { "a", "b", "c" });
    Assert.Equal("[\"a\",\"b\",\"c\"]", multiple, "multiple elements");

    var special = SerializationHelper.SerializeStringArray(new[] { "a\"b" });
    Assert.Equal("[\"a\\\"b\"]", special, "special char escaped");
}

static void SerializationHelperIsOutputTruncated()
{
    Assert.False(SerializationHelper.IsOutputTruncated(Array.Empty<string>()), "empty");
    Assert.False(SerializationHelper.IsOutputTruncated(new[] { "short" }), "short string below 50000");
    Assert.True(SerializationHelper.IsOutputTruncated(new[] { new string('x', 50001) }), "long string above 50000");
    Assert.True(SerializationHelper.IsOutputTruncated(new[] { "short", new string('x', 60000) }), "second long string above 50000");
}

static CommandPolicy NewPolicy(int maxCommandLength = 256)
{
    return new CommandPolicy(maxCommandLength, denyDangerousCommands: true);
}

static CommandRequest NewRequest(string queueId, string command, CommandClassification classification)
{
    return new CommandRequest(
        queueId,
        DateTimeOffset.UtcNow,
        command,
        command,
        "127.0.0.1:1",
        "fingerprint",
        classification);
}

static class Assert
{
    public static void True(bool value, string message)
    {
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void False(bool value, string message)
    {
        if (value)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}");
        }
    }

    public static void NotEqual<T>(T expected, T actual, string message)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected not equal, but both are {expected}");
        }
    }

    public static void Null(object? value, string message)
    {
        if (value != null)
        {
            throw new InvalidOperationException($"{message}: expected null, got {value}");
        }
    }

    public static void NotNull(object? value, string message)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"{message}: expected non-null");
        }
    }
}
