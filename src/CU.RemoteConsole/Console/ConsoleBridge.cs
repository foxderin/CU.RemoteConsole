using System;
using System.Collections.Generic;
using System.Reflection;

namespace CU.RemoteConsole.Console;

public sealed class ConsoleBridge
{
    private Type? consoleScriptType;
    private FieldInfo? instanceField;
    private FieldInfo? logsField;
    private MethodInfo? executeCommandMethod;
    private bool lookupAttempted;

    public ConsoleExecutionResult Execute(string command)
    {
        if (!EnsureResolved(out var status))
        {
            return new ConsoleExecutionResult(false, status, Array.Empty<string>());
        }

        var instance = instanceField!.GetValue(null);
        if (instance == null)
        {
            return new ConsoleExecutionResult(false, "console_instance_unavailable", Array.Empty<string>());
        }

        var logs = logsField?.GetValue(instance) as List<string>;
        var beforeCount = logs?.Count ?? 0;
        executeCommandMethod!.Invoke(instance, new object[] { command });
        var output = CaptureNewLogs(logs, beforeCount);
        return new ConsoleExecutionResult(true, "executed", output);
    }

    private bool EnsureResolved(out string status)
    {
        if (!lookupAttempted)
        {
            lookupAttempted = true;
            consoleScriptType = Type.GetType("ConsoleScript, Assembly-CSharp");
            instanceField = consoleScriptType?.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            logsField = consoleScriptType?.GetField("logs", BindingFlags.Public | BindingFlags.Instance);
            executeCommandMethod = consoleScriptType?.GetMethod("ExecuteCommand", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
        }

        if (consoleScriptType == null)
        {
            status = "console_type_unavailable";
            return false;
        }

        if (instanceField == null)
        {
            status = "console_instance_field_unavailable";
            return false;
        }

        if (executeCommandMethod == null)
        {
            status = "console_execute_method_unavailable";
            return false;
        }

        status = "bridge_ready";
        return true;
    }

    private static IReadOnlyList<string> CaptureNewLogs(List<string>? logs, int beforeCount)
    {
        if (logs == null || logs.Count <= beforeCount)
        {
            return Array.Empty<string>();
        }

        var output = new List<string>();
        for (var i = beforeCount; i < logs.Count; i++)
        {
            output.Add(logs[i]);
        }

        return output;
    }
}
