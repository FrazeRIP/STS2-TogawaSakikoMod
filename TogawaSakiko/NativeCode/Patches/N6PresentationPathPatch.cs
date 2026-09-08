using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Potions;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(PowerModel), "get_PackedIconPath")]
internal static class N6PowerPackedIconPathPatch
{
    private static void Postfix(PowerModel __instance, ref string __result)
    {
        if (__instance is DazzlingDownPower)
        {
            __result = NativeAssetPaths.DazzlingDownIcon;
        }
        else if (__instance is FreshlySqueezedCucumberPower)
        {
            __result = NativeAssetPaths.FreshlySqueezedCucumberIcon;
        }
    }
}

[HarmonyPatch(typeof(PowerModel), "get_BigIconPath")]
internal static class N6PowerBigIconPathPatch
{
    private static void Postfix(PowerModel __instance, ref string __result)
    {
        if (__instance is DazzlingDownPower)
        {
            __result = NativeAssetPaths.DazzlingDownBigIcon;
        }
        else if (__instance is FreshlySqueezedCucumberPower)
        {
            __result = NativeAssetPaths.FreshlySqueezedCucumberBigIcon;
        }
    }
}

[HarmonyPatch(typeof(PowerModel), "get_BigBetaIconPath")]
internal static class N6PowerBigBetaIconPathPatch
{
    private static void Postfix(PowerModel __instance, ref string __result)
    {
        if (__instance is DazzlingDownPower)
        {
            __result = NativeAssetPaths.DazzlingDownBigIcon;
        }
        else if (__instance is FreshlySqueezedCucumberPower)
        {
            __result = NativeAssetPaths.FreshlySqueezedCucumberBigIcon;
        }
    }
}

[HarmonyPatch(typeof(PotionModel), "get_PackedImagePath")]
internal static class N6PotionPackedImagePathPatch
{
    private static void Postfix(PotionModel __instance, ref string __result)
    {
        if (__instance is SakikoPotionModel potion)
        {
            __result = potion.NativeContainerPath;
        }
    }
}

[HarmonyPatch(typeof(PotionModel), "get_PackedOutlinePath")]
internal static class N6PotionPackedOutlinePathPatch
{
    private static void Postfix(PotionModel __instance, ref string __result)
    {
        if (__instance is SakikoPotionModel potion)
        {
            __result = potion.NativeOutlinePath;
        }
    }
}

[HarmonyPatch(typeof(PotionModel), "get_LargeImagePath")]
internal static class N6PotionLargeImagePathPatch
{
    private static void Postfix(PotionModel __instance, ref string __result)
    {
        if (__instance is SakikoPotionModel potion)
        {
            __result = potion.NativeContainerPath;
        }
    }
}
