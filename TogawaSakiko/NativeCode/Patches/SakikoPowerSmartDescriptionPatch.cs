using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Patches;

// Combat hover tips read SmartDescription directly instead of the virtual Description.
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SmartDescription), MethodType.Getter)]
internal static class SakikoPowerSmartDescriptionPatch
{
    private static void Postfix(PowerModel __instance, ref LocString __result)
    {
        if (__instance is AllWillsBecomeOnePower allWills)
        {
            __result = allWills.PopulateDescription(__result);
        }
        else if (__instance is UnrestrainedPower unrestrained)
        {
            __result = unrestrained.PopulateDescription(__result);
        }
    }
}
