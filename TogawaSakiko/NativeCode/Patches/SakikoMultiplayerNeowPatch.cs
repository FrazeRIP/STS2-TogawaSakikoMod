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
    private sealed class SelectionState
    {
        internal bool Started;
    }

    private static readonly ConditionalWeakTable<Neow, SelectionState> Selections = new();
    private static readonly MethodInfo FinishAncient = AccessTools.Method(typeof(AncientEventModel), "Done")
        ?? throw new MissingMethodException(typeof(AncientEventModel).FullName, "Done");

    private static bool Prefix(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.GetType() != typeof(Neow) || !ShouldOfferFixedChoices(__instance.Owner))
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
    [
        CreateOption<AnotherMask>(neow, "ANOTHER_MASK"),
        CreateOption<TheThirdMovement>(neow, "THE_THIRD_MOVEMENT"),
        CreateOption<BlazingHairband>(neow, "BLAZING_HAIRBAND")
    ];

    private static EventOption CreateOption<T>(Neow neow, string choice) where T : RelicModel
    {
        string key = OceanOfMemories.Entry + ".pages.INITIAL.options." + choice;
        return new EventOption(neow, () => ChooseAsync<T>(neow),
                new LocString("events", key + ".title"),
                new LocString("events", key + ".description"), key, [])
            .WithRelic<T>(neow.Owner);
    }

    private static async Task ChooseAsync<T>(Neow neow) where T : RelicModel
    {
        neow.AssertMutable();
        SelectionState selection = Selections.GetValue(neow, _ => new SelectionState());
        if (selection.Started || neow.IsFinished)
        {
            return;
        }

        selection.Started = true;
        await RelicCmd.Obtain(ModelDb.Relic<T>().ToMutable(), neow.Owner!);
        // Done updates that owner's AncientChoices before EventRoom saves once all
        // players finish. Reconstructed pre-finished Neow never reruns this callback.
        FinishAncient.Invoke(neow, null);
    }
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
