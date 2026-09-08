using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Random;
using TogawaSakiko.NativeCode.Models.Modifiers;
using TogawaSakiko.NativeCode.Models.Events;
using TogawaSakiko.NativeCode.Patches;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class StartingOptionsDiagnostics
{
    internal static void Validate(Player player)
    {
        ModifierModel[] originalModifiers = player.RunState.Modifiers.ToArray();
        Require(originalModifiers.Length == 1 && originalModifiers[0] is KingsRewardCarrierModifier,
            "standard Sakiko run must contain only its internal Kings carrier");

        Neow neow = (Neow)ModelDb.Event<Neow>().ToMutable();
        AccessTools.Property(typeof(EventModel), nameof(EventModel.Owner)).SetValue(neow, player);
        AccessTools.Property(typeof(EventModel), nameof(EventModel.Rng)).SetValue(neow, new Rng(7321));
        IReadOnlyList<EventOption> options = (IReadOnlyList<EventOption>)
            AccessTools.Method(typeof(Neow), "GenerateInitialOptions").Invoke(neow, null)!;
        Require(options.Count == 3 && options.All(option => option.Relic is not null),
            "internal Kings carrier suppressed native Neow's three starting relic choices");
        Require(neow.InitialDescription.LocEntryKey == "NEOW.pages.INITIAL.description",
            "internal Kings carrier changed native Neow's starting description");
        Require(player.RunState.Modifiers.SequenceEqual(originalModifiers),
            "generating Neow choices mutated the persistent Kings carrier");

        OceanOfMemories ocean = (OceanOfMemories)ModelDb.Event<OceanOfMemories>().ToMutable();
        AccessTools.Property(typeof(EventModel), nameof(EventModel.Owner)).SetValue(ocean, player);
        AccessTools.Property(typeof(EventModel), nameof(EventModel.Rng)).SetValue(ocean, new Rng(7321));
        IReadOnlyList<EventOption> oceanOptions = (IReadOnlyList<EventOption>)
            AccessTools.Method(typeof(OceanOfMemories), "GenerateInitialOptions").Invoke(ocean, null)!;
        Require(oceanOptions.Select(option => option.TextKey.Split('.').Last())
                .SequenceEqual(new[] { "ANOTHER_MASK", "THE_THIRD_MOVEMENT", "BLAZING_HAIRBAND" }),
            "Sakiko's three fixed starting options were missing or reordered");
        Require(ocean.InitialDescription.LocEntryKey == "NEOW.pages.INITIAL.description",
            "Sakiko's starting room did not reuse Neow's description");
        foreach (uint seed in new uint[] { 1, 42, 987654 })
        {
            AccessTools.Property(typeof(EventModel), nameof(EventModel.Rng)).SetValue(ocean, new Rng(seed));
            IReadOnlyList<EventOption> repeated = (IReadOnlyList<EventOption>)
                AccessTools.Method(typeof(OceanOfMemories), "GenerateInitialOptions").Invoke(ocean, null)!;
            Require(repeated.Select(option => option.TextKey).SequenceEqual(oceanOptions.Select(option => option.TextKey)),
                "Sakiko's fixed options changed with the event seed");
        }
        Require(oceanOptions.All(option => option.Title.Exists() && option.Description.Exists()),
            "Sakiko's starting choices had missing localization");

        ModifierModel draft = ModelDb.Modifier<Draft>().ToMutable();
        ModifierModel vintage = ModelDb.Modifier<Vintage>().ToMutable();
        ModifierModel[] realModifiers = [draft, vintage];
        Require(ReferenceEquals(KingsRewardCarrierNeowPatch.VisibleModifiers(realModifiers), realModifiers),
            "a vanilla custom-run modifier list was replaced");
        ModifierModel[] mixedModifiers = [draft, originalModifiers[0], vintage];
        Require(KingsRewardCarrierNeowPatch.VisibleModifiers(mixedModifiers).SequenceEqual(realModifiers),
            "filtering the Kings carrier removed or reordered real run modifiers");
        Require(mixedModifiers.Length == 3 && ReferenceEquals(mixedModifiers[1], originalModifiers[0]),
            "filtering the Kings carrier mutated its source modifier list");

        NativeSmokeTrace.N5Info(
            "Starting options contract passed. SakikoChoices=3, VanillaNeowChoices=3, KingsCarrier=retained, RealModifiers=preserved.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Starting options contract failed: " + message + ".");
        }
    }
}
