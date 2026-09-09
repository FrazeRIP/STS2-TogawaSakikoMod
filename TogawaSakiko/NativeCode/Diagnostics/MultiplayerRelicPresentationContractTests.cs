using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Events;
using TogawaSakiko.NativeCode.Models.Relics;
using TogawaSakiko.NativeCode.Patches;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Diagnostics;

// Isolated model-level contracts. These do not claim a transport, reconnect, or replay test.
internal static class MultiplayerRelicPresentationContractTests
{
    internal static int Run()
    {
        int assertions = 0;
        void Require(bool passed, string message)
        {
            assertions++;
            if (!passed)
            {
                throw new InvalidOperationException("Multiplayer relic/presentation contract failed: " + message);
            }
        }

        Player first = Player.CreateForNewRun<SakikoCharacter>(UnlockState.all, 8101);
        Player vanilla = Player.CreateForNewRun<Ironclad>(UnlockState.all, 8102);
        Player second = Player.CreateForNewRun<SakikoCharacter>(UnlockState.all, 8103);
        RunState run = RunState.CreateForTest([first, vanilla, second], seed: "SAKIKOMULTINEOW");
        run.ExtraFields.StartedWithNeow = true;
        Require(SakikoMultiplayerNeowPatch.ShouldOfferFixedChoices(first), "first Sakiko choices");
        Require(SakikoMultiplayerNeowPatch.ShouldOfferFixedChoices(second), "duplicate Sakiko choices");
        Require(!SakikoMultiplayerNeowPatch.ShouldOfferFixedChoices(vanilla), "vanilla choices preserved");
        Require(!SakikoStartingRoomPatch.ShouldReplace(run), "multiplayer room retains native Neow identity");

        Neow firstEvent = CreateNeow(first);
        Neow secondEvent = CreateNeow(second);
        Neow vanillaEvent = CreateNeow(vanilla);
        IReadOnlyList<EventOption> firstOptions = GenerateOptions(firstEvent);
        IReadOnlyList<EventOption> secondOptions = GenerateOptions(secondEvent);
        IReadOnlyList<EventOption> vanillaOptions = GenerateOptions(vanillaEvent);
        string[] expected = ["ANOTHER_MASK", "THE_THIRD_MOVEMENT", "BLAZING_HAIRBAND"];
        Require(firstOptions.Select(option => option.TextKey.Split('.').Last()).SequenceEqual(expected),
            "fixed choice ordering");
        Require(secondOptions.Select(option => option.TextKey).SequenceEqual(firstOptions.Select(option => option.TextKey)),
            "duplicate Sakiko option parity");
        Require(firstOptions.All(option => ReferenceEquals(option.Relic?.Owner, first)) &&
                secondOptions.All(option => ReferenceEquals(option.Relic?.Owner, second)), "relic preview owner isolation");
        Require(vanillaOptions.Count == 3 && vanillaOptions.All(option =>
                !option.TextKey.StartsWith(OceanOfMemories.Entry, StringComparison.Ordinal)), "native vanilla options");
        Require(firstOptions.All(option => option.Title.Exists() && option.Description.Exists()), "fixed option localization");
        Require(firstEvent.DialogueSet.FirstVisitEverDialogue is null, "local Sakiko visit dialogue");
        Require(ModelDb.Event<Neow>().DialogueSet.FirstVisitEverDialogue is not null,
            "canonical Neow introduction preserved");

        WarmthInfusedPorcelainCup cup = (WarmthInfusedPorcelainCup)ModelDb.Relic<WarmthInfusedPorcelainCup>().ToMutable();
        cup.Owner = first;
        SerializableRng before = vanilla.PlayerRng.Rewards.ToSerializable();
        Require(!cup.TryModifyCardRewardOptions(vanilla, [], CardCreationOptions.ForRoom(vanilla, RoomType.Monster)),
            "remote Cup rejects another player's reward");
        Require(before == vanilla.PlayerRng.Rewards.ToSerializable(), "remote Cup leaves entire reward RNG state unchanged");
        Require(typeof(AnotherMask).GetProperty(nameof(AnotherMask.AppliedStartingChange))!
                .IsDefined(typeof(SavedPropertyAttribute), inherit: true), "Another Mask conversion guard is saved");

        Require(SakikoAudioCmd.ShouldPlayOwnerVoice(true, first.NetId, first.NetId), "owner voice enabled");
        Require(!SakikoAudioCmd.ShouldPlayOwnerVoice(true, first.NetId, second.NetId), "remote duplicate voice silent");
        Require(!SakikoAudioCmd.ShouldPlayOwnerVoice(false, vanilla.NetId, vanilla.NetId), "vanilla voice untouched");
        Require(!SakikoAudioCmd.ShouldPlayOwnerVoice(true, first.NetId, null), "no stale local owner voice");
        return assertions;
    }

    private static Neow CreateNeow(Player player)
    {
        Neow neow = (Neow)ModelDb.Event<Neow>().ToMutable();
        AccessTools.Property(typeof(EventModel), nameof(EventModel.Owner)).SetValue(neow, player);
        AccessTools.Property(typeof(EventModel), nameof(EventModel.Rng)).SetValue(neow, new Rng(7321));
        return neow;
    }

    private static IReadOnlyList<EventOption> GenerateOptions(Neow neow) =>
        (IReadOnlyList<EventOption>)AccessTools.Method(typeof(Neow), "GenerateInitialOptions").Invoke(neow, null)!;
}
