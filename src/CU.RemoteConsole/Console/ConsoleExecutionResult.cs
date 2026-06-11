using System.Collections.Generic;

namespace CU.RemoteConsole.Console;

public sealed class ConsoleExecutionResult
{
    public ConsoleExecutionResult(bool executed, string status, IReadOnlyList<string> output)
    {
        Executed = executed;
        Status = status;
        Output = output;
    }

    public bool Executed { get; }

    public string Status { get; }

    public IReadOnlyList<string> Output { get; }
}
