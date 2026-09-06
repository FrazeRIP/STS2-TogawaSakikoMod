using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(PowerModel), "get_PackedIconPath")]
internal static class PowerPackedIconPathPatch
{
    private static void Postfix(PowerModel __instance, ref string __result)
    {
        if (__instance is DazzlingPower)
        {
            __result = NativeAssetPaths.DazzlingIcon;
        }
    }
}

[HarmonyPatch(typeof(PowerModel), "get_BigIconPath")]
internal static class PowerBigIconPathPatch
{
    private static void Postfix(PowerModel __instance, ref string __result)
    {
        if (__instance is DazzlingPower)
        {
            __result = NativeAssetPaths.DazzlingBigIcon;
        }
    }
}

[HarmonyPatch(typeof(PowerModel), "get_BigBetaIconPath")]
internal static class PowerBigBetaIconPathPatch
{
    private static void Postfix(PowerModel __instance, ref string __result)
    {
        if (__instance is DazzlingPower)
        {
            __result = NativeAssetPaths.DazzlingBigIcon;
        }
    }
}
