using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Models.Events;

// One state per mutable owner event, also on remote peers. No state belongs to the room or canonical model.
internal static class SakikoStartingRewards
{
    internal const string NormalBlessing = "NORMAL_BLESSING";
    internal const string SpeechIconPath = "res://TogawaSakiko/images/ui/normal_blessing.svg";
    internal const string NormalOptionKey = OceanOfMemories.Entry + ".pages.INITIAL.options." + NormalBlessing;

    private enum Stage { Custom, Normal, Completing }

    private sealed class SelectionState
    {
        internal Stage Stage;
        internal bool GeneratingNative;
        internal IReadOnlyList<EventOption>? NormalOptions;
    }

    private static readonly ConditionalWeakTable<Neow, SelectionState> Selections = new();
    private static readonly MethodInfo GenerateNative = AccessTools.Method(typeof(Neow), "GenerateInitialOptions");
    private static readonly MethodInfo SetState = AccessTools.Method(typeof(EventModel), "SetEventState");
    private static readonly MethodInfo FinishAncient = AccessTools.Method(typeof(AncientEventModel), "Done");
    private static readonly PropertyInfo HistoryOptions = AccessTools.Property(typeof(AncientEventModel), "GeneratedOptions");

    internal static bool IsGeneratingNative(Neow neow) =>
        Selections.TryGetValue(neow, out SelectionState? state) && state.GeneratingNative;

    // Catalog enumeration must never roll the native rewards or access mutable event RNG.
    internal static IReadOnlyList<EventOption> CreateOptions(Neow neow) =>
    [
        CreateRelicOption<AnotherMask>(neow, "ANOTHER_MASK"),
        CreateRelicOption<BlazingHairband>(neow, "BLAZING_HAIRBAND"),
        new EventOption(neow, () => ChooseAsync(neow, NormalBlessing),
            new LocString("events", NormalOptionKey + ".title"),
            new LocString("events", NormalOptionKey + ".description"), NormalOptionKey, [])
            .ThatWontSaveToChoiceHistory()
    ];

    private static EventOption CreateRelicOption<T>(Neow neow, string choice) where T : RelicModel
    {
        string key = OceanOfMemories.Entry + ".pages.INITIAL.options." + choice;
        return new EventOption(neow, () => ChooseAsync(neow, choice),
            new LocString("events", key + ".title"), new LocString("events", key + ".description"), key, [])
            .WithRelic<T>(neow.Owner);
    }

    internal static async Task ChooseAsync(Neow neow, string choice)
    {
        neow.AssertMutable();
        SelectionState state = Selections.GetValue(neow, _ => new SelectionState());
        if (neow.IsFinished || state.Stage != Stage.Custom)
        {
            return;
        }

        if (choice == NormalBlessing)
        {
            IReadOnlyList<EventOption> options = GetNormalOptions(neow);
            state.Stage = Stage.Normal;
            HistoryOptions.SetValue(neow, options.ToList());
            SetState.Invoke(neow, [neow.InitialDescription, options]);
            return;
        }

        RelicModel relic = choice switch
        {
            "ANOTHER_MASK" => ModelDb.Relic<AnotherMask>().ToMutable(),
            "BLAZING_HAIRBAND" => ModelDb.Relic<BlazingHairband>().ToMutable(),
            _ => throw new ArgumentOutOfRangeException(nameof(choice), choice, "Unknown Sakiko starting choice.")
        };
        state.Stage = Stage.Completing;
        HistoryOptions.SetValue(neow, neow.CurrentOptions.Where(option => option.Relic != null).ToList());
        // Its native obtain hook removes Monochrome Hairband.
        await RelicCmd.Obtain(relic, neow.Owner!);
        // Done updates that owner's AncientChoices before EventRoom saves once all
        // players finish. Reconstructed pre-finished Neow never reruns this callback.
        FinishAncient.Invoke(neow, null);
    }

    internal static IReadOnlyList<EventOption> GetNormalOptions(Neow neow)
    {
        neow.AssertMutable();
        SelectionState state = Selections.GetValue(neow, _ => new SelectionState());
        if (state.NormalOptions != null)
        {
            return state.NormalOptions;
        }
        state.GeneratingNative = true;
        try
        {
            // The subclass calls base explicitly; MethodInfo.Invoke would dispatch its override again.
            IReadOnlyList<EventOption> native = neow is OceanOfMemories ocean
                ? ocean.GenerateNormalOptions()
                : (IReadOnlyList<EventOption>)GenerateNative.Invoke(neow, null)!;
            state.NormalOptions = native.Select(option => GuardNormalOption(neow, option)).ToArray();
            return state.NormalOptions;
        }
        finally
        {
            state.GeneratingNative = false;
        }
    }

    private static EventOption GuardNormalOption(Neow neow, EventOption native)
    {
        EventOption guarded = new(neow, native.IsLocked ? null : Choose,
            native.Title, native.Description, native.TextKey, native.HoverTips);
        if (native.Relic != null)
        {
            guarded.WithRelic(native.Relic);
        }
        if (native.WillKillPlayer != null)
        {
            AccessTools.Property(typeof(EventOption), nameof(EventOption.WillKillPlayer)).SetValue(guarded, native.WillKillPlayer);
        }
        return guarded;

        async Task Choose()
        {
            SelectionState state = Selections.GetValue(neow, _ => new SelectionState());
            if (neow.IsFinished || state.Stage != Stage.Normal)
            {
                return;
            }
            state.Stage = Stage.Completing;
            await native.Chosen();
        }
    }
}
