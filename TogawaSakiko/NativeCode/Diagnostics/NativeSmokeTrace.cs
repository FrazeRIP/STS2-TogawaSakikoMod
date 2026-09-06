using MegaCrit.Sts2.Core.Helpers;
using System.Threading;
using GameLogger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class NativeSmokeTrace
{
    public const string GameplayArgument = "togawa-native-gameplay-smoke";
    public const string VanillaGameplayArgument = "togawa-native-vanilla-smoke";
    public const string ReloadArgument = "togawa-native-reload-smoke";

    private static readonly GameLogger Logger = new(Bootstrap.ModEntryPoint.ModId, LogType.Generic);
    private static int _modelDbInitialized;

    public static bool Enabled => CommandLineHelper.HasArg(GameplayArgument);

    public static bool VanillaGameplayEnabled => CommandLineHelper.HasArg(VanillaGameplayArgument);

    public static bool AutoSlayEnabled => Enabled || VanillaGameplayEnabled;

    public static bool ReloadEnabled => CommandLineHelper.HasArg(ReloadArgument);

    public static bool ModelDbInitialized => Volatile.Read(ref _modelDbInitialized) != 0;

    public static void MarkModelDbInitialized()
    {
        Volatile.Write(ref _modelDbInitialized, 1);
    }

    public static void Info(string message)
    {
        if (Enabled)
        {
            Logger.Info("Native gameplay smoke: " + message);
        }
    }

    public static void ReloadInfo(string message)
    {
        if (ReloadEnabled)
        {
            Logger.Info("Native reload smoke: " + message);
        }
    }
}
