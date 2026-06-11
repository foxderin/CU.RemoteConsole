using CU.RemoteConsole.Console;
using CU.RemoteConsole.Security;
using CU.RemoteConsole.Threading;

var tests = new (string Name, Action Test)[]
{
    ("CommandPolicy accepts safe command", CommandPolicyAcceptsSafeCommand),
    ("CommandPolicy rejects dangerous command", CommandPolicyRejectsDangerousCommand),
    ("CommandPolicy rejects unknown command", CommandPolicyRejectsUnknownCommand),
    ("CommandPolicy rejects invalid input", CommandPolicyRejectsInvalidInput),
    ("CommandPolicy rejects state-changing command by default", CommandPolicyRejectsStateChangingCommand),
    ("CommandPolicy catalog mirrors policy", CommandPolicyCatalogMirrorsPolicy),
    ("CommandPolicy summary mirrors policy", CommandPolicySummaryMirrorsPolicy),
    ("Authenticator validates bearer token", AuthenticatorValidatesBearerToken),
    ("Authenticator rejects missing or invalid token", AuthenticatorRejectsMissingOrInvalidToken),
    ("RateLimiter rejects over limit and recovers", RateLimiterRejectsOverLimitAndRecovers),
    ("CommandQueue enqueues dequeues and rejects overflow", CommandQueueEnqueuesDequeuesAndRejectsOverflow)
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

static void CommandPolicyRejectsStateChangingCommand()
{
    var policy = NewPolicy();
    var decision = policy.Evaluate("heal");

    Assert.False(decision.Allowed, "state-changing should be rejected by default");
    Assert.Equal("command_not_allowlisted", decision.Reason, "reason");
    Assert.Equal(CommandClassification.StateChanging, decision.Classification, "classification");
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
}
