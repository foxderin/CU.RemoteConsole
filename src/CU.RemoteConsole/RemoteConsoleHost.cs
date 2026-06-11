using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using CU.RemoteConsole.Config;
using CU.RemoteConsole.Console;
using CU.RemoteConsole.Diagnostics;
using CU.RemoteConsole.Security;
using CU.RemoteConsole.Threading;
using CU.RemoteConsole.Web;
using HarmonyLib;
using UnityEngine;

namespace CU.RemoteConsole;

public sealed class RemoteConsoleHost
{
    private static RemoteConsoleHost? instance;

    private readonly ManualLogSource logger;
    private readonly RemoteConsoleConfig remoteConfig;
    private readonly CommandQueue commandQueue;
    private readonly CommandHistory commandHistory;
    private readonly ConsoleBridge consoleBridge;
    private readonly AuditLogger auditLogger;
    private readonly CommandPolicy policy;
    private readonly LocalHttpServer httpServer;
    private readonly Harmony harmony;
    private bool patchApplied;
    private string bridgeLastStatus = "not_started";
    private DateTimeOffset? lastExecutionAt;

    private RemoteConsoleHost(RemoteConsolePlugin plugin)
    {
        logger = plugin.PluginLogger;
        logger.LogInfo("CU.RemoteConsole host loading.");

        remoteConfig = RemoteConsoleConfig.Load(plugin.Config);
        auditLogger = new AuditLogger(remoteConfig.AuditLogEnabled.Value);
        commandQueue = new CommandQueue(remoteConfig.MaxQueueDepth.Value);
        commandHistory = new CommandHistory(100);
        consoleBridge = new ConsoleBridge();

        var authenticator = new Authenticator(remoteConfig.Token);
        policy = new CommandPolicy(remoteConfig);
        var rateLimiter = new RateLimiter(remoteConfig.MaxCommandsPerSecond.Value);
        httpServer = new LocalHttpServer(remoteConfig, authenticator, policy, rateLimiter, commandQueue, commandHistory, auditLogger, CreateHealthSnapshot, CreateStatusSnapshot);
        httpServer.Start();

        harmony = new Harmony(RemoteConsolePlugin.PluginGuid);
        PatchConsoleUpdate();

        Application.quitting += Shutdown;
        logger.LogInfo($"CU.RemoteConsole listening on {httpServer.Prefix}");
        logger.LogInfo("Authentication token is stored in the BepInEx config file and is not printed to logs.");
    }

    public static bool EnsureCreated(RemoteConsolePlugin plugin)
    {
        if (instance != null)
        {
            return false;
        }

        try
        {
            instance = new RemoteConsoleHost(plugin);
            return true;
        }
        catch (System.Exception ex)
        {
            plugin.PluginLogger.LogError($"CU.RemoteConsole failed to start: {ex.GetType().Name}: {ex.Message}");
            instance?.auditLogger.Error("startup", ex.GetType().Name);
            instance?.Shutdown();
            instance = null;
            return false;
        }
    }

    public static void DrainFromConsoleUpdate()
    {
        instance?.DrainCommands();
    }

    private void DrainCommands()
    {
        var max = remoteConfig.MaxCommandsPerFrame.Value;
        for (var i = 0; i < max && commandQueue.TryDequeue(out var request); i++)
        {
            Execute(request);
        }
    }

    private void Execute(CommandRequest request)
    {
        try
        {
            var result = consoleBridge.Execute(request.Command);
            RecordExecution(
                request,
                result.Executed ? CommandExecutionState.Executed : CommandExecutionState.Failed,
                result.Status,
                result.Output);
        }
        catch (System.Exception ex)
        {
            logger.LogWarning($"Command execution failed for queue {request.QueueId}: {ex.GetType().Name}");
            RecordExecution(request, CommandExecutionState.Failed, "execution_exception_" + ex.GetType().Name, Array.Empty<string>());
        }
    }

    private void Shutdown()
    {
        Application.quitting -= Shutdown;
        harmony.UnpatchSelf();
        httpServer.Dispose();
        logger.LogInfo("CU.RemoteConsole host unloaded.");
        instance = null;
    }

    private void PatchConsoleUpdate()
    {
        var consoleScript = AccessTools.TypeByName("ConsoleScript");
        var update = AccessTools.Method(consoleScript, "Update");
        var postfix = AccessTools.Method(typeof(RemoteConsoleHost), nameof(ConsoleUpdatePostfix));
        if (consoleScript == null || update == null || postfix == null)
        {
            throw new System.InvalidOperationException("ConsoleScript.Update patch target unavailable.");
        }

        harmony.Patch(update, postfix: new HarmonyMethod(postfix));
        patchApplied = true;
        logger.LogInfo("CU.RemoteConsole patched ConsoleScript.Update for main-thread queue drain.");
    }

    private static void ConsoleUpdatePostfix()
    {
        DrainFromConsoleUpdate();
    }

    private void RecordExecution(CommandRequest request, CommandExecutionState state, string status, IReadOnlyList<string> output)
    {
        bridgeLastStatus = status;
        lastExecutionAt = DateTimeOffset.UtcNow;
        commandHistory.Complete(request, state, status, output);
        var latencyMs = (long)(lastExecutionAt.Value - request.SubmittedAt).TotalMilliseconds;
        auditLogger.Execution(request, status, latencyMs);
    }

    private HealthSnapshot CreateHealthSnapshot()
    {
        return new HealthSnapshot(
            RemoteConsolePlugin.PluginVersion,
            true,
            patchApplied,
            remoteConfig.RequireAuth.Value,
            remoteConfig.BindAddress.Value,
            remoteConfig.Port.Value,
            commandQueue.Count,
            bridgeLastStatus,
            lastExecutionAt);
    }

    private StatusSnapshot CreateStatusSnapshot()
    {
        return new StatusSnapshot(
            CreateHealthSnapshot(),
            remoteConfig.MaxQueueDepth.Value,
            remoteConfig.MaxCommandsPerSecond.Value,
            remoteConfig.MaxCommandsPerFrame.Value,
            remoteConfig.AllowLan.Value,
            remoteConfig.AllowPublic.Value,
            remoteConfig.AuditLogEnabled.Value,
            policy.GetSummary());
    }
}
