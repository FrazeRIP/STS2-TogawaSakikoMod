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
    public const string KingsContractArgument = "togawa-native-kings-contract-smoke";
    public const string KingsReloadArgument = "togawa-native-kings-contract-reload";
    public const string N6ContractArgument = "togawa-native-n6-contract-smoke";
    public const string N6ReloadArgument = "togawa-native-n6-contract-reload";
    public const string N7FullRunArgument = "togawa-native-n7-full-run";
    public const string N7ReloadArgument = "togawa-native-n7-reload";
    public const string StartingRoomArgument = "togawa-native-starting-room-smoke";

    private static readonly GameLogger Logger = new(Bootstrap.ModEntryPoint.ModId, LogType.Generic);
    private static int _modelDbInitialized;

    public static bool Enabled =>
        CommandLineHelper.HasArg(GameplayArgument) ||
        ContractEnabled ||
        N5BatchEnabled ||
        KingsContractEnabled ||
        N6ContractEnabled;

    public static bool ContractEnabled => CommandLineHelper.HasArg(ContractArgument);

    public static bool VanillaGameplayEnabled => CommandLineHelper.HasArg(VanillaGameplayArgument);

    public static bool AutoSlayEnabled => Enabled || VanillaGameplayEnabled || N7FullRunEnabled || StartingRoomEnabled;

    public static bool ReloadEnabled => CommandLineHelper.HasArg(ReloadArgument);

    public static bool ContractReloadEnabled => CommandLineHelper.HasArg(ContractReloadArgument);

    public static bool CatalogEnabled => CommandLineHelper.HasArg(CatalogArgument);

    public static bool N5BatchEnabled => CommandLineHelper.HasArg(N5BatchArgument);

    public static bool KingsContractEnabled => CommandLineHelper.HasArg(KingsContractArgument);

    public static bool KingsReloadEnabled => CommandLineHelper.HasArg(KingsReloadArgument);

    public static bool N6ContractEnabled => CommandLineHelper.HasArg(N6ContractArgument);

    public static bool N6ReloadEnabled => CommandLineHelper.HasArg(N6ReloadArgument);

    public static bool N7FullRunEnabled => CommandLineHelper.HasArg(N7FullRunArgument);

    public static bool N7ReloadEnabled => CommandLineHelper.HasArg(N7ReloadArgument);

    public static bool StartingRoomEnabled => CommandLineHelper.HasArg(StartingRoomArgument);

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

    public static void KingsInfo(string message)
    {
        if (KingsContractEnabled || KingsReloadEnabled)
        {
            Logger.Info("Kings lifecycle: " + message);
        }
    }

    public static void N6Info(string message)
    {
        if (N6ContractEnabled || N6ReloadEnabled)
        {
            Logger.Info("Phase N6 contract: " + message);
        }
    }

    public static void N7Info(string message)
    {
        if (N7FullRunEnabled || N7ReloadEnabled)
        {
            Logger.Info("Phase N7: " + message);
        }
    }

    public static void StartingRoomInfo(string message)
    {
        if (StartingRoomEnabled)
        {
            Logger.Info("Starting room contract: " + message);
        }
    }
}
