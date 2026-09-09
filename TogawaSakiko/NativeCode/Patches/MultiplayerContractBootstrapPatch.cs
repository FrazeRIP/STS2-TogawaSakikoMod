using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Rewards;
using TogawaSakiko.NativeCode.Diagnostics;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(NGame), "LaunchMainMenu")]
internal static class MultiplayerContractBootstrapPatch
{
    private static void Postfix(ref Task __result)
    {
        if (MultiplayerContractDiagnostics.Enabled)
        {
            __result = MultiplayerContractDiagnostics.AfterStartup(__result);
        }
    }
}

[HarmonyPatch(typeof(RewardsSetSynchronizer), nameof(RewardsSetSynchronizer.BeginRewardsSet))]
internal static class MultiplayerContractRewardTracePatch
{
    private static void Postfix(RewardsSet set) => MultiplayerContractDiagnostics.ObserveRewards(set);
}
