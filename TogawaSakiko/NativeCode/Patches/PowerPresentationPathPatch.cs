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
        else if (__instance is ForeverDreamPower)
        {
            __result = NativeAssetPaths.GodsCreationIcon;
        }
        else if (__instance is KindnessPower)
        {
            __result = NativeAssetPaths.GirlOfSpringIcon;
        }
        else if (__instance is FreeNextCardPower)
        {
            __result = NativeAssetPaths.PrideIcon;
        }
        else if (__instance is StayElegancePower)
        {
            __result = NativeAssetPaths.EnduranceIcon;
        }
        else if (__instance is FireEchoPower)
        {
            __result = NativeAssetPaths.PerdereOmniaIcon;
        }
        else if (__instance is DarkHeavenPower)
        {
            __result = NativeAssetPaths.SeizeTheFateIcon;
        }
        else if (__instance is NextTurnArdorPower)
        {
            __result = NativeAssetPaths.CrychicIcon;
        }
        else if (__instance is EncorePower)
        {
            __result = NativeAssetPaths.CharismaticFormIcon;
        }
        else if (__instance is AnglesPower)
        {
            __result = NativeAssetPaths.KingsIcon;
        }
        else if (__instance is UnrestrainedPower)
        {
            __result = NativeAssetPaths.OurSongIcon;
        }
        else if (__instance is SweetDreamsPower)
        {
            __result = NativeAssetPaths.PrideIcon;
        }
        else if (__instance is AllWillsBecomeOnePower)
        {
            __result = NativeAssetPaths.WorldviewIcon;
        }
        else if (__instance is DolorisPower)
        {
            __result = NativeAssetPaths.DolorisIcon;
        }
        else if (__instance is OblivionisPower)
        {
            __result = NativeAssetPaths.OblivionisIcon;
        }
        else if (__instance is MelodiaPower or MonsterDivinityPower)
        {
            __result = NativeAssetPaths.MonsterDivinityIcon;
        }
        else if (__instance is CuriosityPower or ArdorPower)
        {
            __result = ModelDb.Power<StrengthPower>().PackedIconPath;
        }
        else if (__instance is FearlessPower)
        {
            __result = NativeAssetPaths.FearlessIcon;
        }
        else if (__instance is OurSongPower)
        {
            __result = NativeAssetPaths.OurSongIcon;
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
        else if (__instance is ForeverDreamPower)
        {
            __result = NativeAssetPaths.GodsCreationBigIcon;
        }
        else if (__instance is KindnessPower)
        {
            __result = NativeAssetPaths.GirlOfSpringBigIcon;
        }
        else if (__instance is FreeNextCardPower)
        {
            __result = NativeAssetPaths.PrideBigIcon;
        }
        else if (__instance is StayElegancePower)
        {
            __result = NativeAssetPaths.EnduranceBigIcon;
        }
        else if (__instance is FireEchoPower)
        {
            __result = NativeAssetPaths.PerdereOmniaBigIcon;
        }
        else if (__instance is DarkHeavenPower)
        {
            __result = NativeAssetPaths.SeizeTheFateBigIcon;
        }
        else if (__instance is NextTurnArdorPower)
        {
            __result = NativeAssetPaths.CrychicBigIcon;
        }
        else if (__instance is EncorePower)
        {
            __result = NativeAssetPaths.CharismaticFormBigIcon;
        }
        else if (__instance is AnglesPower)
        {
            __result = NativeAssetPaths.KingsBigIcon;
        }
        else if (__instance is UnrestrainedPower)
        {
            __result = NativeAssetPaths.OurSongBigIcon;
        }
        else if (__instance is SweetDreamsPower)
        {
            __result = NativeAssetPaths.PrideBigIcon;
        }
        else if (__instance is AllWillsBecomeOnePower)
        {
            __result = NativeAssetPaths.WorldviewBigIcon;
        }
        else if (__instance is DolorisPower)
        {
            __result = NativeAssetPaths.DolorisBigIcon;
        }
        else if (__instance is OblivionisPower)
        {
            __result = NativeAssetPaths.OblivionisBigIcon;
        }
        else if (__instance is MelodiaPower or MonsterDivinityPower)
        {
            __result = NativeAssetPaths.MonsterDivinityBigIcon;
        }
        else if (__instance is CuriosityPower or ArdorPower)
        {
            __result = ModelDb.Power<StrengthPower>().ResolvedBigIconPath;
        }
        else if (__instance is FearlessPower)
        {
            __result = NativeAssetPaths.FearlessBigIcon;
        }
        else if (__instance is OurSongPower)
        {
            __result = NativeAssetPaths.OurSongBigIcon;
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
        else if (__instance is ForeverDreamPower)
        {
            __result = NativeAssetPaths.GodsCreationBigIcon;
        }
        else if (__instance is KindnessPower)
        {
            __result = NativeAssetPaths.GirlOfSpringBigIcon;
        }
        else if (__instance is FreeNextCardPower)
        {
            __result = NativeAssetPaths.PrideBigIcon;
        }
        else if (__instance is StayElegancePower)
        {
            __result = NativeAssetPaths.EnduranceBigIcon;
        }
        else if (__instance is FireEchoPower)
        {
            __result = NativeAssetPaths.PerdereOmniaBigIcon;
        }
        else if (__instance is DarkHeavenPower)
        {
            __result = NativeAssetPaths.SeizeTheFateBigIcon;
        }
        else if (__instance is NextTurnArdorPower)
        {
            __result = NativeAssetPaths.CrychicBigIcon;
        }
        else if (__instance is EncorePower)
        {
            __result = NativeAssetPaths.CharismaticFormBigIcon;
        }
        else if (__instance is AnglesPower)
        {
            __result = NativeAssetPaths.KingsBigIcon;
        }
        else if (__instance is UnrestrainedPower)
        {
            __result = NativeAssetPaths.OurSongBigIcon;
        }
        else if (__instance is SweetDreamsPower)
        {
            __result = NativeAssetPaths.PrideBigIcon;
        }
        else if (__instance is AllWillsBecomeOnePower)
        {
            __result = NativeAssetPaths.WorldviewBigIcon;
        }
        else if (__instance is DolorisPower)
        {
            __result = NativeAssetPaths.DolorisBigIcon;
        }
        else if (__instance is OblivionisPower)
        {
            __result = NativeAssetPaths.OblivionisBigIcon;
        }
        else if (__instance is MelodiaPower or MonsterDivinityPower)
        {
            __result = NativeAssetPaths.MonsterDivinityBigIcon;
        }
        else if (__instance is CuriosityPower or ArdorPower)
        {
            __result = ModelDb.Power<StrengthPower>().ResolvedBigIconPath;
        }
        else if (__instance is FearlessPower)
        {
            __result = NativeAssetPaths.FearlessBigIcon;
        }
        else if (__instance is OurSongPower)
        {
            __result = NativeAssetPaths.OurSongBigIcon;
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
        else if (__instance is WorldviewPower)
        {
            __result = NativeAssetPaths.WorldviewBigIcon;
        }
    }
}
