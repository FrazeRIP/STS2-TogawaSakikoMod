using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Modifiers;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Models.Relics;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class KingsLifecycleDiagnostics
{
    private sealed class FirstRewardCardSelector : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(
            IEnumerable<CardModel> options,
            int minSelect,
            int maxSelect)
        {
            return Task.FromResult<IEnumerable<CardModel>>(options.Take(maxSelect));
        }

        public CardRewardSelection GetSelectedCardReward(
            IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<CardRewardAlternative> alternatives)
        {
            return new CardRewardSelection
            {
                card = options.FirstOrDefault()?.Card
            };
        }
    }

    private sealed class Session
    {
        public bool Armed { get; set; }

        public bool AfterCombatEndObserved { get; set; }

        public bool AfterCombatVictoryObserved { get; set; }

        public bool RewardModified { get; set; }

        public bool RewardCleared { get; set; }
    }

    private static readonly ConditionalWeakTable<Player, Session> Sessions = new();
    private static bool _reloadRewardTakenObserved;

    public static async Task ArmAsync(PlayerChoiceContext choiceContext, CardModel sourceCard)
    {
        if (!NativeSmokeTrace.KingsContractEnabled)
        {
            return;
        }

        Player player = sourceCard.Owner;
        Session session = Sessions.GetValue(player, _ => new Session());
        if (session.Armed)
        {
            return;
        }

        KingsRewardCarrierModifier carrier = player.RunState.Modifiers
            .OfType<KingsRewardCarrierModifier>()
            .SingleOrDefault()
            ?? throw Failure("the hidden native Kings run carrier was not injected");
        Require(!carrier.IsPending(player.NetId),
            "the hidden native Kings run carrier started with stale pending state");
        NativeSmokeTrace.KingsInfo("verified one hidden native run carrier for the Sakiko player.");

        CombatState combatState = sourceCard.CombatState as CombatState
            ?? player.Creature.CombatState as CombatState
            ?? throw Failure("could not resolve the active combat while arming Kings");
        await PowerCmd.Remove(player.Creature.GetPower<KingsPower>());
        KingsCard kingsSource = combatState.CreateCard<KingsCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(
            kingsSource,
            PileType.Exhaust,
            player,
            CardPilePosition.Bottom);
        KingsPower applied = await PowerCmd.Apply<KingsPower>(
                choiceContext,
                player.Creature,
                1m,
                player.Creature,
                kingsSource)
            ?? throw Failure("native PowerCmd.Apply did not create KingsPower");
        Require(applied.Amount == 1, "native PowerCmd.Apply created the wrong KingsPower amount");

        StarterRelicTogawaSakiko starterRelic = player.Relics
            .OfType<StarterRelicTogawaSakiko>()
            .SingleOrDefault()
            ?? throw Failure("the diagnostic Sakiko run did not start with Monochrome Hairband");
        await RelicCmd.Remove(starterRelic);
        Require(!player.Relics.OfType<StarterRelicTogawaSakiko>().Any(),
            "native RelicCmd.Remove did not remove Monochrome Hairband");
        Require(!player.Deck.Cards.OfType<KingsCard>().Any(),
            "the diagnostic edge case unexpectedly had a persistent Kings card");
        NativeSmokeTrace.KingsInfo(
            "removed the starter relic and verified no persistent Kings card before victory.");

        session.Armed = true;
        NativeSmokeTrace.KingsInfo("armed one nonstacking Kings power through native PowerCmd.Apply.");
    }

    public static void RecordAfterCombatEnd(Player player)
    {
        if (!NativeSmokeTrace.KingsContractEnabled ||
            !Sessions.TryGetValue(player, out Session? session) ||
            !session.Armed ||
            session.AfterCombatEndObserved)
        {
            return;
        }

        Require(player.Creature.HasPower<KingsPower>(),
            "AfterCombatEnd ran after KingsPower had already been removed");
        Require(KingsRewardState.IsPending(player),
            "AfterCombatEnd did not persist the Kings reward state");
        session.AfterCombatEndObserved = true;
        NativeSmokeTrace.KingsInfo("AfterCombatEnd persisted pending state before power teardown.");
    }

    public static void RecordAfterCombatVictory(Player player)
    {
        if (!NativeSmokeTrace.KingsContractEnabled ||
            !Sessions.TryGetValue(player, out Session? session) ||
            !session.Armed ||
            session.AfterCombatVictoryObserved)
        {
            return;
        }

        Require(session.AfterCombatEndObserved,
            "AfterCombatVictory ran before the Kings AfterCombatEnd observation");
        Require(!player.Creature.HasPower<KingsPower>(),
            "KingsPower remained attached after player combat teardown");
        Require(KingsRewardState.IsPending(player),
            "Kings pending state was lost before AfterCombatVictory");
        session.AfterCombatVictoryObserved = true;
        NativeSmokeTrace.KingsInfo(
            "AfterCombatVictory observed pending state after power teardown and before native save.");
    }

    public static void RecordRewardModification(
        AbstractModel carrier,
        Player player,
        IReadOnlyCollection<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions,
        bool modified)
    {
        if (!NativeSmokeTrace.KingsContractEnabled ||
            !KingsRewardState.IsCombatCardReward(creationOptions) ||
            !Sessions.TryGetValue(player, out Session? session) ||
            !session.Armed ||
            session.RewardModified)
        {
            return;
        }

        Require(KingsRewardState.IsCarrierForPlayer(carrier, player),
            "the reward hook used a Kings carrier that does not belong to the player");
        Require(session.AfterCombatVictoryObserved,
            "combat card reward generation ran before AfterCombatVictory completed");
        Require(modified, "the native encounter card reward was not modified");
        Require(cardRewardOptions.Count == 2,
            $"the native encounter card reward contained {cardRewardOptions.Count} options instead of two");
        Require(KingsRewardState.IsPending(player),
            "Kings pending state cleared during reward generation");
        session.RewardModified = true;
        NativeSmokeTrace.KingsInfo(
            "native encounter reward generated with 2 options while pending state remained active.");
        Thread.Sleep(750);
    }

    public static void RecordAfterRewardTaken(Player player, Reward reward)
    {
        if (NativeSmokeTrace.KingsReloadEnabled && reward is CardReward)
        {
            Require(!KingsRewardState.IsPending(player),
                "reloaded native card reward consumption did not clear Kings pending state");
            _reloadRewardTakenObserved = true;
            return;
        }

        if (!NativeSmokeTrace.KingsContractEnabled ||
            reward is not CardReward ||
            !Sessions.TryGetValue(player, out Session? session) ||
            !session.RewardModified ||
            session.RewardCleared)
        {
            return;
        }

        Require(!KingsRewardState.IsPending(player),
            "native card reward consumption did not clear Kings pending state");
        session.RewardCleared = true;
        NativeSmokeTrace.KingsInfo("native card reward consumption cleared pending state.");
    }

    public static async Task VerifyReloadedRewardAsync(
        Player player,
        SerializableRoom? serializedRoom)
    {
        if (!NativeSmokeTrace.KingsReloadEnabled)
        {
            return;
        }

        Require(KingsRewardState.IsPending(player),
            "the native save reload did not restore Kings pending state");
        Require(!player.Relics.OfType<StarterRelicTogawaSakiko>().Any(),
            "the removed starter relic returned after loading the Kings edge-case save");
        Require(!player.Deck.Cards.OfType<KingsCard>().Any(),
            "a persistent Kings card unexpectedly appeared after loading the edge-case save");
        Require(player.RunState.Modifiers.OfType<KingsRewardCarrierModifier>().Count() == 1,
            "the reload did not restore exactly one hidden native Kings run carrier");
        if (serializedRoom is null || !serializedRoom.IsPreFinished)
        {
            throw Failure("the reloaded save did not contain its pre-finished combat room");
        }

        CombatRoom room = CombatRoom.FromSerializable(serializedRoom, player.RunState);
        CardCreationOptions options = CardCreationOptions.ForRoom(player, room.RoomType)
            .WithFlags(CardCreationFlags.IsFromCombat);
        NetSingleplayerGameService netService = new();
        using PlayerChoiceSynchronizer synchronizer = new(netService, player.RunState);
        CardReward reward = new(options, 3, player, synchronizer);
        reward.Populate();
        Require(reward.Cards.Count() == 2,
            "the reloaded combat card reward did not contain exactly two options");
        Require(KingsRewardState.IsPending(player),
            "reloaded reward generation cleared Kings before reward consumption");
        NativeSmokeTrace.KingsInfo(
            "reload restored pending state and generated a 2-option combat card reward.");

        int deckCountBefore = player.Deck.Cards.Count;
        bool previousTestMode = TestMode.IsOn;
        ulong? previousLocalNetId = LocalContext.NetId;
        try
        {
            TestMode.IsOn = true;
            LocalContext.NetId = player.NetId;
            using IDisposable selectorScope = CardSelectCmd.PushSelector(
                new FirstRewardCardSelector());
            bool selected = await reward.SelectUnsynchronized();
            Require(selected, "the reloaded native card reward was not selected");
        }
        finally
        {
            LocalContext.NetId = previousLocalNetId;
            TestMode.IsOn = previousTestMode;
        }

        Require(player.Deck.Cards.Count == deckCountBefore + 1,
            "native reloaded reward selection did not add exactly one card to the deck");
        Require(_reloadRewardTakenObserved,
            "native Hook.AfterRewardTaken did not observe the reloaded card reward");
        Require(!KingsRewardState.IsPending(player),
            "Kings pending state returned after native reward selection completed");
        NativeSmokeTrace.KingsInfo(
            "reload consumed the native card reward and cleared pending state.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw Failure(message);
        }
    }

    private static InvalidOperationException Failure(string message)
    {
        return new InvalidOperationException("Kings lifecycle actual-game contract failed: " + message + ".");
    }
}
