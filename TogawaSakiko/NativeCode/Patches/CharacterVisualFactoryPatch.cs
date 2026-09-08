using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TogawaSakiko.NativeCode.Presentation;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
internal static class CharacterVisualFactoryPatch
{
    private static bool Prefix(CharacterModel __instance, ref NCreatureVisuals __result)
    {
        if (__instance is not SakikoCharacter)
        {
            return true;
        }

        __result = NativeCharacterVisualFactory.Create();
        return false;
    }
}
