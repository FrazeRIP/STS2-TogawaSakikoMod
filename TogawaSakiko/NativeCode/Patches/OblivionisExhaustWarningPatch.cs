using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldGlowRed), MethodType.Getter)]
internal static class OblivionisExhaustWarningPatch
{
    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (!__result && __instance.IsMutable && __instance.Type == CardType.Attack &&
            __instance.Pile?.Type == PileType.Hand &&
            __instance.Pile.Cards.Any(card => card is OblivionisCard))
        {
            __result = true;
        }
    }
}
