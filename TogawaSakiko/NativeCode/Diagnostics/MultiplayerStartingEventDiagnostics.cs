using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Potions;
using TogawaSakiko.NativeCode.Models.Relics;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Diagnostics;

// Optional phase for the real ENet harness. Call after assets-ready and before entering combat.
// ShouldSave must be enabled by the caller; every process already has an isolated app-data root.
internal static class MultiplayerStartingEventDiagnostics
{
    private static readonly JsonSerializerOptions EvidenceOptions = new() { WriteIndented = true, IncludeFields = true };
    private static readonly string[] ChoiceKeys = ["ANOTHER_MASK", "THE_THIRD_MOVEMENT", "BLAZING_HAIRBAND"];

    internal static bool Enabled => CommandLineHelper.HasArg("togawa-mp-starting-event");

    internal static async Task RunAsync(RunState state, Func<string, Task> barrier)
    {
        if (!Enabled)
        {
            return;
        }

        RunManager manager = RunManager.Instance;
        Require(manager.NetService.Type is NetGameType.Host or NetGameType.Client,
            "starting event phase requires a real multiplayer service");
        Require(manager.ShouldSave, "enable native saving for the isolated starting-event probe");
        Require(state.ExtraFields.StartedWithNeow, "native starting Neow flag was not initialized");
        int offset = int.TryParse(CommandLineHelper.GetValue("togawa-mp-starting-choice-offset"), out int value) ? value : 0;
        Require(offset is >= 0 and < 3, "starting-choice offset must be 0, 1, or 2");
        string root = CommandLineHelper.GetValue("togawa-mp-artifacts")
            ?? throw new InvalidOperationException("Starting-event artifact root is missing.");
        ulong localId = manager.NetService.NetId;

        await manager.EnterMapCoord(state.Map.StartingMapPoint.coord);
        await Wait(() => manager.EventSynchronizer.Events.Count == state.Players.Count &&
            manager.EventSynchronizer.Events.All(model => model.CurrentOptions.Count == 3), "native Neow options");
        EventRoom room = state.CurrentRoom as EventRoom
            ?? throw new InvalidOperationException("Starting map point did not enter an event room.");
        Require(room.CanonicalEvent.GetType() == typeof(Neow) && !manager.EventSynchronizer.IsShared,
            "multiplayer starting room must be native, independently chosen Neow");
        await barrier("starting-options-ready");
        int invariantAssertions = ValidateRelicAndPotionOwnerInvariants(state.Players);
        await CompareSnapshot(state, root, "options", barrier);

        Player[] sakikoPlayers = state.Players.Where(player => player.Character is SakikoCharacter).ToArray();
        var selected = new Dictionary<ulong, int>();
        using IDisposable selector = CardSelectCmd.UseSelector(new FirstCardSelector(), localOnly: true);
        foreach (Player owner in state.Players)
        {
            EventModel ownerEvent = manager.EventSynchronizer.GetEventForPlayer(owner);
            EventOption[] originalOptions = ownerEvent.CurrentOptions.ToArray();
            Dictionary<ulong, string> unrelatedBefore = state.Players.Where(player => player != owner)
                .ToDictionary(player => player.NetId, InventorySnapshot);
            int choice = owner.Character is SakikoCharacter
                ? (Array.IndexOf(sakikoPlayers, owner) + offset) % 3
                : SelectVanillaChoice(originalOptions);
            selected[owner.NetId] = choice;
            if (owner.Character is SakikoCharacter)
            {
                Require(originalOptions.Select(option => option.TextKey.Split('.').Last()).SequenceEqual(ChoiceKeys),
                    $"player {owner.NetId} fixed options were missing or reordered");
            }
            await barrier($"starting-choice-ready-{owner.NetId}");
            if (LocalContext.IsMe(owner))
            {
                // This is the same network entry point as the native option button.
                // Remote peers receive OptionIndexChosenMessage and never invoke this directly.
                manager.EventSynchronizer.ChooseLocalOption(choice);
            }
            await Wait(() => ownerEvent.IsFinished, $"player {owner.NetId} native starting choice");
            await manager.EventSynchronizer.AwaitPendingOptionTasks();
            await barrier($"starting-choice-finished-{owner.NetId}");
            foreach (Player unrelated in state.Players.Where(player => player != owner))
            {
                Require(unrelatedBefore[unrelated.NetId] == InventorySnapshot(unrelated),
                    $"player {owner.NetId}'s starting choice changed player {unrelated.NetId}'s deck or relics");
            }
            if (owner.Character is SakikoCharacter)
            {
                VerifySakikoChoice(owner, choice);
                string inventory = InventorySnapshot(owner);
                // Reuse stale callback objects to prove the per-event guard spans all three options.
                foreach (EventOption stale in originalOptions)
                {
                    await stale.Chosen();
                }
                Require(inventory == InventorySnapshot(owner), "stale Sakiko options granted another starting reward");
            }
            await CompareSnapshot(state, root, $"choice-{owner.NetId}", barrier);
        }

        await Wait(() => room.IsPreFinished, "all-player ancient pre-finish");
        await manager.EventSynchronizer.AwaitPendingOptionTasks();
        await SaveManager.Instance.SaveRun(room, saveProgress: false);
        await barrier("starting-save-complete");
        bool host = manager.NetService.Type == NetGameType.Host;
        Require(SaveManager.Instance.HasMultiplayerRunSave == host,
            "native multiplayer save must exist only in the isolated host profile");
        SerializableRun saved = manager.ToSave(room);
        if (host)
        {
            ReadSaveResult<SerializableRun> nativeRead = SaveManager.Instance.LoadAndCanonicalizeMultiplayerRunSave(localId);
            Require(nativeRead.Success && nativeRead.SaveData is not null, "native host multiplayer save did not reload");
            saved = nativeRead.SaveData!;
        }
        Require(saved.PreFinishedRoom is { IsPreFinished: true } && saved.PreFinishedRoom.EventId == ModelDb.Event<Neow>().Id,
            "native save lost the completed Neow room identity");
        string saveJson = JsonSerializationUtility.ToJson(saved);
        string savePath = Path.Combine(root, $"peer-{localId}-starting-run.save");
        File.WriteAllText(savePath, saveJson);
        SerializableRun reread = JsonSerializer.Deserialize(File.ReadAllText(savePath),
            JsonSerializationUtility.GetTypeInfo<SerializableRun>())
            ?? throw new InvalidOperationException("Native serialized starting save did not parse.");
        RunState restored = RunState.FromSerializable(reread);
        Require(restored.Players.Select(player => player.NetId).SequenceEqual(state.Players.Select(player => player.NetId)),
            "save reconstruction changed player identities or order");
        foreach (Player restoredPlayer in restored.Players)
        {
            Player original = state.GetPlayer(restoredPlayer.NetId)
                ?? throw new InvalidOperationException("A restored player was not found in the active run.");
            Require(InventorySnapshot(restoredPlayer) == InventorySnapshot(original),
                $"player {restoredPlayer.NetId} inventory changed during native save reconstruction");
            if (restoredPlayer.Character is SakikoCharacter)
            {
                int choice = selected[restoredPlayer.NetId];
                VerifySakikoChoice(restoredPlayer, choice);
                var history = reread.MapPointHistory.SelectMany(act => act).SelectMany(point => point.PlayerStats)
                    .Where(stats => stats.PlayerId == restoredPlayer.NetId).SelectMany(stats => stats.AncientChoices).ToArray();
                Require(history.Length == 3 && history.Count(entry => entry.WasChosen) == 1 &&
                    history.Single(entry => entry.WasChosen).TextKey == ChoiceKeys[choice],
                    $"player {restoredPlayer.NetId} ancient choice history was not saved independently");
                if (restoredPlayer.GetRelic<AnotherMask>() is { } mask)
                {
                    string before = InventorySnapshot(restoredPlayer);
                    await mask.AfterObtained();
                    Require(before == InventorySnapshot(restoredPlayer), "Another Mask repeated its saved conversion");
                }
            }
        }
        await CompareSnapshot(state, root, "saved", barrier);
        File.WriteAllText(Path.Combine(root, $"peer-{localId}-starting-result.json"), JsonSerializer.Serialize(new
        {
            Passed = true,
            PlayerCount = state.Players.Count,
            LocalId = localId,
            NativeEventChoiceTransport = "OptionIndexChosenMessage through EventSynchronizer",
            Choices = state.Players.Select(player => new { Owner = player.NetId, Index = selected[player.NetId],
                Choice = player.Character is SakikoCharacter ? ChoiceKeys[selected[player.NetId]] : "native-vanilla" }),
            HostNativeSaveReloaded = host,
            ClientNativeSaveAbsent = !host,
            NativeSaveReconstructionVerified = true,
            ReconnectSessionVerified = false,
            PeerComparisonExcludesLocalDiscoveryMetadata = true,
            RelicAndPotionOwnerInvariantAssertions = invariantAssertions
        }, EvidenceOptions));
        GD.Print($"Multiplayer starting event PASSED: peer {localId}, choices={state.Players.Count}, owner invariants={invariantAssertions}, native host save={host}.");
        await barrier("starting-phase-complete");
    }

    private static int SelectVanillaChoice(IReadOnlyList<EventOption> options)
    {
        for (int index = 0; index < options.Count; index++)
        {
            if (options[index].Relic is { HasUponPickupEffect: false } and not MegaCrit.Sts2.Core.Models.Relics.ScrollBoxes)
            {
                return index;
            }
        }
        // ScrollBoxes opens a bundle screen with no native local-selector hook.
        // All other Neow card choices below use native deck or choose-one contracts.
        return Enumerable.Range(0, options.Count).First(index =>
            options[index].Relic is not MegaCrit.Sts2.Core.Models.Relics.ScrollBoxes);
    }

    private static void VerifySakikoChoice(Player player, int choice)
    {
        Require(player.Deck.Cards.Count == 9 && player.Deck.Cards.Count(card => card is StrikeTogawaSakiko) == 4 &&
            player.Deck.Cards.Count(card => card is TheMoonlightSonataCard) == 1 &&
            player.Deck.Cards.Count(card => card is DefendTogawaSakiko) == (choice == 0 ? 0 : 4) &&
            player.Deck.Cards.Count(card => card is DesireCard) == (choice == 0 ? 4 : 0),
            $"player {player.NetId} starting deck conversion was not exact");
        Require(choice switch
        {
            0 => player.Relics.Count == 1 && player.GetRelic<AnotherMask>() is { AppliedStartingChange: true },
            1 => player.Relics.Count == 2 && player.GetRelic<StarterRelicTogawaSakiko>() is not null &&
                 player.GetRelic<TheThirdMovement>() is { RemainingUses: 3 },
            2 => player.Relics.Count == 1 && player.GetRelic<BlazingHairband>() is not null,
            _ => false
        }, $"player {player.NetId} starting relic result was not exact");
    }

    private static int ValidateRelicAndPotionOwnerInvariants(IReadOnlyList<Player> players)
    {
        RelicModel[] relics = [ModelDb.Relic<StarterRelicTogawaSakiko>(), ModelDb.Relic<AnotherMask>(),
            ModelDb.Relic<BlazingHairband>(), ModelDb.Relic<ColorfulNotebook>(), ModelDb.Relic<CuteAnimalBandAid>(),
            ModelDb.Relic<FountainDrink>(), ModelDb.Relic<GoldenPocketWatch>(), ModelDb.Relic<MasqueradeMask>(),
            ModelDb.Relic<TheCompass>(), ModelDb.Relic<TheDoll>(), ModelDb.Relic<TheThirdMovement>(),
            ModelDb.Relic<WarmthInfusedPorcelainCup>()];
        PotionModel[] potions = [ModelDb.Potion<ChocolateMilkJelly>(), ModelDb.Potion<EarlGreyTea>(),
            ModelDb.Potion<FreshlySqueezedCucumber>(), ModelDb.Potion<HallucinationPotion>(),
            ModelDb.Potion<MatchaParfait>(), ModelDb.Potion<OrangeMilkJelly>()];
        int assertions = 0;
        foreach (Player owner in players)
        {
            foreach (RelicModel canonical in relics)
            {
                RelicModel mutable = canonical.ToMutable();
                mutable.Owner = owner;
                int ownerSlot = owner.RunState.GetPlayerSlotIndex(owner);
                switch (mutable)
                {
                    case GoldenPocketWatch watch:
                        watch.CardsPlayed = ownerSlot + 3;
                        break;
                    case MasqueradeMask mask:
                        mask.PurgedCards = ownerSlot + 1;
                        break;
                    case TheDoll doll:
                        doll.TurnsElapsed = ownerSlot;
                        break;
                    case TheThirdMovement movement:
                        movement.RemainingUses = ownerSlot % 3;
                        break;
                }
                RelicModel restored = RelicModel.FromSerializable(mutable.ToSerializable());
                restored.Owner = owner;
                Require(mutable != restored && mutable.Owner == owner && restored.Owner == owner && restored.Id == mutable.Id,
                    $"{canonical.Id} owner {owner.NetId} relic save identity");
                assertions++;
                Require(JsonSerializer.Serialize(mutable.ToSerializable(), EvidenceOptions) ==
                    JsonSerializer.Serialize(restored.ToSerializable(), EvidenceOptions),
                    $"{canonical.Id} owner {owner.NetId} saved counters changed during reconstruction");
                assertions++;
            }
            foreach (PotionModel canonical in potions)
            {
                PotionModel mutable = canonical.ToMutable();
                mutable.Owner = owner;
                PotionModel restored = PotionModel.FromSerializable(mutable.ToSerializable(0));
                restored.Owner = owner;
                Require(mutable != restored && mutable.Owner == owner && restored.Owner == owner &&
                    restored.Id == mutable.Id && mutable.TargetType == TargetType.Self,
                    $"{canonical.Id} owner {owner.NetId} potion target/save identity");
                assertions++;
            }
        }
        return assertions;
    }

    private static string InventorySnapshot(Player player) => JsonSerializer.Serialize(new
    {
        Deck = player.Deck.Cards.Select(card => card.ToSerializable()).ToArray(),
        Relics = player.Relics.Select(relic => relic.ToSerializable()).ToArray()
    }, EvidenceOptions);

    private static async Task CompareSnapshot(RunState state, string root, string phase, Func<string, Task> barrier)
    {
        string json = JsonSerializer.Serialize(new
        {
            Players = state.Players.Select(SharedPlayerSnapshot).ToArray(),
            Rng = state.Rng.ToSerializable(),
            History = state.MapPointHistory.SelectMany(act => act).ToArray(),
            EventFinished = RunManager.Instance.EventSynchronizer.Events.Select(model => model.IsFinished).ToArray()
        }, EvidenceOptions);
        ulong local = RunManager.Instance.NetService.NetId;
        File.WriteAllText(Path.Combine(root, $"peer-{local}-starting-state-{phase}.json"), json);
        await barrier($"starting-state-written-{phase}");
        foreach (Player peer in state.Players)
        {
            Require(json == File.ReadAllText(Path.Combine(root, $"peer-{peer.NetId}-starting-state-{phase}.json")),
                $"starting event deck/relic/history/RNG state differs from peer {peer.NetId} at {phase}");
        }
    }

    private static SerializablePlayer SharedPlayerSnapshot(Player player)
    {
        SerializablePlayer snapshot = player.ToSerializable();
        // Native discovery collections track local presentation/progress, so peer copies differ.
        // Normalize only the detached comparison object; native run saves retain every field.
        snapshot.DiscoveredCards = [];
        snapshot.DiscoveredEnemies = [];
        snapshot.DiscoveredEpochs = [];
        snapshot.DiscoveredPotions = [];
        snapshot.DiscoveredRelics = [];
        return snapshot;
    }

    private static async Task Wait(Func<bool> predicate, string operation)
    {
        ulong deadline = Time.GetTicksMsec() + 90000;
        while (!predicate())
        {
            if (Time.GetTicksMsec() > deadline)
            {
                throw new TimeoutException("Multiplayer starting event timed out waiting for " + operation);
            }
            await NGame.Instance!.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Multiplayer starting event contract failed: " + message);
        }
    }

    private sealed class FirstCardSelector : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            CardModel[] available = options.ToArray();
            CardModel[] selected = available.Take(Math.Max(1, minSelect)).ToArray();
            if (available.Length > 0)
            {
                Player owner = available[0].Owner;
                PlayerChoiceSynchronizer synchronizer = RunManager.Instance.PlayerChoiceSynchronizer;
                int slot = owner.RunState.GetPlayerSlotIndex(owner);
                // Native Neow pickup paths reserve a choice before the LocalSelector hook,
                // but only their visible UI branch submits it. Reproduce that owner packet.
                PlayerChoiceResult result = available.All(card => card.Pile?.Type == PileType.Deck)
                    ? PlayerChoiceResult.FromMutableDeckCards(selected)
                    : PlayerChoiceResult.FromIndex(Array.IndexOf(available, selected.FirstOrDefault()));
                synchronizer.SyncLocalChoice(owner, synchronizer.ChoiceIds.ElementAt(slot) - 1, result);
            }
            return Task.FromResult<IEnumerable<CardModel>>(selected);
        }

        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<MegaCrit.Sts2.Core.Entities.CardRewardAlternatives.CardRewardAlternative> alternatives) =>
            new() { card = options.FirstOrDefault()?.Card };
    }
}
