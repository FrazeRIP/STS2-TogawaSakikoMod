using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch]
internal static class CharacterPresentationPathPatch
{
    private static readonly IReadOnlyDictionary<string, string> Paths = new Dictionary<string, string>
    {
        ["get_VisualsPath"] = NativeAssetPaths.CharacterVisualsPreload,
        ["get_IconTexturePath"] = NativeAssetPaths.CharacterIcon,
        ["get_IconOutlineTexturePath"] = NativeAssetPaths.CharacterIconOutline,
        ["get_EnergyCounterPath"] = NativeAssetPaths.CharacterEnergyCounter,
        ["get_MerchantAnimPath"] = NativeAssetPaths.CharacterMerchant,
        ["get_RestSiteAnimPath"] = NativeAssetPaths.CharacterRestSite,
        ["get_ArmPointingTexturePath"] = NativeAssetPaths.CharacterArmPoint,
        ["get_ArmRockTexturePath"] = NativeAssetPaths.CharacterArmRock,
        ["get_ArmPaperTexturePath"] = NativeAssetPaths.CharacterArmPaper,
        ["get_ArmScissorsTexturePath"] = NativeAssetPaths.CharacterArmScissors,
        ["get_CharacterSelectBg"] = NativeAssetPaths.CharacterSelectBackground,
        ["get_CharacterSelectTransitionPath"] = NativeAssetPaths.CharacterTransitionMaterial,
        ["get_TrailPath"] = NativeAssetPaths.CharacterTrail,
        ["get_AttackSfx"] = "event:/sfx/characters/defect/defect_attack",
        ["get_CastSfx"] = "event:/sfx/characters/defect/defect_cast",
        ["get_DeathSfx"] = "event:/sfx/characters/defect/defect_die"
    };

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (string methodName in Paths.Keys)
        {
            yield return AccessTools.Method(typeof(CharacterModel), methodName) ??
                         throw new MissingMethodException(typeof(CharacterModel).FullName, methodName);
        }
    }

    private static void Postfix(CharacterModel __instance, MethodBase __originalMethod, ref string __result)
    {
        if (__instance is SakikoCharacter)
        {
            __result = Paths[__originalMethod.Name];
        }
    }
}
