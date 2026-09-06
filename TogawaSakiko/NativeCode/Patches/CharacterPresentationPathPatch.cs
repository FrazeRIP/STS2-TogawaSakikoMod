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
        ["get_IconOutlineTexturePath"] = "res://images/ui/top_panel/character_icon_defect_outline.png",
        ["get_EnergyCounterPath"] = "res://scenes/combat/energy_counters/defect_energy_counter.tscn",
        ["get_MerchantAnimPath"] = "res://scenes/merchant/characters/defect_merchant.tscn",
        ["get_RestSiteAnimPath"] = "res://scenes/rest_site/characters/defect_rest_site.tscn",
        ["get_ArmPointingTexturePath"] = "res://images/ui/hands/multiplayer_hand_defect_point.png",
        ["get_ArmRockTexturePath"] = "res://images/ui/hands/multiplayer_hand_defect_rock.png",
        ["get_ArmPaperTexturePath"] = "res://images/ui/hands/multiplayer_hand_defect_paper.png",
        ["get_ArmScissorsTexturePath"] = "res://images/ui/hands/multiplayer_hand_defect_scissors.png",
        ["get_CharacterSelectBg"] = NativeAssetPaths.CharacterSelectBackground,
        ["get_CharacterSelectTransitionPath"] = "res://materials/transitions/defect_transition_mat.tres",
        ["get_TrailPath"] = "res://scenes/vfx/card_trail_defect.tscn",
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
