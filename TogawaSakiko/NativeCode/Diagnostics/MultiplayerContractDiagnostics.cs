using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Connection;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.GameActions;
using TogawaSakiko.NativeCode.Content;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Diagnostics;

/// <summary>
/// Opt-in, real ENet multi-process contract probe. Files coordinate test phases and collect evidence only;
/// game actions, card choices, rewards, and checksums travel through the game's network services.
/// This does not set TestMode.IsOn, replace the transport, or suppress checksum validation.
/// </summary>
internal static class MultiplayerContractDiagnostics
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, IncludeFields = true };
    private static readonly List<object> Checksums = [];
    private sealed record ChoiceEvidence(ulong Owner, uint Id, string Result);
    private static readonly List<ChoiceEvidence> Choices = [];
    private static readonly Dictionary<ulong, RewardsSet> RewardSets = [];
    internal static Dictionary<string, CardModel> FixtureCards { get; } = [];
    private static INetGameService? _service;
    private static string _root = "";
    private static ulong _id;
    private static int _count;
    private static bool _diverged;
    private static int _sharedContractActionsFinished;
    private static bool _started;

    public static bool Enabled => CommandLineHelper.HasArg("togawa-native-multiplayer-contract") ||
        CommandLineHelper.HasArg("togawa-native-multiplayer-replay");

    public static async Task AfterStartup(Task startup)
    {
        await startup;
        if (_started)
        {
            return;
        }
        _started = true;
        try
        {
            if (CommandLineHelper.HasArg("togawa-native-multiplayer-replay"))
            {
                string path = CommandLineHelper.GetValue("togawa-mp-replay-path")
                    ?? throw new InvalidOperationException("Missing multiplayer replay path.");
                int index = int.Parse(CommandLineHelper.GetValue("togawa-mp-replay-player-index") ?? "0");
                _root = Path.GetDirectoryName(path)!;
                _id = (ulong)index + 1;
                await MultiplayerReplayDiagnostics.RunAsync(path, index);
                NGame.Instance!.GetTree().Quit();
            }
            else
            {
                await RunAsync();
            }
        }
        catch (Exception error)
        {
            GD.PrintErr($"Multiplayer contract FAILED: {error}");
            if (!string.IsNullOrEmpty(_root))
            {
                Write($"peer-{_id}-failure.json", new { Error = error.ToString() });
            }
            NGame.Instance!.GetTree().Quit(1);
        }
    }

    public static void PumpBeforeRun()
    {
        if (Enabled && NRun.Instance is null)
        {
            _service?.Update();
        }
    }

    public static void ObserveRewards(RewardsSet set)
    {
        if (Enabled)
        {
            RewardSets[set.Player.NetId] = set;
        }
    }

    private static async Task RunAsync()
    {
        _root = CommandLineHelper.GetValue("togawa-mp-artifacts")
            ?? throw new InvalidOperationException("Missing isolated multiplayer artifact directory.");
        _id = ulong.Parse(CommandLineHelper.GetValue("togawa-mp-id") ?? "1");
        _count = int.Parse(CommandLineHelper.GetValue("togawa-mp-count") ?? "2");
        string scenario = CommandLineHelper.GetValue("togawa-mp-scenario") ?? "mixed-host";
        string seed = CommandLineHelper.GetValue("seed") ?? "SAKIKOMP001";
        ushort port = ushort.Parse(CommandLineHelper.GetValue("togawa-mp-port") ?? "27851");
        Require(_count is >= 2 and <= 4 && _id >= 1 && _id <= (ulong)_count, "Invalid peer count or identity.");
        Directory.CreateDirectory(_root);
        GD.Print($"Multiplayer contract: content assertions={MultiplayerContentContractTests.Run()}, relic/presentation assertions={MultiplayerRelicPresentationContractTests.Run()}, handshake assertions={NativeHandshakeContractTests.Run()}.");
        NGame.Instance!.GetTree().ProcessFrame += PumpBeforeRun;
        PeerVersionInfo version = PeerVersionInfo.LocalDefault();
        if (_id == 1)
        {
            var host = new NetHostGameService(version);
            _service = host;
            Require(host.StartENetHost(port, _count - 1) is null, "ENet host failed to bind.");
            await Wait(() => host.ConnectedPeers.Count == _count - 1, "ENet peer handshakes");
        }
        else
        {
            var client = new NetClientGameService(version);
            _service = client;
            NetErrorInfo? error = await new ENetClientConnectionInitializer(_id, "127.0.0.1", port).Connect(client);
            Require(error is null, $"ENet connection failed: {error}");
            await Wait(() => client.IsConnected, "game handshake");
        }
        GD.Print($"Multiplayer contract: connected peer {_id}/{_count} using {_service.Type} native ENet.");
        Player[] players = Enumerable.Range(1, _count).Select(index =>
        {
            bool sakiko = scenario switch
            {
                "vanilla" => false,
                "mixed-host" => index == 1,
                "mixed-client" => index == 2,
                "duplicate" => true,
                "four-player" => index is 1 or 3,
                _ => throw new InvalidOperationException($"Unknown multiplayer scenario {scenario}.")
            };
            CharacterModel character = sakiko ? ModelDb.Character<SakikoCharacter>() : ModelDb.Character<Ironclad>();
            return Player.CreateForNewRun(character, UnlockState.all, (ulong)index);
        }).ToArray();
        RunState state = RunState.CreateForNewRun(players,
            ActModel.GetDefaultList().Select(act => act.ToMutable()).ToArray(), [], GameMode.Standard, 0, seed);
        RunManager manager = RunManager.Instance;
        manager.SetUpTest(state, _service, disableCombatStateSync: false, shouldSave: MultiplayerStartingEventDiagnostics.Enabled);
        manager.ChecksumTracker.IsEnabled = true;
        manager.ChecksumTracker.StateDiverged += (_, _) => _diverged = true;
        manager.ChecksumTracker.ChecksumGenerated += (data, context, _) =>
            Checksums.Add(new { Id = data.id, Value = data.checksum, Context = context });
        manager.PlayerChoiceSynchronizer.PlayerChoiceReceived += (player, choice, result) =>
            Choices.Add(new ChoiceEvidence(player.NetId, choice, result.ToString()));
        manager.ActionExecutor.AfterActionExecuted += action =>
        {
            if (action is CombatMultiplayerContractGameAction && action.Exception is null)
            {
                _sharedContractActionsFinished++;
            }
        };
        await Barrier("initialized");
        if (_service is NetHostGameService hostService)
        {
            foreach (Player player in players.Skip(1))
            {
                hostService.SetPeerReadyForBroadcasting(player.NetId);
            }
        }
        manager.Launch();
        await Barrier("launched");
        manager.GenerateRooms();
        await PreloadManager.LoadRunAssets(players.Select(player => player.Character));
        await PreloadManager.LoadActAssets(state.Act);
        await manager.FinalizeStartingRelics();
        NGame.Instance.RootSceneContainer.SetCurrentScene(NRun.Create(state));
        await manager.GenerateMap();
        await Barrier("assets-ready");
        await MultiplayerStartingEventDiagnostics.RunAsync(state, Barrier);
        CombatRoom room = (CombatRoom)await manager.EnterRoomDebug(RoomType.Monster,
            model: ModelDb.Encounter<VineShamblerNormal>().ToMutable(), showTransition: false);
        await Wait(() => manager.ActionQueueSynchronizer.CombatState == ActionSynchronizerCombatState.PlayPhase,
            "first multiplayer play phase");
        await Barrier("play-ready");
        Require(!TestMode.IsOn && _service.Type is NetGameType.Host or NetGameType.Client,
            "Probe must use native game mode and actual multiplayer service.");

        if (scenario == "vanilla")
        {
            await PlayFixtureCard<DefendIronclad>(room, players[0], "vanilla-host");
            await PlayFixtureCard<DefendIronclad>(room, players[1], "vanilla-client");
        }
        else
        {
            Player[] performers = players.Where(player => player.Character is SakikoCharacter).ToArray();
            int accumulatedHype = 0;
            int accumulatedRewards = 0;
            foreach (Player performer in performers)
            {
                await PlayFixtureCard<NovaHistoriaCard>(room, performer, $"hype-{performer.NetId}");
                accumulatedHype += 2;
                Require(players.All(player => player.Creature.GetPower<HypePower>()?.Amount == accumulatedHype),
                    "Team Hype must stack exactly once per source for every living teammate.");
                await Snapshot(state, room, $"hype-{performer.NetId}");
                await PlayFixtureCard<MomentMemoryCard>(room, performer, $"purge-{performer.NetId}");
                accumulatedRewards++;
                Require(players.All(player => room.ExtraRewards.TryGetValue(player, out var rewards) &&
                    rewards.OfType<CardRemovalReward>().Count() == accumulatedRewards),
                    "Team purge must register exactly one native removal reward per player per play.");
                await Snapshot(state, room, $"purge-{performer.NetId}");
            }
            if (LocalContext.IsMe(players[0]))
            {
                manager.ActionQueueSynchronizer.RequestEnqueue(new CombatMultiplayerContractGameAction(players[0]));
            }
            await Wait(() => _sharedContractActionsFinished == 1, "shared combat authority, deck, ledger, and Hype contracts");
            await Barrier("shared-combat-contracts");
        }

        await Snapshot(state, room, "combat-final");
        manager.CombatReplayWriter.WriteReplay(Path.Combine(_root, $"peer-{_id}.replay").Replace('\\', '/'), false);
        File.WriteAllText(Path.Combine(_root, $"peer-{_id}.replay.package"), NativePackageIdentity.HandshakeEntry);
        await PlayFixtureCard<Whirlwind>(room, players[0], "victory", enemyTarget: false);
        await Wait(() => !CombatManager.Instance.IsInProgress && RewardSets.Count == _count, "victory reward sets");
        await Barrier("rewards-ready");
        int[] originalDeckCounts = players.Select(player => player.Deck.Cards.Count).ToArray();
        if (scenario != "vanilla")
        {
            using IDisposable selector = CardSelectCmd.UseSelector(new FirstCardSelector(), localOnly: true);
            int expectedRemovals = players.Count(player => player.Character is SakikoCharacter);
            foreach (Player owner in players)
            {
                if (LocalContext.IsMe(owner))
                {
                    foreach (CardRemovalReward reward in RewardSets[owner.NetId].Rewards.OfType<CardRemovalReward>().ToArray())
                    {
                        Require(await manager.RewardsSetSynchronizer.SelectLocalReward(reward), "Native purge reward was not selected.");
                    }
                }
                int slot = Array.IndexOf(players, owner);
                await Wait(() => owner.Deck.Cards.Count == originalDeckCounts[slot] - expectedRemovals,
                    $"peer {owner.NetId} synchronized purge choices");
                await Barrier($"reward-owner-{owner.NetId}");
            }
        }
        await Snapshot(state, room, "post-rewards");
        await Barrier("complete");
        Require(!_diverged, "Native checksum tracker reported state divergence.");
        Write($"peer-{_id}-result.json", new
        {
            Passed = true, Scenario = scenario, PeerId = _id, PlayerCount = _count,
            Transport = "Native ENet loopback, separate OS processes", TestMode = TestMode.IsOn,
            NativeChecksums = Checksums, PlayerChoices = Choices,
            ReplayCaptured = true, ReplayPlaybackVerified = false, ReconnectVerified = false
        });
        GD.Print($"Multiplayer contract PASSED: {scenario} peer {_id}/{_count}.");
        await Barrier("results-written");
        NGame.Instance.GetTree().Quit();
    }

    private static async Task PlayFixtureCard<T>(CombatRoom room, Player owner, string phase, bool enemyTarget = false)
        where T : CardModel
    {
        if (LocalContext.IsMe(owner))
        {
            RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(
                new MultiplayerFixtureGameAction(owner, ModelDb.Card<T>().Id, phase, finishCombat: phase == "victory"));
        }
        await Wait(() => FixtureCards.ContainsKey(phase) && !RunManager.Instance.ActionExecutor.IsRunning,
            $"recorded card fixture {phase}");
        CardModel card = FixtureCards[phase];
        await Barrier($"card-ready-{phase}");
        if (LocalContext.IsMe(owner))
        {
            RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(
                new PlayCardAction(card, enemyTarget ? room.CombatState.Enemies.First() : null));
        }
        await Wait(() => card.Pile?.Type != PileType.Hand, $"native card action {phase}");
        await Wait(() => !RunManager.Instance.ActionExecutor.IsRunning, $"native action completion {phase}");
        await Barrier($"card-played-{phase}");
        Require(card.Pile?.Type is PileType.Discard or PileType.Exhaust || !CombatManager.Instance.IsInProgress,
            $"Card {phase} did not complete its native play.");
    }

    private static async Task Snapshot(RunState state, CombatRoom room, string phase)
    {
        await Barrier($"snapshot-{phase}");
        var packet = new PacketWriter();
        NetFullCombatState.FromRun(state, null).Serialize(packet);
        string nativeHash = Convert.ToHexString(SHA256.HashData(packet.Buffer.AsSpan(0, packet.BytePosition)));
        object snapshot = new
        {
            NativeStateSha256 = nativeHash,
            Players = state.Players.Select(player => new
            {
                Id = player.NetId, Character = player.Character.Id.ToString(),
                Hp = player.Creature.CurrentHp, Block = player.Creature.Block,
                Deck = player.Deck.Cards.Select(card => card.ToSerializable()).ToArray(),
                Hype = player.Creature.GetPower<HypePower>()?.Amount ?? 0,
                PurgeRewards = room.ExtraRewards.TryGetValue(player, out var rewards) ? rewards.OfType<CardRemovalReward>().Count() : 0,
                Choices = Choices.Where(choice => choice.Owner == player.NetId).OrderBy(choice => choice.Id).ToArray()
            }).ToArray()
        };
        Write($"peer-{_id}-state-{phase}.json", snapshot);
        await Barrier($"snapshot-written-{phase}");
        string local = File.ReadAllText(Path.Combine(_root, $"peer-{_id}-state-{phase}.json"));
        foreach (int peer in Enumerable.Range(1, _count))
        {
            Require(local == File.ReadAllText(Path.Combine(_root, $"peer-{peer}-state-{phase}.json")),
                $"State, RNG, reward, deck, or choice divergence from peer {peer} at {phase}.");
        }
    }

    private static async Task Barrier(string phase)
    {
        File.WriteAllText(Path.Combine(_root, $"peer-{_id}-{phase}.ready"), "ready");
        await Wait(() => Enumerable.Range(1, _count).All(peer =>
            File.Exists(Path.Combine(_root, $"peer-{peer}-{phase}.ready"))), $"barrier {phase}");
    }

    private static async Task Wait(Func<bool> predicate, string operation)
    {
        ulong deadline = Time.GetTicksMsec() + 90000;
        while (!predicate())
        {
            Require(!_diverged, $"Native checksum divergence while waiting for {operation}.");
            if (Time.GetTicksMsec() > deadline)
            {
                throw new TimeoutException($"Multiplayer contract timed out waiting for {operation}.");
            }
            await NGame.Instance!.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void Write(string filename, object data) =>
        File.WriteAllText(Path.Combine(_root, filename), JsonSerializer.Serialize(data, JsonOptions));

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class FirstCardSelector : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            CardModel[] cards = options.Take(Math.Max(1, minSelect)).ToArray();
            if (cards.Length > 0)
            {
                Player owner = cards[0].Owner;
                PlayerChoiceSynchronizer choices = RunManager.Instance.PlayerChoiceSynchronizer;
                int slot = owner.RunState.Players.ToList().IndexOf(owner);
                // Native FromDeckGeneric's LocalSelector hook omits the UI branch's SyncLocalChoice call.
                // Reproduce that owning UI submission so remote peers still consume a real network choice.
                choices.SyncLocalChoice(owner, choices.ChoiceIds.ElementAt(slot) - 1,
                    PlayerChoiceResult.FromMutableDeckCards(cards));
            }
            return Task.FromResult<IEnumerable<CardModel>>(cards);
        }

        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<MegaCrit.Sts2.Core.Entities.CardRewardAlternatives.CardRewardAlternative> alternatives) =>
            new() { card = options.FirstOrDefault()?.Card };
    }
}
