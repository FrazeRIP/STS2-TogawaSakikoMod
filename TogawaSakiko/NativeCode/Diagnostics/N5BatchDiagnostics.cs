using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static partial class N5BatchDiagnostics
{
    private readonly record struct CombatCardLocation(CardModel Card, PileType PileType);

    private static readonly ConditionalWeakTable<CombatState, object> Sessions = new();

    public static async Task RunInCombatAsync(PlayerChoiceContext choiceContext, CardModel sourceCard)
    {
        if (!NativeSmokeTrace.N5BatchEnabled)
        {
            return;
        }

        CombatState combatState = sourceCard.CombatState as CombatState
            ?? sourceCard.Owner.Creature.CombatState as CombatState
            ?? throw new InvalidOperationException("Phase N5 diagnostics require a live CombatState.");
        if (Sessions.TryGetValue(combatState, out _))
        {
            return;
        }
        Sessions.Add(combatState, new object());

        Player player = sourceCard.Owner;
        Creature target = combatState.GetOpponentsOf(player.Creature).First(creature => creature.IsHittable);

        StartingOptionsDiagnostics.Validate(player);
        await VerifySpringSunlightStartupAsync(combatState, player, target, choiceContext);
        await VerifyDazzlingPriorStacksAsync(combatState, player, target, choiceContext);
        await VerifyGreetingsAsync(combatState, player, choiceContext);
        await VerifyIdealAsync(combatState, player, choiceContext);
        await VerifyDormantCursePowerScaffoldsAsync(player, target, choiceContext);
        await VerifyMelodiaAndDivinityAsync(combatState, player, target, choiceContext);
        await VerifyHeartsBarrierAsync(combatState, player, choiceContext);
        await VerifyAccompliceAsync(combatState, player, choiceContext);
        await VerifyDesuWaAsync(combatState, player, target, choiceContext);
        await VerifyRadianceAsync(combatState, player, choiceContext);
        await VerifyFeedbackGameplayAsync(combatState, player, target, choiceContext);

        NativeSmokeTrace.N5Info(
            "native card batches passed. GreetingsEnergy=5, IdealFreeAttacks=5, DormantCursePowers=DolorisOblivionisTick, MelodiaDivinity=PlayerEnemyTriple, HeartsBarrier=DeckSizedBlock+Retain+LinkedHandProbe, CommonTail=Accomplice5+DesuWaDraw1BaseExhaustUpgradeDiscard, RadianceDazzling=10.");
    }

    private static async Task VerifyGreetingsAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        PlayerCombatState playerState = player.PlayerCombatState
            ?? throw new InvalidOperationException("Phase N5 Greetings diagnostic requires player combat state.");
        decimal energyBefore = playerState.Energy;
        GreetingsCard baseCard = await CreateAndAutoPlayAsync<GreetingsCard>(combatState, player, choiceContext, null, upgraded: false);
        GreetingsCard upgradedCard = await CreateAndAutoPlayAsync<GreetingsCard>(combatState, player, choiceContext, null, upgraded: true);
        Require(playerState.Energy - energyBefore == 5m, "Greetings did not gain 2 plus 3 energy");
        Require(baseCard.Pile?.Type == PileType.Exhaust, "base Greetings did not Exhaust");
        Require(upgradedCard.Pile?.Type == PileType.Exhaust, "upgraded Greetings did not Exhaust");
    }

    private static async Task VerifyIdealAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        decimal amountBefore = player.Creature.Powers.OfType<FreeAttackPower>().SingleOrDefault()?.Amount ?? 0m;
        await CreateAndAutoPlayAsync<IdealCard>(combatState, player, choiceContext, null, upgraded: false);
        await CreateAndAutoPlayAsync<IdealCard>(combatState, player, choiceContext, null, upgraded: true);
        decimal amountAfter = player.Creature.Powers.OfType<FreeAttackPower>().Single().Amount;
        Require(amountAfter - amountBefore == 5m, "Ideal did not apply 2 plus 3 native Free Attack stacks");
    }

    private static async Task VerifyDormantCursePowerScaffoldsAsync(
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        DolorisPower doloris = await PowerCmd.Apply<DolorisPower>(
                choiceContext,
                player.Creature,
                1m,
                player.Creature,
                null)
            ?? throw new InvalidOperationException("Could not apply DolorisPower scaffold.");
        Require(doloris.SkipNextDurationTick, "DolorisPower did not receive native player-debuff tick protection");
        await doloris.AfterSideTurnEnd(choiceContext, CombatSide.Enemy, [target]);
        Require(player.Creature.GetPower<DolorisPower>() == doloris && !doloris.SkipNextDurationTick,
            "DolorisPower did not consume only its protected first tick");
        await doloris.AfterSideTurnEnd(choiceContext, CombatSide.Enemy, [target]);
        Require(player.Creature.GetPower<DolorisPower>() is null, "DolorisPower did not expire on its next duration tick");

        OblivionisPower oblivionis = await PowerCmd.Apply<OblivionisPower>(
                choiceContext,
                player.Creature,
                1m,
                player.Creature,
                null)
            ?? throw new InvalidOperationException("Could not apply OblivionisPower scaffold.");
        Require(oblivionis.SkipNextDurationTick, "OblivionisPower did not receive native player-debuff tick protection");
        await oblivionis.AfterSideTurnEnd(choiceContext, CombatSide.Enemy, [target]);
        Require(player.Creature.GetPower<OblivionisPower>() == oblivionis && !oblivionis.SkipNextDurationTick,
            "OblivionisPower did not consume only its protected first tick");
        await oblivionis.AfterSideTurnEnd(choiceContext, CombatSide.Enemy, [target]);
        Require(player.Creature.GetPower<OblivionisPower>() is null,
            "OblivionisPower did not expire on its next duration tick");
    }

    private static async Task VerifyMelodiaAndDivinityAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        PlayerCombatState playerState = player.PlayerCombatState
            ?? throw new InvalidOperationException("Phase N5 Melodia diagnostic requires player combat state.");
        decimal playerBlockBefore = player.Creature.Block;
        decimal targetBlockBefore = target.Block;
        int playerStrengthBefore = player.Creature.GetPower<StrengthPower>()?.Amount ?? 0;
        int targetStrengthBefore = target.GetPower<StrengthPower>()?.Amount ?? 0;

        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        await PowerCmd.Remove(target.GetPower<StrengthPower>());
        await CreatureCmd.GainBlock(player.Creature, 100m, ValueProp.Unpowered, null, fast: true);
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);

        Require(player.Creature.GetPower<MelodiaPower>() is null, "Melodia diagnostic started with stale player Melodia");
        Require(player.Creature.GetPower<MonsterDivinityPower>() is null,
            "Melodia diagnostic started with stale player Divinity");
        Require(player.Creature.GetPower<DazzlingPower>() is null,
            "Melodia diagnostic started with stale player Dazzling");

        await PowerCmd.Apply<DazzlingPower>(
            choiceContext,
            player.Creature,
            2m,
            player.Creature,
            null);
        decimal blockBeforeMelodia = target.Block;
        decimal energyBefore = playerState.Energy;
        await PowerCmd.Apply<MelodiaPower>(
            choiceContext,
            player.Creature,
            8m,
            player.Creature,
            null);
        Require(player.Creature.GetPower<MelodiaPower>()?.Amount == 8,
            "sub-threshold Melodia did not retain its amount");
        Require(player.Creature.GetPower<MonsterDivinityPower>() is null,
            "sub-threshold Melodia entered Divinity early");
        await PowerCmd.Apply<MelodiaPower>(
            choiceContext,
            player.Creature,
            2m,
            player.Creature,
            null);

        MonsterDivinityPower playerDivinity = player.Creature.GetPower<MonsterDivinityPower>()
            ?? throw new InvalidOperationException("10 player Melodia did not enter Divinity.");
        Require(player.Creature.GetPower<MelodiaPower>() is null,
            "player Melodia was not reduced by exactly 10 at threshold");
        Require(playerState.Energy - energyBefore == SakikoStanceCmd.DivinityEnergyGain,
            "player Divinity did not grant exactly 3 Energy on entry");
        Require(blockBeforeMelodia - target.Block == 4m,
            "Dazzling did not trigger once per Melodia gain or incorrectly triggered for Divinity");
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());

        decimal targetBlockBeforeAttack = target.Block;
        await CreatureCmd.Damage(
            choiceContext,
            target,
            2m,
            ValueProp.Move,
            player.Creature);
        Require(targetBlockBeforeAttack - target.Block == 6m,
            "player Divinity did not triple powered Attack damage");

        decimal energyBeforeReentry = playerState.Energy;
        DivinityEntryResult reentry = await SakikoStanceCmd.EnterDivinityAsync(
            choiceContext,
            player.Creature,
            player.Creature,
            null);
        Require(reentry.Status == DivinityEntryStatus.AlreadyActive,
            "Divinity re-entry did not preserve the active single instance");
        Require(playerState.Energy == energyBeforeReentry,
            "Divinity re-entry granted Energy twice");
        await playerDivinity.AfterSideTurnEnd(choiceContext, CombatSide.Player, [player.Creature]);
        Require(player.Creature.GetPower<MonsterDivinityPower>() is null,
            "player Divinity did not expire at player-side turn end");

        decimal energyBeforeEnemyDivinity = playerState.Energy;
        await PowerCmd.Apply<MelodiaPower>(choiceContext, target, 10m, target, null);
        MonsterDivinityPower enemyDivinity = target.GetPower<MonsterDivinityPower>()
            ?? throw new InvalidOperationException("10 enemy Melodia did not enter Divinity.");
        Require(target.GetPower<MelodiaPower>() is null,
            "enemy Melodia was not reduced by exactly 10 at threshold");
        Require(playerState.Energy == energyBeforeEnemyDivinity,
            "enemy Divinity incorrectly granted player Energy");
        decimal playerBlockBeforeAttack = player.Creature.Block;
        await CreatureCmd.Damage(
            choiceContext,
            player.Creature,
            2m,
            ValueProp.Move,
            target);
        Require(playerBlockBeforeAttack - player.Creature.Block == 6m,
            "enemy Divinity did not triple powered Attack damage");
        await enemyDivinity.AfterSideTurnEnd(choiceContext, CombatSide.Enemy, [target]);
        Require(target.GetPower<MonsterDivinityPower>() is null,
            "enemy Divinity did not expire at enemy-side turn end");

        if (playerStrengthBefore != 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                player.Creature,
                playerStrengthBefore,
                player.Creature,
                null);
        }
        if (targetStrengthBefore != 0)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, target, targetStrengthBefore, target, null);
        }
        await RemoveAddedBlockAsync(choiceContext, player.Creature, playerBlockBefore);
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);
    }

    private static async Task VerifyRadianceAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        // Each gain past the first fires Dazzling at a random enemy; Block keeps the fight alive.
        Creature[] opponents = combatState.GetOpponentsOf(player.Creature)
            .Where(creature => creature.IsHittable)
            .ToArray();
        Dictionary<Creature, int> blockBefore = opponents.ToDictionary(creature => creature, creature => creature.Block);
        foreach (Creature opponent in opponents)
        {
            await CreatureCmd.GainBlock(opponent, 100m, ValueProp.Unpowered, null, fast: true);
        }

        decimal amountBefore = player.Creature.GetPower<DazzlingPower>()?.Amount ?? 0m;
        RadianceCard baseCard = await CreateAndAutoPlayAsync<RadianceCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        RadianceCard upgradedCard = await CreateAndAutoPlayAsync<RadianceCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        decimal amountAfter = player.Creature.GetPower<DazzlingPower>()?.Amount ?? 0m;
        Require(amountAfter - amountBefore == 10m,
            $"Radiance did not apply two Dazzling per repetition (gained {amountAfter - amountBefore})");
        Require(baseCard.Pile is null && upgradedCard.Pile is null, "Radiance power cards did not leave combat piles");
        foreach (Creature opponent in opponents)
        {
            await RemoveAddedBlockAsync(choiceContext, opponent, blockBefore[opponent]);
        }
    }

    private static async Task VerifyHeartsBarrierAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        decimal blockBefore = player.Creature.Block;
        int deckSizeBefore = player.Deck.Cards.Count;
        HeartsBarrierCard baseCard = combatState.CreateCard<HeartsBarrierCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(
            baseCard,
            PileType.Draw,
            player,
            CardPilePosition.Top);
        Require(baseCard.DynamicVars.CalculatedBlock.Calculate(null) == deckSizeBefore,
            "base Heart's Barrier preview did not match the persistent deck size");
        await CardCmd.AutoPlay(
            choiceContext,
            baseCard,
            null,
            AutoPlayType.Default,
            skipCardPileVisuals: true);

        CardModel persistentProbe = await AddPersistentProbeAsync<DefendTogawaSakiko>(player);
        int upgradedDeckSize = player.Deck.Cards.Count;
        Require(upgradedDeckSize == deckSizeBefore + 1,
            "Heart's Barrier setup did not change the persistent deck size by one");
        IReadOnlyList<CardModel> probeCombatCopies = PersistentDeckMutation.SnapshotLinkedCombatCopies(persistentProbe);
        Require(probeCombatCopies.Count == 1 && probeCombatCopies.Single().Pile?.Type == PileType.Hand,
            "Heart's Barrier persistent probe did not add exactly one linked combat copy to Hand");
        HeartsBarrierCard upgradedCard = combatState.CreateCard<HeartsBarrierCard>(player);
        CardCmd.Upgrade(upgradedCard, CardPreviewStyle.None);
        await CardPileCmd.AddGeneratedCardToCombat(
            upgradedCard,
            PileType.Draw,
            player,
            CardPilePosition.Top);
        Require(upgradedCard.DynamicVars.CalculatedBlock.Calculate(null) == upgradedDeckSize,
            "upgraded Heart's Barrier preview did not use the changed persistent deck size");
        Require(upgradedCard.ShouldRetainThisTurn,
            "upgraded Heart's Barrier did not use native Retain");
        await CardCmd.AutoPlay(
            choiceContext,
            upgradedCard,
            null,
            AutoPlayType.Default,
            skipCardPileVisuals: true);

        Require(player.Creature.Block - blockBefore == deckSizeBefore + upgradedDeckSize,
            "Heart's Barrier did not gain exact play-time persistent-deck-sized Block");
        Require(baseCard.Pile?.Type == PileType.Discard && upgradedCard.Pile?.Type == PileType.Discard,
            "Heart's Barrier cards did not enter Discard");

        PersistentDeckRemovalResult cleanup = await PersistentDeckMutation.RemoveAsync(
            persistentProbe,
            showPersistentPreview: false,
            skipCombatVisuals: true);
        Require(cleanup.Success && cleanup.RemovedCombatCopies.Count == 1 &&
                ReferenceEquals(cleanup.RemovedCombatCopies.Single(), probeCombatCopies.Single()),
            "Heart's Barrier persistent deck probe cleanup did not remove its exact linked combat copy");
        await CardPileCmd.RemoveFromCombat([baseCard, upgradedCard], skipVisuals: true);
        await RemoveAddedBlockAsync(choiceContext, player.Creature, blockBefore);
    }

    private static async Task VerifyAccompliceAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        CombatCardLocation[] cardsMovedAside = await MoveCombatPilesAsideAsync(
            player,
            PileType.Hand,
            PileType.Draw,
            PileType.Discard);
        int deckSizeBefore = player.Deck.Cards.Count;
        int gainHistoryBefore = GetGainHistoryCount(player);

        DesireCard existingDesire = combatState.CreateCard<DesireCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(existingDesire, PileType.Hand, player);
        AccompliceCard baseAccomplice = await CreateAndAutoPlayAsync<AccompliceCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        AccompliceCard upgradedAccomplice = await CreateAndAutoPlayAsync<AccompliceCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        DesireCard[] desires = PileType.Hand.GetPile(player).Cards.OfType<DesireCard>().ToArray();
        Require(desires.Length == 6,
            $"Accomplice expected one existing plus five generated Desires, found {desires.Length}");
        Require(desires.Contains(existingDesire, ReferenceEqualityComparer.Instance),
            "Accomplice lost the pre-existing Desire in hand");
        Require(desires.All(card => card.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0),
            "Accomplice did not make every Desire in hand free this turn");
        Require(baseAccomplice.Pile?.Type == PileType.Discard &&
                upgradedAccomplice.Pile?.Type == PileType.Discard,
            "Accomplice cards did not enter Discard");
        Require(player.Deck.Cards.Count == deckSizeBefore && GetGainHistoryCount(player) == gainHistoryBefore,
            "Accomplice changed the persistent deck");
        await RemoveCombatCardsAsync(
            desires.Cast<CardModel>().Append(baseAccomplice).Append(upgradedAccomplice));
        await RestoreCombatCardsAsync(cardsMovedAside);
    }

    private static async Task VerifyDesuWaAsync(
        CombatState combatState, Player player, Creature target, PlayerChoiceContext choiceContext)
    {
        CombatCardLocation[] moved = await MoveCombatPilesAsideAsync(player, PileType.Hand, PileType.Draw, PileType.Discard);
        foreach (bool upgraded in new[] { false, true })
        {
            CardModel first = combatState.CreateCard<DefendTogawaSakiko>(player);
            CardModel second = combatState.CreateCard<StrikeTogawaSakiko>(player);
            await AddDrawWindowAsync([first, second]);
            DesuWaCard card = await CreateAndAutoPlayAsync<DesuWaCard>(combatState, player, choiceContext, null, upgraded);
            Require(first.Pile?.Type == PileType.Hand && second.Pile?.Type == PileType.Draw,
                "Desu Wa must draw exactly the top card regardless of type");
            Require(card.Pile?.Type == (upgraded ? PileType.Discard : PileType.Exhaust),
                "Desu Wa exhaust/upgrade destination incorrect");
            await RemoveCombatCardsAsync([first, second, card]);
        }
        await RestoreCombatCardsAsync(moved);
    }

    private static async Task<T> CreateAndAutoPlayAsync<T>(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext,
        Creature? target,
        bool upgraded)
        where T : CardModel
    {
        T card = combatState.CreateCard<T>(player);
        if (upgraded)
        {
            CardCmd.Upgrade(card, CardPreviewStyle.None);
        }
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, player, CardPilePosition.Top);
        await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipCardPileVisuals: true);
        return card;
    }

    private static async Task<CardModel> AddPersistentProbeAsync<TCard>(Player player)
        where TCard : CardModel
    {
        CardPileAddResult result = await PersistentDeckMutation.AddCanonicalAsync<TCard>(
            player,
            skipVisuals: true);
        Require(result.success && result.cardAdded.Pile?.Type == PileType.Deck,
            $"native persistent probe add failed for {typeof(TCard).Name}");
        return result.cardAdded;
    }

    private static async Task AddDrawWindowAsync(IReadOnlyList<CardModel> cards)
    {
        foreach (CardModel card in cards.Reverse())
        {
            CardPileAddResult result = await CardPileCmd.Add(
                card,
                PileType.Draw,
                CardPilePosition.Top,
                skipVisuals: true);
            Require(result.success && ReferenceEquals(result.cardAdded, card),
                $"draw-window setup failed for {card.Id}");
        }
    }

    private static async Task<CombatCardLocation[]> MoveCombatPilesAsideAsync(
        Player player,
        params PileType[] pileTypes)
    {
        CombatCardLocation[] locations = pileTypes
            .SelectMany(pileType => pileType.GetPile(player).Cards
                .Select(card => new CombatCardLocation(card, pileType)))
            .ToArray();
        foreach (CombatCardLocation location in locations)
        {
            CardPileAddResult move = await CardPileCmd.Add(
                location.Card,
                PileType.Exhaust,
                CardPilePosition.Bottom,
                skipVisuals: false);
            Require(move.success && location.Card.Pile?.Type == PileType.Exhaust,
                $"diagnostic setup could not move {location.Card.Id} out of {location.PileType}");
        }

        return locations;
    }

    private static async Task RestoreCombatCardsAsync(IEnumerable<CombatCardLocation> locations)
    {
        foreach (CombatCardLocation location in locations)
        {
            CardPileAddResult restore = await CardPileCmd.Add(
                location.Card,
                location.PileType,
                CardPilePosition.Bottom,
                skipVisuals: false);
            Require(restore.success && location.Card.Pile?.Type == location.PileType,
                $"diagnostic cleanup could not restore {location.Card.Id} to {location.PileType}");
        }
    }

    private static async Task RemoveCombatCardsAsync(IEnumerable<CardModel> cards)
    {
        CardModel[] removable = cards
            .Distinct<CardModel>(ReferenceEqualityComparer.Instance)
            .Where(card => !card.HasBeenRemovedFromState && card.Pile is { IsCombatPile: true })
            .ToArray();
        if (removable.Length > 0)
        {
            await CardPileCmd.RemoveFromCombat(removable, skipVisuals: false);
        }
    }

    private static int GetGainHistoryCount(Player player)
    {
        return player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).CardsGained.Count
            ?? throw new InvalidOperationException("Phase N5 persistent-gain history is unavailable.");
    }

    private static async Task RemoveAddedBlockAsync(
        PlayerChoiceContext choiceContext,
        Creature creature,
        decimal amountBefore)
    {
        decimal addedBlockRemaining = creature.Block - amountBefore;
        if (addedBlockRemaining > 0m)
        {
            await CreatureCmd.LoseBlock(choiceContext, creature, addedBlockRemaining, null);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Phase N5 actual-game contract failed: " + message + ".");
        }
    }
}
