using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Managers;
using TogawaSakiko.NativeCode.Diagnostics;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

internal static class SakikoProgressionCompatibility
{
    public static bool ShouldRunBaseGameEpochLogic(Player localPlayer, string checkName)
    {
        if (localPlayer.Character is not SakikoCharacter)
        {
            return true;
        }

        NativeSmokeTrace.N7Info(
            $"skipped base-game {checkName} because Sakiko ships with her complete card, relic, and potion pools unlocked.");
        return false;
    }
}

[HarmonyPatch(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch")]
internal static class SakikoActEpochCompatibilityPatch
{
    private static bool Prefix(Player localPlayer)
    {
        return SakikoProgressionCompatibility.ShouldRunBaseGameEpochLogic(localPlayer, "act epoch lookup");
    }
}

[HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch")]
internal static class SakikoBossEpochCompatibilityPatch
{
    private static bool Prefix(Player localPlayer)
    {
        return SakikoProgressionCompatibility.ShouldRunBaseGameEpochLogic(localPlayer, "boss milestone epoch lookup");
    }
}

[HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch")]
internal static class SakikoEliteEpochCompatibilityPatch
{
    private static bool Prefix(Player localPlayer)
    {
        return SakikoProgressionCompatibility.ShouldRunBaseGameEpochLogic(localPlayer, "elite milestone epoch lookup");
    }
}
