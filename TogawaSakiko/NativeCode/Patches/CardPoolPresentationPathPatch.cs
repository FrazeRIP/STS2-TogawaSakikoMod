using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Pools;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPath), new[] { typeof(string) })]
internal static class SakikoEnergyIconPathPatch
{
    private static void Postfix(string prefix, ref string __result)
    {
        if (string.Equals(prefix, "togawa_sakiko", StringComparison.OrdinalIgnoreCase))
        {
            __result = NativeAssetPaths.EnergyIcon;
        }
    }
}

[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.FrameMaterialPath), MethodType.Getter)]
internal static class SakikoCardFrameMaterialPathPatch
{
    private static void Postfix(CardPoolModel __instance, ref string __result)
    {
        if (__instance is TogawaSakikoCardPool)
        {
            __result = NativeAssetPaths.CardFrameMaterial;
        }
    }
}
