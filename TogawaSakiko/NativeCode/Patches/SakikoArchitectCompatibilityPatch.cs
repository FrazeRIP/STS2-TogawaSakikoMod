using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Diagnostics;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(TheArchitect), "WinRun")]
internal static class SakikoArchitectCompatibilityPatch
{
    private static bool Prefix(TheArchitect __instance, ref Task __result)
    {
        if (__instance.Owner?.Character is not SakikoCharacter)
        {
            return true;
        }

        NativeSmokeTrace.N7Info(
            "used the native WinRun fallback because the base-game Architect event has no custom-character dialogue entry.");
        __result = RunManager.Instance.WinRun();
        return false;
    }
}
