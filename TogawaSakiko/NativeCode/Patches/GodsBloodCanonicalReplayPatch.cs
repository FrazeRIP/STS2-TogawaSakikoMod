using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Patches;

// The library reads the immutable model, before AfterCloned initializes replay.
[HarmonyPatch(typeof(CardModel), nameof(CardModel.BaseReplayCount), MethodType.Getter)]
internal static class GodsBloodCanonicalReplayPatch
{
    private static void Postfix(CardModel __instance, ref int __result)
    {
        if (__instance is GodsBloodCard && !__instance.IsMutable)
        {
            __result = 2;
        }
    }
}
