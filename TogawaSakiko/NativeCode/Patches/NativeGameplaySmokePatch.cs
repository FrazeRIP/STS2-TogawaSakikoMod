using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Models.Cards;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(NGame), nameof(NGame.IsReleaseGame))]
internal static class NativeGameplaySmokePatch
{
    private static void Postfix(ref bool __result)
    {
        if (NativeSmokeTrace.AutoSlayEnabled)
        {
            __result = false;
        }
    }
}

[HarmonyPatch(
    typeof(Player),
    nameof(Player.CreateForNewRun),
    [typeof(CharacterModel), typeof(UnlockState), typeof(ulong)])]
internal static class NativeGameplaySmokeDeckPatch
{
    private static void Postfix(Player __result)
    {
        if (!NativeSmokeTrace.Enabled || __result.Character is not SakikoCharacter ||
            __result.Deck.Cards.Any(card => card is ASplitMomentCard))
        {
            return;
        }

        CardModel splitMoment = ModelDb.Card<ASplitMomentCard>().ToMutable();
        splitMoment.FloorAddedToDeck = 1;
        __result.Deck.AddInternal(splitMoment, silent: true);
        NativeSmokeTrace.Info("injected A Split Moment into the diagnostic run deck.");
    }
}
