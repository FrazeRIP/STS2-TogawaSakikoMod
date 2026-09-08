using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
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

[HarmonyPatch(typeof(CardModel), "get_FramePath")]
internal static class SakikoCardFrameTexturePathPatch
{
    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance.VisualCardPool is TogawaSakikoCardPool &&
            __instance.Type != CardType.Curse && __instance.Rarity != CardRarity.Ancient)
        {
            __result = __instance.Type switch
            {
                CardType.Attack => NativeAssetPaths.AttackCardFrame,
                CardType.Power => NativeAssetPaths.PowerCardFrame,
                _ => NativeAssetPaths.SkillCardFrame
            };
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.FrameMaterial), MethodType.Getter)]
internal static class SakikoCurseFrameMaterialPatch
{
    private static void Postfix(CardModel __instance, ref Material __result)
    {
        if (__instance.Type == CardType.Curse && __instance.VisualCardPool is TogawaSakikoCardPool)
        {
            __result = ModelDb.CardPool<CurseCardPool>().FrameMaterial;
        }
    }
}
