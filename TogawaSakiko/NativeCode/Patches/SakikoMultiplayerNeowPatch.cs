using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Models.Events;
using TogawaSakiko.NativeCode.Models.Relics;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

// Native EventSynchronizer creates one mutable Neow per player on every peer and
// routes OptionIndexChosenMessage to that player's instance. Retaining the shared
// room's NEOW identity preserves vanilla dialogue, choices, RNG, and save history.
[HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
internal static class SakikoMultiplayerNeowPatch
{
    private static bool Prefix(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.GetType() != typeof(Neow) || !ShouldOfferFixedChoices(__instance.Owner) ||
            SakikoStartingRewards.IsGeneratingNative(__instance))
        {
            return true;
        }

        __result = CreateOptions(__instance);
        return false;
    }

    internal static bool ShouldOfferFixedChoices(Player? player) =>
        player?.Character is SakikoCharacter &&
        player.RunState.Players.Count > 1 &&
        player.RunState.GameMode == GameMode.Standard &&
        player.RunState.CurrentActIndex == 0 &&
        player.RunState.ExtraFields.StartedWithNeow &&
        KingsRewardCarrierNeowPatch.VisibleModifiers(player.RunState.Modifiers).Count == 0;

    internal static IReadOnlyList<EventOption> CreateOptions(Neow neow) =>
        SakikoStartingRewards.CreateOptions(neow);

}

[HarmonyPatch(typeof(AncientEventModel), nameof(AncientEventModel.DialogueSet), MethodType.Getter)]
internal static class SakikoMultiplayerNeowDialoguePatch
{
    private static void Postfix(AncientEventModel __instance, ref AncientDialogueSet __result)
    {
        if (__instance.GetType() == typeof(Neow) &&
            SakikoMultiplayerNeowPatch.ShouldOfferFixedChoices(__instance.Owner))
        {
            // Only this owner's returned dialogue view skips the generic first-ever
            // introduction. Never mutate Neow's canonical or another player's set.
            __result = new AncientDialogueSet
            {
                FirstVisitEverDialogue = null,
                CharacterDialogues = __result.CharacterDialogues,
                AgnosticDialogues = __result.AgnosticDialogues
            };
        }
    }
}
