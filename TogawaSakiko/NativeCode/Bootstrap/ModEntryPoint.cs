using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Modding;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Tracking;
using GameLogger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace TogawaSakiko.NativeCode.Bootstrap;

[ModInitializer(nameof(Initialize))]
public static class ModEntryPoint
{
    public const string ModId = "TogawaSakiko";
    public const string ModVersion = "v0.1.0";
    public const string TargetGameVersion = "v0.111.0";
    public const string TargetGameCommit = "41cef1ea";
    public const string BootstrapProbePath = "res://TogawaSakiko/bootstrap/native_bootstrap_probe.tres";

    private static readonly GameLogger Logger = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);
        new Harmony($"{ModId}.Native").PatchAll(assembly);
        PowerChangeLedgerService.Initialize();
        NativeModelCatalog.Register();

        VerifyGameVersion();
        VerifyMountedPck();

        Logger.Info(
            $"Native bootstrap initialized. ModVersion={ModVersion}, Assembly={assembly.GetName().Name}, GameplayModels={NativeModelCatalog.GameplayModelCount}, ExternalModDependencies=0");
    }

    private static void VerifyGameVersion()
    {
        ReleaseInfo? release = ReleaseInfoManager.Instance.ReleaseInfo;
        if (release is null)
        {
            Logger.Warn($"Game release information is unavailable. Expected {TargetGameVersion} ({TargetGameCommit}).");
            return;
        }

        if (!string.Equals(release.Version, TargetGameVersion, StringComparison.Ordinal) ||
            !release.Commit.StartsWith(TargetGameCommit, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Warn(
                $"Unverified game build detected. Expected {TargetGameVersion} ({TargetGameCommit}), found {release.Version} ({release.Commit}).");
            return;
        }

        Logger.Info($"Verified game baseline {release.Version} ({release.Commit}).");
    }

    private static void VerifyMountedPck()
    {
        if (!ResourceLoader.Exists(BootstrapProbePath))
        {
            throw new InvalidOperationException(
                $"The mod DLL loaded, but the required PCK probe was not mounted at {BootstrapProbePath}.");
        }

        Logger.Info($"Verified mounted PCK probe at {BootstrapProbePath}.");
    }
}
