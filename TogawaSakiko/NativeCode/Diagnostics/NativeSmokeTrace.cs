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
    public const string ContractArgument = "togawa-native-n3-contract-smoke";
    public const string ContractReloadArgument = "togawa-native-n3-contract-reload";
    public const string CatalogArgument = "togawa-native-n4-catalog-smoke";
    public const string N5BatchArgument = "togawa-native-n5-batch-smoke";

    private static readonly GameLogger Logger = new(Bootstrap.ModEntryPoint.ModId, LogType.Generic);
    private static int _modelDbInitialized;

    public static bool Enabled => CommandLineHelper.HasArg(GameplayArgument) || ContractEnabled || N5BatchEnabled;

    public static bool ContractEnabled => CommandLineHelper.HasArg(ContractArgument);

    public static bool VanillaGameplayEnabled => CommandLineHelper.HasArg(VanillaGameplayArgument);

    public static bool AutoSlayEnabled => Enabled || VanillaGameplayEnabled;

    public static bool ReloadEnabled => CommandLineHelper.HasArg(ReloadArgument);

    public static bool ContractReloadEnabled => CommandLineHelper.HasArg(ContractReloadArgument);

    public static bool CatalogEnabled => CommandLineHelper.HasArg(CatalogArgument);

    public static bool N5BatchEnabled => CommandLineHelper.HasArg(N5BatchArgument);

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

    public static void ContractInfo(string message)
    {
        if (ContractEnabled || ContractReloadEnabled)
        {
            Logger.Info("Phase N3 contract: " + message);
        }
    }

    public static void N5Info(string message)
    {
        if (N5BatchEnabled)
        {
            Logger.Info("Phase N5 batch: " + message);
        }
    }
}
