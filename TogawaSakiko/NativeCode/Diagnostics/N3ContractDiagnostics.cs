using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Actions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.GameActions;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Models.Relics;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class N3ContractDiagnostics
{
    private sealed class PendingPowerModifier
    {
        public required ModelId PowerId { get; init; }

        public required Creature Target { get; init; }

        public required decimal RequestedAmount { get; init; }

        public required decimal ModifiedAmount { get; init; }

        public bool Consumed { get; set; }
    }

    private sealed class Session
    {
        public required Player Player { get; init; }

        public bool Started { get; set; }

        public int InitialDesireCount { get; set; }

        public Dictionary<CardModel, int> BeforeRemovalHookCounts { get; } = [];

        public PendingPowerModifier? PendingModifier { get; set; }

        public Exception? AsyncFailure { get; set; }

        public TaskCompletionSource QueueScenarioCompletion { get; } = new();

        public bool QueueScenarioPassed { get; set; }

        public int? TeardownHypeAmount { get; set; }

        public int? TeardownBlockAmount { get; set; }
    }

    private sealed class QueueBarrierAction(Player owner) : GameAction
    {
        private readonly TaskCompletionSource _entered = new();
        private readonly TaskCompletionSource _release = new();

        public override ulong OwnerId => owner.NetId;

        public override GameActionType ActionType => GameActionType.Combat;

        public Task Entered => _entered.Task;

        private QueueBarrierAction(Player owner, bool releaseImmediately)
            : this(owner)
        {
            if (releaseImmediately)
            {
                _release.TrySetResult();
            }
        }

        public void Release()
        {
            _release.TrySetResult();
        }

        protected override async Task ExecuteAction()
        {
            _entered.TrySetResult();
            await _release.Task;
        }

        public override INetAction ToNetAction()
        {
            return new NetN3QueueBarrierAction();
        }

        public static QueueBarrierAction CreateReleased(Player owner)
        {
            return new QueueBarrierAction(owner, releaseImmediately: true);
        }
    }

    private static readonly ConditionalWeakTable<CombatState, Session> Sessions = new();

    internal static GameAction CreateReplayedQueueBarrier(Player player)
    {
        return QueueBarrierAction.CreateReleased(player);
    }

    public static async Task RunInCombatAsync(PlayerChoiceContext choiceContext, CardModel sourceCard)
    {
        if (!NativeSmokeTrace.ContractEnabled)
        {
            return;
        }

        CombatState combatState = sourceCard.CombatState as CombatState
            ?? sourceCard.Owner.Creature.CombatState as CombatState
            ?? throw new InvalidOperationException("Phase N3 diagnostics require a live CombatState.");
        Session session = Sessions.GetValue(
            combatState,
            _ => new Session
            {
                Player = sourceCard.Owner,
                InitialDesireCount = sourceCard.Owner.Deck.Cards.Count(card => card is DesireCard)
            });
        if (session.Started)
        {
            return;
        }

        session.Started = true;
        NativeSmokeTrace.ContractInfo(
            $"starting synchronized acceptance action; round={combatState.RoundNumber}, owner={sourceCard.Owner.NetId}.");

        await VerifyDeckContractsAsync(session, combatState);
        NativeSmokeTrace.ContractInfo("deck identity, five-pile, prevention, hook, history, and state checks passed.");

        await VerifyLedgerContractsAsync(session, combatState, choiceContext, sourceCard);
        NativeSmokeTrace.ContractInfo("signed power ledger event, filter, round, removal, and reset checks passed.");

        await VerifyPowerCopyContractsAsync(combatState, choiceContext, sourceCard);
        NativeSmokeTrace.ContractInfo("power copy stack, instance, dynamic state, duration, and rejection checks passed.");

        await VerifyHypeContractsAsync(session, combatState, choiceContext);
        NativeSmokeTrace.ContractInfo("Hype explicit loss, turn clear, damage, retention, owner, and teardown setup checks passed.");

        await ScheduleVisualQueueRemovalScenarioAsync(session, combatState);
        NativeSmokeTrace.ContractInfo("visual play-queue cancellation scenario scheduled through a synchronized mod action.");
    }

    public static void RecordBeforeCardRemoved(CardModel card)
    {
        if (!NativeSmokeTrace.ContractEnabled)
        {
            return;
        }

        if (card.Owner.Creature.CombatState is CombatState combatState &&
            Sessions.TryGetValue(combatState, out Session? session))
        {
            session.BeforeRemovalHookCounts.TryGetValue(card, out int count);
            session.BeforeRemovalHookCounts[card] = count + 1;
        }
    }

    public static bool TryModifyPowerAmountReceived(
        PowerModel power,
        Creature target,
        decimal amount,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (!NativeSmokeTrace.ContractEnabled ||
            target.CombatState is not CombatState combatState ||
            !Sessions.TryGetValue(combatState, out Session? session) ||
            session.PendingModifier is not { Consumed: false } pending ||
            pending.PowerId != power.Id ||
            !ReferenceEquals(pending.Target, target) ||
            pending.RequestedAmount != amount)
        {
            return false;
        }

        pending.Consumed = true;
        modifiedAmount = pending.ModifiedAmount;
        return true;
    }

    public static async Task AfterCombatVictoryAsync(PowerChangeLedger ledger, CombatRoom room)
    {
        if (!NativeSmokeTrace.ContractEnabled ||
            !Sessions.TryGetValue(room.CombatState, out Session? session) ||
            !session.Started)
        {
            return;
        }

        await session.QueueScenarioCompletion.Task;
        Require(session.AsyncFailure is null, $"queued action verification failed: {session.AsyncFailure}");
        Require(session.QueueScenarioPassed, "queued play cancellation did not finish");
        Require(ledger.Count == 0, "power ledger retained events after AfterCombatEnd");
        Require(session.TeardownHypeAmount == 1, "combat teardown consumed Hype before removing the power");
        Require(session.TeardownBlockAmount > 0, "combat teardown Hype probe did not retain positive block until power removal");

        int desireCount = session.Player.Deck.Cards.Count(card => card is DesireCard);
        Require(
            desireCount == session.InitialDesireCount + 1,
            $"Monochrome Hairband expected one Desire, found {desireCount - session.InitialDesireCount}");

        StarterRelicTogawaSakiko hairband = session.Player.Relics
            .OfType<StarterRelicTogawaSakiko>()
            .Single();
        await hairband.AfterCombatVictory(room);
        Require(
            session.Player.Deck.Cards.Count(card => card is DesireCard) == desireCount,
            "Monochrome Hairband duplicated its reward on a repeated victory callback");

        CardModel outsideCombatCard = await AddPersistentAsync<TwoMoonsCard>(session.Player);
        PersistentDeckRemovalResult outsideResult = await VerifySuccessfulRemovalAsync(
            session,
            outsideCombatCard,
            expectedLinkedCopies: 0,
            skipCombatVisuals: true);
        Require(outsideResult.RemovedCombatCopies.Count == 0, "outside-combat removal found a combat copy");

        Require(
            session.Player.Deck.Cards.Count(card => card is SilentFarewellCard) == 1,
            "the persistent save/reload probe card is missing before the native post-victory save");
        Require(
            !session.Player.Deck.Cards.Any(card => card is TwoMoonsCard),
            "a removed persistent Two Moons remained active in the deck");

        NativeSmokeTrace.ContractInfo(
            "all post-combat reset, outside removal, Hairband idempotence, and pre-save checks passed.");
    }

    private static async Task VerifyDeckContractsAsync(Session session, CombatState combatState)
    {
        Player player = session.Player;

        CardModel firstPersistent = await AddPersistentAsync<DesireCard>(player);
        CardModel secondPersistent = await AddPersistentAsync<DesireCard>(player);
        CardModel firstCopy = await AddLinkedCombatCopyAsync(combatState, firstPersistent, PileType.Draw, true);
        CardModel secondCopy = await AddLinkedCombatCopyAsync(combatState, secondPersistent, PileType.Discard, true);

        PersistentDeckRemovalResult firstResult = await VerifySuccessfulRemovalAsync(
            session,
            firstPersistent,
            expectedLinkedCopies: 1,
            skipCombatVisuals: true);
        Require(firstResult.RemovedCombatCopies.Single() == firstCopy, "exact identity removal returned the wrong linked copy");
        Require(secondPersistent.Pile?.Type == PileType.Deck, "the identical persistent card was removed");
        Require(secondCopy.Pile?.Type == PileType.Discard, "the identical card's linked combat copy was removed");

        PersistentDeckRemovalResult secondResult = await VerifySuccessfulRemovalAsync(
            session,
            secondPersistent,
            expectedLinkedCopies: 1,
            skipCombatVisuals: true);
        Require(secondResult.RemovedCombatCopies.Single() == secondCopy, "cleanup selected the wrong identical card copy");

        CardModel fivePilePersistent = await AddPersistentAsync<TwoMoonsCard>(player);
        PileType[] expectedPiles =
        [
            PileType.Hand,
            PileType.Draw,
            PileType.Discard,
            PileType.Exhaust,
            PileType.Play
        ];
        List<CardModel> fivePileCopies = [];
        foreach (PileType pileType in expectedPiles)
        {
            fivePileCopies.Add(await AddLinkedCombatCopyAsync(combatState, fivePilePersistent, pileType, true));
        }

        Require(
            fivePileCopies.Select(card => card.Pile!.Type).Order().SequenceEqual(expectedPiles.Order()),
            "five-pile setup did not cover Hand, Draw, Discard, Exhaust, and Play");
        PersistentDeckRemovalResult fivePileResult = await VerifySuccessfulRemovalAsync(
            session,
            fivePilePersistent,
            expectedLinkedCopies: 5,
            skipCombatVisuals: true);
        Require(
            fivePileResult.RemovedCombatCopies.ToHashSet().SetEquals(fivePileCopies),
            "five-pile removal did not return every exact linked copy");

        CardModel protectedPersistent = await AddPersistentAsync<DesireCard>(player);
        protectedPersistent.AddKeyword(CardKeyword.Eternal);
        CardModel protectedCopy = await AddLinkedCombatCopyAsync(combatState, protectedPersistent, PileType.Exhaust, true);
        int hookCountBefore = GetBeforeRemovalHookCount(session, protectedPersistent);
        int historyCountBefore = GetRemovalHistoryCount(player);
        int postRemovalEvents = 0;
        void OnRemoved(PersistentDeckRemovalResult result)
        {
            if (ReferenceEquals(result.PersistentCard, protectedPersistent))
            {
                postRemovalEvents++;
            }
        }

        PersistentDeckMutation.PersistentCardRemoved += OnRemoved;
        PersistentDeckRemovalResult protectedResult;
        try
        {
            protectedResult = await PersistentDeckMutation.RemoveAsync(
                protectedPersistent,
                showPersistentPreview: false,
                skipCombatVisuals: true);
        }
        finally
        {
            PersistentDeckMutation.PersistentCardRemoved -= OnRemoved;
        }

        Require(protectedResult.Prevented, "Eternal card removal was not prevented");
        Require(protectedPersistent.Pile?.Type == PileType.Deck, "prevented persistent card left the deck");
        Require(protectedCopy.Pile?.Type == PileType.Exhaust, "prevented removal deleted a linked combat copy");
        Require(GetBeforeRemovalHookCount(session, protectedPersistent) == hookCountBefore, "prevented removal fired BeforeCardRemoved");
        Require(GetRemovalHistoryCount(player) == historyCountBefore, "prevented removal wrote removal history");
        Require(postRemovalEvents == 0, "prevented removal published a post-removal event");
        protectedPersistent.RemoveKeyword(CardKeyword.Eternal);
        await VerifySuccessfulRemovalAsync(session, protectedPersistent, 1, true);

        CardPileAddResult saveProbeResult = await PersistentDeckMutation.AddCanonicalAsync<SilentFarewellCard>(
            player,
            skipVisuals: true);
        Require(saveProbeResult.success, "persistent save/reload probe card add was prevented");
        Require(saveProbeResult.cardAdded.Pile?.Type == PileType.Deck, "save/reload probe did not enter the persistent deck");
    }

    private static async Task VerifyLedgerContractsAsync(
        Session session,
        CombatState combatState,
        PlayerChoiceContext choiceContext,
        CardModel sourceCard)
    {
        Creature player = session.Player.Creature;
        Creature enemy = combatState.Enemies.First(creature => creature.IsAlive);
        PowerChangeLedger ledger = PowerChangeLedgerService.GetLedger(combatState);
        int baseRound = combatState.RoundNumber;

        Require(player.GetPower<StrengthPower>() is null, "ledger probe requires no pre-existing player Strength");
        session.PendingModifier = new PendingPowerModifier
        {
            PowerId = ModelDb.GetId<StrengthPower>(),
            Target = player,
            RequestedAmount = 1m,
            ModifiedAmount = 3m
        };

        StrengthPower strength = await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            player,
            1m,
            player,
            sourceCard,
            silent: true)
            ?? throw new InvalidOperationException("ledger Strength probe was prevented");
        Require(session.PendingModifier.Consumed, "post-modifier ledger probe modifier did not execute");
        Require(strength.Amount == 3, "native power command did not apply the modified amount");
        await PowerCmd.Apply<StrengthPower>(choiceContext, player, 2m, player, sourceCard, silent: true);
        await PowerCmd.ModifyAmount(choiceContext, strength, -1m, enemy, sourceCard, silent: true);
        await PowerCmd.Remove(strength);

        WeakPower weak = await PowerCmd.Apply<WeakPower>(
            choiceContext,
            enemy,
            2m,
            player,
            sourceCard,
            silent: true)
            ?? throw new InvalidOperationException("ledger Weak probe was prevented");
        await PowerCmd.TickDownDuration(weak);
        await PowerCmd.TickDownDuration(weak);

        foreach (Creature creature in combatState.Creatures.Where(creature =>
                     !ReferenceEquals(creature, player) && !ReferenceEquals(creature, enemy)))
        {
            HypePower extraCreatureProbe = await PowerCmd.Apply<HypePower>(
                choiceContext,
                creature,
                1m,
                player,
                sourceCard,
                silent: true)
                ?? throw new InvalidOperationException($"ledger probe could not apply to {creature.LogName}");
            await PowerCmd.Remove(extraCreatureProbe);
        }

        IReadOnlyList<PowerChangeEvent> baseRoundEvents = ledger.Snapshot(baseRound);
        PowerChangeEvent modifiedGain = baseRoundEvents.Single(powerEvent =>
            powerEvent.PowerModelId == ModelDb.GetId<StrengthPower>() &&
            powerEvent.Kind == PowerChangeKind.New);
        Require(modifiedGain.Delta == 3m, "ledger stored the requested amount instead of the post-modifier delta");
        Require(modifiedGain.AmountBefore == 0 && modifiedGain.AmountAfter == 3, "ledger amount snapshot for a new power is wrong");
        Require(modifiedGain.Applier == CreatureIdentity.FromCreature(player), "ledger applier snapshot is wrong");
        Require(modifiedGain.CardSource?.ModelId == sourceCard.Id, "ledger card-source snapshot is wrong");
        Require(
            baseRoundEvents.Count(powerEvent =>
                powerEvent.PowerModelId == ModelDb.GetId<StrengthPower>() && powerEvent.IsGain) == 2,
            "ledger did not preserve multiple gains to the same power");
        Require(baseRoundEvents.Any(powerEvent =>
            powerEvent.PowerModelId == ModelDb.GetId<StrengthPower>() &&
            powerEvent.Kind == PowerChangeKind.Reduced &&
            powerEvent.Delta == -1m), "ledger missed a reduced power");
        Require(baseRoundEvents.Any(powerEvent =>
            powerEvent.PowerModelId == ModelDb.GetId<StrengthPower>() &&
            powerEvent.Kind == PowerChangeKind.DirectRemoval &&
            powerEvent.Delta == -4m), "ledger missed a direct full removal");
        Require(baseRoundEvents.Any(powerEvent =>
            powerEvent.PowerModelId == ModelDb.GetId<WeakPower>() &&
            powerEvent.Kind == PowerChangeKind.FullRemoval), "ledger missed an amount-driven full removal");
        Require(baseRoundEvents.Count(powerEvent =>
            powerEvent.PowerModelId == ModelDb.GetId<WeakPower>() && powerEvent.Kind == PowerChangeKind.Reduced) == 1,
            "ledger missed the duration tick or double-counted final removal");
        Require(
            combatState.Creatures.All(creature =>
                baseRoundEvents.Any(powerEvent => powerEvent.Target == CreatureIdentity.FromCreature(creature))),
            "ledger did not capture an event for every player and enemy");
        Require(
            ledger.Snapshot(baseRound, null, MegaCrit.Sts2.Core.Entities.Powers.PowerType.Buff).Length > 0 &&
            ledger.Snapshot(baseRound, null, MegaCrit.Sts2.Core.Entities.Powers.PowerType.Debuff).Length > 0,
            "ledger buff/debuff filters did not return both types");

        combatState.RoundNumber = baseRound + 1;
        HypePower nextRoundPower = await PowerCmd.Apply<HypePower>(
            choiceContext,
            enemy,
            1m,
            player,
            sourceCard,
            silent: true)
            ?? throw new InvalidOperationException("next-round ledger probe was prevented");
        Require(ledger.CurrentRound(combatState, enemy).Length == 1, "current-round query did not select the new round");
        Require(ledger.PreviousRound(combatState).Length == baseRoundEvents.Count, "previous-round query changed the prior bucket");

        combatState.RoundNumber = baseRound;
        HypePower rewindPower = await PowerCmd.Apply<HypePower>(
            choiceContext,
            player,
            1m,
            player,
            sourceCard,
            silent: true)
            ?? throw new InvalidOperationException("rewind ledger probe was prevented");
        Require(ledger.Count == 1, "ledger did not reset when the same combat state rewound to an earlier round");
        Require(ledger.CurrentRound(combatState, player).Single().PowerModelId == ModelDb.GetId<HypePower>(), "rewind reset retained stale events");
        await PowerCmd.Remove(rewindPower);
        await PowerCmd.Remove(nextRoundPower);
    }

    private static async Task VerifyPowerCopyContractsAsync(
        CombatState combatState,
        PlayerChoiceContext choiceContext,
        CardModel sourceCard)
    {
        Creature player = sourceCard.Owner.Creature;
        Creature enemy = combatState.Enemies.First(creature => creature.IsAlive);

        StrengthPower sourceStrength = await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            player,
            2m,
            player,
            sourceCard,
            silent: true)
            ?? throw new InvalidOperationException("power-copy Strength source was prevented");
        PowerCopyResult firstStrengthCopy = await PowerCopyCommand.ApplyCopyAsync(
            choiceContext,
            sourceStrength,
            enemy,
            cardSource: sourceCard,
            silent: true);
        PowerCopyResult stackedStrengthCopy = await PowerCopyCommand.ApplyCopyAsync(
            choiceContext,
            sourceStrength,
            enemy,
            cardSource: sourceCard,
            silent: true);
        Require(firstStrengthCopy.Status == PowerCopyStatus.AppliedNewInstance, "normal stackable copy did not add a new instance");
        Require(stackedStrengthCopy.Status == PowerCopyStatus.StackedExistingInstance, "normal stackable copy did not use native stacking");
        Require(stackedStrengthCopy.AppliedPower?.Amount == 4, "normal stackable copy preserved the wrong amount");
        await PowerCmd.Remove(firstStrengthCopy.AppliedPower);
        await PowerCmd.Remove(sourceStrength);

        TheBombPower sourceBomb = await PowerCmd.Apply<TheBombPower>(
            choiceContext,
            player,
            2m,
            player,
            sourceCard,
            silent: true)
            ?? throw new InvalidOperationException("power-copy Bomb source was prevented");
        sourceBomb.SetDamage(77m);
        int sourceRemovedCalls = 0;
        sourceBomb.Removed += () => sourceRemovedCalls++;
        PowerCopyResult bombCopyResult = await PowerCopyCommand.ApplyCopyAsync(
            choiceContext,
            sourceBomb,
            enemy,
            cardSource: sourceCard,
            silent: true);
        TheBombPower copiedBomb = bombCopyResult.AppliedPower as TheBombPower
            ?? throw new InvalidOperationException("instanced Bomb copy was not applied");
        Require(bombCopyResult.Status == PowerCopyStatus.AppliedNewInstance, "instanced power did not remain instanced");
        Require(!ReferenceEquals(copiedBomb, sourceBomb), "instanced power reused the source object");
        Require(ReferenceEquals(copiedBomb.Owner, enemy), "copied power retained its old owner");
        Require(copiedBomb.Target is null, "copied power retained a target reference");
        Require(copiedBomb.DynamicVars.Damage.BaseValue == 77m, "copied power lost dynamic-variable state");
        await PowerCmd.Remove(copiedBomb);
        Require(sourceRemovedCalls == 0, "copied power retained the source Removed event handler");
        await PowerCmd.Remove(sourceBomb);
        Require(sourceRemovedCalls == 1, "source power Removed event did not remain isolated");

        StranglePower sourceStrangle = await PowerCmd.Apply<StranglePower>(
            choiceContext,
            enemy,
            2m,
            player,
            sourceCard,
            silent: true)
            ?? throw new InvalidOperationException("power-copy Strangle source was prevented");
        PowerCopyResult strangleCopyResult = await PowerCopyCommand.ApplyCopyAsync(
            choiceContext,
            sourceStrangle,
            player,
            cardSource: sourceCard,
            silent: true);
        Require(strangleCopyResult.AppliedPower is StranglePower, "instanced-per-applier power was not copied");
        Require(ReferenceEquals(strangleCopyResult.Applier, player), "instanced-per-applier copy lost the source applier policy");
        await PowerCmd.Remove(strangleCopyResult.AppliedPower);
        await PowerCmd.Remove(sourceStrangle);

        WeakPower sourceWeak = await PowerCmd.Apply<WeakPower>(
            choiceContext,
            enemy,
            2m,
            player,
            sourceCard,
            silent: true)
            ?? throw new InvalidOperationException("power-copy duration source was prevented");
        PowerCopyResult weakCopyResult = await PowerCopyCommand.ApplyCopyAsync(
            choiceContext,
            sourceWeak,
            player,
            cardSource: sourceCard,
            silent: true);
        WeakPower copiedWeak = weakCopyResult.AppliedPower as WeakPower
            ?? throw new InvalidOperationException("duration power was not copied");
        Require(copiedWeak.Amount == 2, "duration power amount was not preserved");
        Require(copiedWeak.SkipNextDurationTick, "native player-debuff duration policy was bypassed");
        await PowerCmd.TickDownDuration(copiedWeak);
        Require(copiedWeak.Amount == 2 && !copiedWeak.SkipNextDurationTick, "first native duration skip was not preserved");
        await PowerCmd.TickDownDuration(copiedWeak);
        Require(copiedWeak.Amount == 1, "duration power did not tick after its native skip");
        await PowerCmd.Remove(copiedWeak);
        await PowerCmd.Remove(sourceWeak);

        PowerCopyResult temporaryResult = await PowerCopyCommand.ApplyCopyAsync(
            choiceContext,
            ModelDb.Power<FlexPotionPower>(),
            player,
            PowerCopyApplierPolicy.None,
            amountOverride: 2,
            silent: true);
        Require(temporaryResult.Status == PowerCopyStatus.Unsupported, "paired temporary power was not rejected");
        PowerCopyResult internalStateResult = await PowerCopyCommand.ApplyCopyAsync(
            choiceContext,
            ModelDb.Power<AutomationPower>(),
            player,
            PowerCopyApplierPolicy.None,
            amountOverride: 1,
            silent: true);
        Require(internalStateResult.Status == PowerCopyStatus.Unsupported, "unsupported custom internal state was not rejected");
    }

    private static async Task VerifyHypeContractsAsync(
        Session session,
        CombatState combatState,
        PlayerChoiceContext choiceContext)
    {
        Creature player = session.Player.Creature;
        Creature enemy = combatState.Enemies.First(creature => creature.IsAlive);

        await CreatureCmd.GainBlock(player, 10m, ValueProp.Unpowered, null, fast: true);
        int playerExplicitBlock = player.Block;
        HypePower playerHype = await PowerCmd.Apply<HypePower>(choiceContext, player, 2m, player, null, silent: true)
            ?? throw new InvalidOperationException("player Hype probe was prevented");
        await CreatureCmd.LoseBlock(choiceContext, player, 3m, enemy);
        Require(
            player.Block == playerExplicitBlock && playerHype.Amount == 1,
            $"partial explicit loss was not wholly prevented for one Hype; block={player.Block}/{playerExplicitBlock}, Hype={playerHype.Amount}");
        await CreatureCmd.LoseBlock(choiceContext, player, 99m, enemy);
        Require(
            player.Block == playerExplicitBlock && player.GetPower<HypePower>() is null,
            $"full explicit loss did not consume exactly one Hype; block={player.Block}/{playerExplicitBlock}");
        await CreatureCmd.LoseBlock(choiceContext, player, 2m, enemy);
        Require(player.Block == playerExplicitBlock - 2, "explicit loss without Hype was incorrectly prevented");
        await CreatureCmd.LoseBlock(choiceContext, player, 999m, enemy);

        await CreatureCmd.GainBlock(player, 5m, ValueProp.Unpowered, null, fast: true);
        int zeroLossBlock = player.Block;
        HypePower zeroLossHype = await PowerCmd.Apply<HypePower>(choiceContext, player, 1m, player, null, silent: true)
            ?? throw new InvalidOperationException("zero-loss Hype probe was prevented");
        await CreatureCmd.LoseBlock(choiceContext, player, 0m, enemy);
        await CreatureCmd.LoseBlock(choiceContext, player, -2m, enemy);
        Require(player.Block == zeroLossBlock && zeroLossHype.Amount == 1, "zero or negative loss consumed Hype");
        await PowerCmd.Remove(zeroLossHype);
        await CreatureCmd.LoseBlock(choiceContext, player, 999m, enemy);

        HypePower zeroBlockHype = await PowerCmd.Apply<HypePower>(choiceContext, player, 1m, player, null, silent: true)
            ?? throw new InvalidOperationException("zero-block Hype probe was prevented");
        await CreatureCmd.LoseBlock(choiceContext, player, 1m, enemy);
        Require(player.Block == 0 && zeroBlockHype.Amount == 1, "zero-block loss consumed Hype");
        await PowerCmd.Remove(zeroBlockHype);

        await CreatureCmd.GainBlock(player, 5m, ValueProp.Unpowered, null, fast: true);
        int damageBlock = player.Block;
        HypePower damageHype = await PowerCmd.Apply<HypePower>(choiceContext, player, 1m, player, null, silent: true)
            ?? throw new InvalidOperationException("damage Hype probe was prevented");
        await CreatureCmd.Damage(choiceContext, player, 1m, ValueProp.Unpowered, enemy);
        Require(player.Block == damageBlock - 1 && damageHype.Amount == 1, "normal damage absorption invoked explicit-loss Hype behavior");
        await PowerCmd.Remove(damageHype);
        await CreatureCmd.LoseBlock(choiceContext, player, 999m, enemy);

        await CreatureCmd.GainBlock(enemy, 5m, ValueProp.Unpowered, null, fast: true);
        int enemyExplicitBlock = enemy.Block;
        HypePower enemyExplicitHype = await PowerCmd.Apply<HypePower>(choiceContext, enemy, 1m, enemy, null, silent: true)
            ?? throw new InvalidOperationException("enemy Hype probe was prevented");
        await CreatureCmd.LoseBlock(choiceContext, enemy, 5m, player);
        Require(enemy.Block == enemyExplicitBlock && enemy.GetPower<HypePower>() is null, "enemy explicit loss did not use creature-wide Hype behavior");
        Require(enemyExplicitHype.Amount == 0, "enemy explicit Hype was not decremented to removal");
        await CreatureCmd.LoseBlock(choiceContext, enemy, 999m, player);

        await CreatureCmd.GainBlock(enemy, 7m, ValueProp.Unpowered, null, fast: true);
        int turnBoundaryBlock = enemy.Block;
        BlurPower blur = await PowerCmd.Apply<BlurPower>(choiceContext, enemy, 1m, enemy, null, silent: true)
            ?? throw new InvalidOperationException("Blur interoperability probe was prevented");
        HypePower turnHype = await PowerCmd.Apply<HypePower>(choiceContext, enemy, 1m, enemy, null, silent: true)
            ?? throw new InvalidOperationException("turn-boundary Hype probe was prevented");
        await enemy.AfterTurnStart(CombatSide.Enemy);
        Require(enemy.Block == turnBoundaryBlock && turnHype.Amount == 1, "later Hype consumed after Blur won native preventer ordering");
        await PowerCmd.Remove(blur);
        await enemy.AfterTurnStart(CombatSide.Enemy);
        Require(enemy.Block == turnBoundaryBlock && enemy.GetPower<HypePower>() is null, "turn-boundary clear did not consume exactly one Hype");
        await CreatureCmd.LoseBlock(choiceContext, enemy, 999m, player);

        BarricadePower barricade = await PowerCmd.Apply<BarricadePower>(
            choiceContext,
            player,
            1m,
            player,
            null,
            silent: true)
            ?? throw new InvalidOperationException("teardown retention probe was prevented");
        HypePower teardownHype = await PowerCmd.Apply<HypePower>(choiceContext, player, 1m, player, null, silent: true)
            ?? throw new InvalidOperationException("teardown Hype probe was prevented");
        await CreatureCmd.GainBlock(player, 5m, ValueProp.Unpowered, null, fast: true);
        teardownHype.Removed += () =>
        {
            session.TeardownHypeAmount = teardownHype.Amount;
            session.TeardownBlockAmount = player.Block;
        };
        Require(barricade.Amount == 1 && teardownHype.Amount == 1, "teardown probe setup failed");
    }

    private static async Task ScheduleVisualQueueRemovalScenarioAsync(Session session, CombatState combatState)
    {
        CardModel persistentCard = await AddPersistentAsync<DesireCard>(session.Player);
        CardModel combatCopy = await AddLinkedCombatCopyAsync(
            combatState,
            persistentCard,
            PileType.Hand,
            skipVisuals: true);
        int hookCountBefore = GetBeforeRemovalHookCount(session, persistentCard);
        int historyCountBefore = GetRemovalHistoryCount(session.Player);
        int postRemovalEvents = 0;

        void OnRemoved(PersistentDeckRemovalResult result)
        {
            if (ReferenceEquals(result.PersistentCard, persistentCard))
            {
                postRemovalEvents++;
            }
        }

        PersistentDeckMutation.PersistentCardRemoved += OnRemoved;
        QueueBarrierAction barrier = new(session.Player);
        RunManager.Instance.ActionQueueSet.EnqueueWithoutSynchronizing(barrier);
        await barrier.Entered;
        bool scenarioScheduled = false;
        try
        {
            PersistentDeckRemovalGameAction removeAction = PersistentDeckRemovalGameAction.Request(
                persistentCard,
                skipCombatVisuals: true);
            removeAction.AfterFinished += _ =>
            {
                try
                {
                    PersistentDeckRemovalResult actionResult = removeAction.Result
                        ?? throw new InvalidOperationException("synchronized removal action did not publish a result");
                    Require(actionResult.Success, $"synchronized removal action failed: {actionResult.FailureReason}");
                    Require(actionResult.RemovedCombatCopies.Count == 1, "synchronized action removed the wrong copy count");
                    Require(actionResult.RemovedCombatCopies.Single() == combatCopy, "synchronized action selected the wrong linked copy");
                    Require(GetBeforeRemovalHookCount(session, persistentCard) == hookCountBefore + 1, "synchronized action did not fire BeforeCardRemoved once");
                    Require(GetRemovalHistoryCount(session.Player) == historyCountBefore + 1, "synchronized action did not write removal history once");
                    Require(postRemovalEvents == 1, "synchronized action did not publish one post-removal event");
                    Require(NCardPlayQueue.Instance?.GetCardNode(combatCopy) is not null, "queued play lost its visual before its cancellation action executed");
                }
                catch (Exception exception)
                {
                    session.AsyncFailure = exception;
                }
            };

            PlayCardAction playAction = new(combatCopy, null);
            playAction.AfterFinished += _ =>
            {
                TaskHelper.RunSafely(FinalizeVisualQueueScenarioAsync(
                    session,
                    persistentCard,
                    combatCopy,
                    playAction,
                    OnRemoved));
            };
            // AutoSlay invokes card logic outside the normal local input enqueue path. Enqueue the
            // diagnostic play directly after the synchronized removal so the real ActionEnqueued
            // visual route runs while preserving removal-before-play queue ordering.
            RunManager.Instance.ActionQueueSet.EnqueueWithoutSynchronizing(playAction);

            NCardPlayQueue playQueue = NCardPlayQueue.Instance
                ?? throw new InvalidOperationException("the actual visual play queue is unavailable");
            NCard? queuedNode = playQueue.GetCardNode(combatCopy);
            if (queuedNode is null)
            {
                playQueue.OnLocalCardPlayed(playAction, null, combatCopy);
                queuedNode = playQueue.GetCardNode(combatCopy);
            }

            if (queuedNode is null)
            {
                throw new InvalidOperationException("the linked card was not placed in the actual visual play queue");
            }

            Require(playQueue.IsAncestorOf(queuedNode), "the queued card node is not parented under the visual play queue");
            scenarioScheduled = true;
        }
        finally
        {
            barrier.Release();
            if (!scenarioScheduled)
            {
                PersistentDeckMutation.PersistentCardRemoved -= OnRemoved;
                session.QueueScenarioCompletion.TrySetResult();
            }
        }
    }

    private static async Task FinalizeVisualQueueScenarioAsync(
        Session session,
        CardModel persistentCard,
        CardModel combatCopy,
        PlayCardAction playAction,
        Action<PersistentDeckRemovalResult> removalHandler)
    {
        try
        {
            await Cmd.CustomScaledWait(0.6f, 0.6f);
            Require(playAction.State == GameActionState.Finished, "canceled linked play action did not leave the action queue");
            Require(persistentCard.HasBeenRemovedFromState && persistentCard.Pile is null, "queued persistent card remained active");
            Require(combatCopy.HasBeenRemovedFromState && combatCopy.Pile is null, "queued linked combat card remained active");
            Require(NCardPlayQueue.Instance?.GetCardNode(combatCopy) is null, "queued card node reappeared after action completion");
            NCard? remainingNode = NCard.FindOnTable(combatCopy);
            Require(remainingNode is null || !GodotObject.IsInstanceValid(remainingNode), "a card node remained on the combat table after queue cancellation");
            session.QueueScenarioPassed = true;
            NativeSmokeTrace.ContractInfo("visual play-queue node and stale play action cleanup passed.");
        }
        catch (Exception exception)
        {
            session.AsyncFailure ??= exception;
        }
        finally
        {
            PersistentDeckMutation.PersistentCardRemoved -= removalHandler;
            session.QueueScenarioCompletion.TrySetResult();
        }
    }

    private static async Task<CardModel> AddPersistentAsync<TCard>(Player player)
        where TCard : CardModel
    {
        CardPileAddResult result = await PersistentDeckMutation.AddCanonicalAsync<TCard>(
            player,
            skipVisuals: true);
        Require(result.success, $"native deck add for {typeof(TCard).Name} was prevented");
        Require(result.cardAdded.Pile?.Type == PileType.Deck, $"native deck add for {typeof(TCard).Name} missed the deck");
        return result.cardAdded;
    }

    private static async Task<CardModel> AddLinkedCombatCopyAsync(
        CombatState combatState,
        CardModel persistentCard,
        PileType pileType,
        bool skipVisuals)
    {
        CardModel combatCopy = combatState.CloneCard(persistentCard);
        combatCopy.DeckVersion = persistentCard;
        CardPileAddResult result = await CardPileCmd.Add(
            combatCopy,
            pileType,
            skipVisuals: skipVisuals);
        Require(result.success, $"linked combat copy add to {pileType} was prevented");
        Require(ReferenceEquals(result.cardAdded, combatCopy), $"linked combat copy add to {pileType} replaced the card");
        Require(combatCopy.Pile?.Type == pileType, $"linked combat copy expected {pileType}, found {combatCopy.Pile?.Type}");
        return combatCopy;
    }

    private static async Task<PersistentDeckRemovalResult> VerifySuccessfulRemovalAsync(
        Session session,
        CardModel persistentCard,
        int expectedLinkedCopies,
        bool skipCombatVisuals)
    {
        int hookCountBefore = GetBeforeRemovalHookCount(session, persistentCard);
        int historyCountBefore = GetRemovalHistoryCount(session.Player);
        int postRemovalEvents = 0;
        void OnRemoved(PersistentDeckRemovalResult result)
        {
            if (ReferenceEquals(result.PersistentCard, persistentCard))
            {
                postRemovalEvents++;
            }
        }

        PersistentDeckMutation.PersistentCardRemoved += OnRemoved;
        PersistentDeckRemovalResult removalResult;
        try
        {
            removalResult = await PersistentDeckMutation.RemoveAsync(
                persistentCard,
                showPersistentPreview: false,
                skipCombatVisuals: skipCombatVisuals);
        }
        finally
        {
            PersistentDeckMutation.PersistentCardRemoved -= OnRemoved;
        }

        Require(removalResult.Success, $"native persistent removal failed: {removalResult.FailureReason}");
        Require(removalResult.RemovedCombatCopies.Count == expectedLinkedCopies, "native persistent removal returned the wrong linked-copy count");
        Require(persistentCard.HasBeenRemovedFromState && persistentCard.Pile is null, "removed persistent card remained active or piled");
        Require(removalResult.RemovedCombatCopies.All(card => card.HasBeenRemovedFromState && card.Pile is null), "removed combat copy remained active or piled");
        Require(GetBeforeRemovalHookCount(session, persistentCard) == hookCountBefore + 1, "BeforeCardRemoved did not fire exactly once");
        Require(GetRemovalHistoryCount(session.Player) == historyCountBefore + 1, "persistent removal history did not advance exactly once");
        Require(postRemovalEvents == 1, "post-removal event did not fire exactly once");
        return removalResult;
    }

    private static int GetBeforeRemovalHookCount(Session session, CardModel card)
    {
        return session.BeforeRemovalHookCounts.GetValueOrDefault(card);
    }

    private static int GetRemovalHistoryCount(Player player)
    {
        return player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).CardsRemoved.Count
            ?? throw new InvalidOperationException("the active map-point removal history is unavailable");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Phase N3 actual-game contract failure: " + message);
        }
    }
}

public struct NetN3QueueBarrierAction : INetAction, IPacketSerializable
{
    public readonly GameAction ToGameAction(Player player)
    {
        return N3ContractDiagnostics.CreateReplayedQueueBarrier(player);
    }

    public readonly void Serialize(PacketWriter writer)
    {
    }

    public void Deserialize(PacketReader reader)
    {
    }
}
