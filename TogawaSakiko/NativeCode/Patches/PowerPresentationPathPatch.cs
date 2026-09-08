using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;
using SakikoCrueltyPower = TogawaSakiko.NativeCode.Models.Powers.CrueltyPower;

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
        else if (__instance is HypePower)
        {
            __result = NativeAssetPaths.HypeIcon;
        }
        else if (__instance is DolorisPower)
        {
            __result = NativeAssetPaths.DolorisIcon;
        }
        else if (__instance is MortisPower)
        {
            __result = NativeAssetPaths.MortisIcon;
        }
        else if (__instance is OblivionisPower)
        {
            __result = NativeAssetPaths.OblivionisIcon;
        }
        else if (__instance is TimorisPower)
        {
            __result = NativeAssetPaths.TimorisIcon;
        }
        else if (__instance is MantraPower or MonsterDivinityPower)
        {
            __result = NativeAssetPaths.MonsterDivinityIcon;
        }
        else if (__instance is KingsPower)
        {
            __result = NativeAssetPaths.KingsIcon;
        }
        else if (__instance is CuriosityPower)
        {
            __result = ModelDb.Power<StrengthPower>().PackedIconPath;
        }
        else if (__instance is EndurancePower)
        {
            __result = NativeAssetPaths.EnduranceIcon;
        }
        else if (__instance is FearlessPower)
        {
            __result = NativeAssetPaths.FearlessIcon;
        }
        else if (__instance is GodsCreationPower)
        {
            __result = NativeAssetPaths.GodsCreationIcon;
        }
        else if (__instance is GirlOfSpringPower)
        {
            __result = NativeAssetPaths.GirlOfSpringIcon;
        }
        else if (__instance is OurSongPower)
        {
            __result = NativeAssetPaths.OurSongIcon;
        }
        else if (__instance is PerdereOmniaPower)
        {
            __result = NativeAssetPaths.PerdereOmniaIcon;
        }
        else if (__instance is PrimoDieInScaenaPower)
        {
            __result = NativeAssetPaths.PrimoDieInScaenaIcon;
        }
        else if (__instance is SeizeTheFatePower)
        {
            __result = NativeAssetPaths.SeizeTheFateIcon;
        }
        else if (__instance is SharedDestinyPower)
        {
            __result = NativeAssetPaths.SharedDestinyIcon;
        }
        else if (__instance is CharismaticFormPower)
        {
            __result = NativeAssetPaths.CharismaticFormIcon;
        }
        else if (__instance is SakikoCrueltyPower)
        {
            __result = NativeAssetPaths.CrueltyIcon;
        }
        else if (__instance is CrychicPower)
        {
            __result = NativeAssetPaths.CrychicIcon;
        }
        else if (__instance is PridePower)
        {
            __result = NativeAssetPaths.PrideIcon;
        }
        else if (__instance is WishYouGoodLuckPower)
        {
            __result = NativeAssetPaths.WishYouGoodLuckIcon;
        }
        else if (__instance is WorldviewPower)
        {
            __result = NativeAssetPaths.WorldviewIcon;
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
        else if (__instance is HypePower)
        {
            __result = NativeAssetPaths.HypeBigIcon;
        }
        else if (__instance is DolorisPower)
        {
            __result = NativeAssetPaths.DolorisBigIcon;
        }
        else if (__instance is MortisPower)
        {
            __result = NativeAssetPaths.MortisBigIcon;
        }
        else if (__instance is OblivionisPower)
        {
            __result = NativeAssetPaths.OblivionisBigIcon;
        }
        else if (__instance is TimorisPower)
        {
            __result = NativeAssetPaths.TimorisBigIcon;
        }
        else if (__instance is MantraPower or MonsterDivinityPower)
        {
            __result = NativeAssetPaths.MonsterDivinityBigIcon;
        }
        else if (__instance is KingsPower)
        {
            __result = NativeAssetPaths.KingsBigIcon;
        }
        else if (__instance is CuriosityPower)
        {
            __result = ModelDb.Power<StrengthPower>().ResolvedBigIconPath;
        }
        else if (__instance is EndurancePower)
        {
            __result = NativeAssetPaths.EnduranceBigIcon;
        }
        else if (__instance is FearlessPower)
        {
            __result = NativeAssetPaths.FearlessBigIcon;
        }
        else if (__instance is GodsCreationPower)
        {
            __result = NativeAssetPaths.GodsCreationBigIcon;
        }
        else if (__instance is GirlOfSpringPower)
        {
            __result = NativeAssetPaths.GirlOfSpringBigIcon;
        }
        else if (__instance is OurSongPower)
        {
            __result = NativeAssetPaths.OurSongBigIcon;
        }
        else if (__instance is PerdereOmniaPower)
        {
            __result = NativeAssetPaths.PerdereOmniaBigIcon;
        }
        else if (__instance is PrimoDieInScaenaPower)
        {
            __result = NativeAssetPaths.PrimoDieInScaenaBigIcon;
        }
        else if (__instance is SeizeTheFatePower)
        {
            __result = NativeAssetPaths.SeizeTheFateBigIcon;
        }
        else if (__instance is SharedDestinyPower)
        {
            __result = NativeAssetPaths.SharedDestinyBigIcon;
        }
        else if (__instance is CharismaticFormPower)
        {
            __result = NativeAssetPaths.CharismaticFormBigIcon;
        }
        else if (__instance is SakikoCrueltyPower)
        {
            __result = NativeAssetPaths.CrueltyBigIcon;
        }
        else if (__instance is CrychicPower)
        {
            __result = NativeAssetPaths.CrychicBigIcon;
        }
        else if (__instance is PridePower)
        {
            __result = NativeAssetPaths.PrideBigIcon;
        }
        else if (__instance is WishYouGoodLuckPower)
        {
            __result = NativeAssetPaths.WishYouGoodLuckBigIcon;
        }
        else if (__instance is WorldviewPower)
        {
            __result = NativeAssetPaths.WorldviewBigIcon;
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
        else if (__instance is HypePower)
        {
            __result = NativeAssetPaths.HypeBigIcon;
        }
        else if (__instance is DolorisPower)
        {
            __result = NativeAssetPaths.DolorisBigIcon;
        }
        else if (__instance is MortisPower)
        {
            __result = NativeAssetPaths.MortisBigIcon;
        }
        else if (__instance is OblivionisPower)
        {
            __result = NativeAssetPaths.OblivionisBigIcon;
        }
        else if (__instance is TimorisPower)
        {
            __result = NativeAssetPaths.TimorisBigIcon;
        }
        else if (__instance is MantraPower or MonsterDivinityPower)
        {
            __result = NativeAssetPaths.MonsterDivinityBigIcon;
        }
        else if (__instance is KingsPower)
        {
            __result = NativeAssetPaths.KingsBigIcon;
        }
        else if (__instance is CuriosityPower)
        {
            __result = ModelDb.Power<StrengthPower>().ResolvedBigIconPath;
        }
        else if (__instance is EndurancePower)
        {
            __result = NativeAssetPaths.EnduranceBigIcon;
        }
        else if (__instance is FearlessPower)
        {
            __result = NativeAssetPaths.FearlessBigIcon;
        }
        else if (__instance is GodsCreationPower)
        {
            __result = NativeAssetPaths.GodsCreationBigIcon;
        }
        else if (__instance is GirlOfSpringPower)
        {
            __result = NativeAssetPaths.GirlOfSpringBigIcon;
        }
        else if (__instance is OurSongPower)
        {
            __result = NativeAssetPaths.OurSongBigIcon;
        }
        else if (__instance is PerdereOmniaPower)
        {
            __result = NativeAssetPaths.PerdereOmniaBigIcon;
        }
        else if (__instance is PrimoDieInScaenaPower)
        {
            __result = NativeAssetPaths.PrimoDieInScaenaBigIcon;
        }
        else if (__instance is SeizeTheFatePower)
        {
            __result = NativeAssetPaths.SeizeTheFateBigIcon;
        }
        else if (__instance is SharedDestinyPower)
        {
            __result = NativeAssetPaths.SharedDestinyBigIcon;
        }
        else if (__instance is CharismaticFormPower)
        {
            __result = NativeAssetPaths.CharismaticFormBigIcon;
        }
        else if (__instance is SakikoCrueltyPower)
        {
            __result = NativeAssetPaths.CrueltyBigIcon;
        }
        else if (__instance is CrychicPower)
        {
            __result = NativeAssetPaths.CrychicBigIcon;
        }
        else if (__instance is PridePower)
        {
            __result = NativeAssetPaths.PrideBigIcon;
        }
        else if (__instance is WishYouGoodLuckPower)
        {
            __result = NativeAssetPaths.WishYouGoodLuckBigIcon;
        }
        else if (__instance is WorldviewPower)
        {
            __result = NativeAssetPaths.WorldviewBigIcon;
        }
    }
}
