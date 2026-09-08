using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using TogawaSakiko.NativeCode.Diagnostics;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
internal static class CharacterRegistryPatch
{
    private static void Postfix(ref IEnumerable<CharacterModel> __result)
    {
        if (NativeSmokeTrace.ModelDbInitialized &&
            (NativeSmokeTrace.Enabled || NativeSmokeTrace.N7FullRunEnabled || NativeSmokeTrace.StartingRoomEnabled))
        {
            __result = [ModelDb.Character<SakikoCharacter>()];
            return;
        }

        if (NativeSmokeTrace.ModelDbInitialized && NativeSmokeTrace.VanillaGameplayEnabled)
        {
            __result = [ModelDb.Character<Ironclad>()];
            return;
        }

        if (__result.Any(character => character is SakikoCharacter))
        {
            return;
        }

        __result = __result.Append(ModelDb.Character<SakikoCharacter>());
    }
}
