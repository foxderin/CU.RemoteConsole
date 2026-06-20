using BepInEx;
using BepInEx.Logging;

namespace CU.RemoteConsole;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class RemoteConsolePlugin : BaseUnityPlugin
{
    public const string PluginGuid = "cu.remoteconsole";
    public const string PluginName = "CU.RemoteConsole";
    public const string PluginVersion = "1.2.0";

    internal ManualLogSource PluginLogger => Logger;

    private void Awake()
    {
        if (RemoteConsoleHost.EnsureCreated(this))
        {
            Logger.LogInfo("CU.RemoteConsole host created.");
        }
    }

    private void OnDestroy()
    {
        Logger.LogInfo("CU.RemoteConsole plugin component destroyed; persistent host remains active until application quit.");
    }
}
