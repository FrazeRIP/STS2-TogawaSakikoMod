using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Models.Relics;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static partial class N5BatchDiagnostics
{
    private sealed class Session
    {
        public bool WaterExtraTurnArmed { get; set; }

        public bool WaterExtraTurnHookSeen { get; set; }

        public Player? WaterOwner { get; set; }

        public int WaterTurnNumberBefore { get; set; }

        public int WaterRoundNumberBefore { get; set; }

        public int WaterAmbergrisAmountBefore { get; set; }

        public CombatCardLocation[] WaterCardsMovedAside { get; set; } = [];
    }

    private readonly record struct CombatCardLocation(CardModel Card, PileType PileType);

    private static readonly ConditionalWeakTable<CombatState, Session> Sessions = new();

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
        Session session = new();
        Sessions.Add(combatState, session);

        Player player = sourceCard.Owner;
        Creature target = combatState.GetOpponentsOf(player.Creature).First(creature => creature.IsHittable);

        StartingOptionsDiagnostics.Validate(player);
        await VerifyDazzlingPriorStacksAsync(combatState, player, target, choiceContext);
        await VerifyMelodyAsync(combatState, player, target, choiceContext);
        await VerifyGreetingsAsync(combatState, player, choiceContext);
        await VerifyTirednessAsync(combatState, player, choiceContext);
        await VerifyIdealAsync(combatState, player, choiceContext);
        await VerifyAmorisAsync(combatState, player);
        await VerifyDolorisAsync(combatState, player, choiceContext);
        await VerifyMortisAsync(combatState, player, target, choiceContext);
        await VerifyTimorisAsync(combatState, player, choiceContext);
        await VerifyOblivionisAsync(combatState, player, target, choiceContext);
        await VerifyDormantCursePowerScaffoldsAsync(player, target, choiceContext);
        await VerifyKindnessAsync(combatState, player, choiceContext);
        await VerifyBlackAndWhiteKeysAsync(combatState, player, target, choiceContext);
        await VerifyMelodiaAndDivinityAsync(combatState, player, target, choiceContext);
        await VerifyMementoMoriAsync(combatState, player, target, choiceContext);
        await VerifyPersistentGenerationCommonCardsAsync(combatState, player, target, choiceContext);
        await VerifyStrengthTradeAndDeckBlockCardsAsync(combatState, player, target, choiceContext);
        await VerifySelectionAndRetrievalCommonCardsAsync(combatState, player, target, choiceContext);
        await VerifyQuaerereLuminaAsync(combatState, player, choiceContext);
        await VerifyKingsAsync(combatState, player, target, choiceContext);
        await VerifyAccompliceAndCarefreeAsync(combatState, player, choiceContext);
        await VerifyDesuWaAsync(combatState, player, target, choiceContext);
        await VerifyEdgeMasqueradeAndWeaknessAsync(combatState, player, target, choiceContext);
        await VerifyUncommonDirectCardsAsync(combatState, player, target, choiceContext);
        await VerifyProtectionAsync(combatState, player, choiceContext);
        await VerifyRadianceAsync(combatState, player, choiceContext);
        await VerifyRemainingUncommonCoreAsync(combatState, player, target, choiceContext);
        await VerifyRemainingUncommonHooksAsync(combatState, player, target, choiceContext);
        await VerifyRemainingUncommonMutationsAsync(combatState, player, choiceContext);
        await VerifyRareCoreAsync(combatState, player, target, choiceContext);
        await VerifyRarePowersAsync(combatState, player, target, choiceContext);
        await VerifyRareMutationsAsync(combatState, player, target, choiceContext);
        await VerifyRareFinaleAsync(combatState, player, choiceContext);
        await VerifyFeedbackGameplayAsync(combatState, player, target, choiceContext);
        await VerifySymbolIIIWaterAsync(combatState, player, choiceContext, session);

        NativeSmokeTrace.N5Info(
            "native card batches passed. GreetingsEnergy=5, TirednessDraw=3, MelodyDamage=15, IdealFreeAttacks=5, Artifact=5, Dazzling=10, KindnessCurrentAndPriorLossSelection=passed, Keys=Black5+White5, MelodiaDivinity=PlayerEnemyTriple+Voice10+InnerCry7, MementoMori=BaseExhaust6+UpgradePurge5+CombatOnly1+Triple60, PersistentAdds=Tiredness2+Radiance2+Ideal2+Voice2+Amoris2+Mortis2, CommonDamage=Phantom24+24+28+Symbol28, Regen=9, SymbolDraw=7, StrengthTrade=Dark27+ActualSteal3+Georgette27+EnemyStrength2+EnemyHype1, HeartsBarrier=DeckSizedBlock+Retain, SelectionCards=Daten30+ExactPersistentPurge2+Kill19+DesireRetrieve3+EarthProjectedBlockDamage, Quaerere=Scry7+9+DiscardBlock7, Kings=Damage34+Single+Reward2+Reroll2+Clear3+Saved, CommonTail=Accomplice5+CarefreeRetain2+DesuWaDrawPriority3+EdgeFrail3+PurgeGeneratedAndPersistent+MasqueradeDamage8+SavedGrowth+WeaknessDiscard, UncommonDirect=Gold35+Tiredness2+Dazzling16+Plating16+Mutsumi12AndBlock12+Protection2+SoyoAoE14+Kindness2+RhinoBlock17WithoutDexterity+CountingBuffTypes, Curses=AmorisRetain+DolorisBlockable2+MortisInjury+OblivionisHandExhaust+TimorisVulnerable, RemainingUncommon=20Cards+10Powers, VoiceRoutes=25.");
    }

    private static async Task VerifyMelodyAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal blockBefore = target.Block;
        MelodyCard baseCard = await CreateAndAutoPlayAsync<MelodyCard>(combatState, player, choiceContext, target, upgraded: false);
        MelodyCard upgradedCard = await CreateAndAutoPlayAsync<MelodyCard>(combatState, player, choiceContext, target, upgraded: true);
        Require(blockBefore - target.Block == 15m, $"Melody expected 15 total damage, found {blockBefore - target.Block}");
        Require(baseCard.Pile?.Type == PileType.Discard, "base Melody did not enter Discard");
        Require(upgradedCard.Pile?.Type == PileType.Discard, "upgraded Melody did not enter Discard");
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

    private static async Task VerifyTirednessAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        CardModel[] drawProbes =
        [
            combatState.CreateCard<DefendTogawaSakiko>(player),
            combatState.CreateCard<DefendTogawaSakiko>(player),
            combatState.CreateCard<DefendTogawaSakiko>(player)
        ];
        foreach (CardModel probe in drawProbes)
        {
            await CardPileCmd.AddGeneratedCardToCombat(probe, PileType.Draw, player, CardPilePosition.Top);
        }

        TirednessCard baseCard = await CreateAndAutoPlayAsync<TirednessCard>(combatState, player, choiceContext, null, upgraded: false);
        TirednessCard upgradedCard = await CreateAndAutoPlayAsync<TirednessCard>(combatState, player, choiceContext, null, upgraded: true);
        Require(drawProbes.All(card => card.Pile?.Type == PileType.Hand), "Tiredness did not draw exactly three prepared cards");
        Require(baseCard.Pile?.Type == PileType.Exhaust, "base Tiredness did not Exhaust");
        Require(upgradedCard.Pile?.Type == PileType.Exhaust, "upgraded Tiredness did not Exhaust");
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

    private static async Task VerifyAmorisAsync(CombatState combatState, Player player)
    {
        AmorisCard card = combatState.CreateCard<AmorisCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        Require(card.Pile?.Type == PileType.Hand, "Amoris was not added to Hand");
        Require(card.ShouldRetainThisTurn, "Amoris did not use native Retain");
        Require(!card.HasTurnEndInHandEffect, "Amoris would leave Hand through the turn-end play queue");
        await CardPileCmd.RemoveFromCombat(card, skipVisuals: false);
    }

    private static async Task VerifyDolorisAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        decimal initialBlock = player.Creature.Block;
        decimal hpBefore = player.Creature.CurrentHp;
        await CreatureCmd.GainBlock(player.Creature, 5m, ValueProp.Unpowered, null, fast: true);

        DolorisCard card = combatState.CreateCard<DolorisCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        await card.OnTurnEndInHandWrapper(choiceContext);

        Require(player.Creature.CurrentHp == hpBefore, "Doloris bypassed Block");
        Require(player.Creature.Block == initialBlock + 3m, "Doloris did not deal exactly 2 blockable damage");
        Require(player.Creature.GetPower<DolorisPower>() is null, "Doloris applied its commented-out power scaffold");

        await CardPileCmd.RemoveFromCombat(card, skipVisuals: false);
        await CreatureCmd.LoseBlock(choiceContext, player.Creature, 3m, null);
    }

    private static async Task VerifyMortisAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        MortisCard card = combatState.CreateCard<MortisCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        await card.OnTurnEndInHandWrapper(choiceContext);

        MortisPower power = player.Creature.GetPower<MortisPower>()
            ?? throw new InvalidOperationException("Mortis did not apply MortisPower.");
        Require(power.Amount == 1, "Mortis power amount was not 1");
        Require(!power.SkipNextDurationTick, "Mortis incorrectly skipped its same-round duration tick");

        HashSet<CardModel> drawCardsBefore = PileType.Draw.GetPile(player).Cards.ToHashSet();
        decimal hpBefore = player.Creature.CurrentHp;
        await CreatureCmd.Damage(
            choiceContext,
            player.Creature,
            1m,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            player.Creature);
        CardModel[] generatedInjuries = PileType.Draw.GetPile(player).Cards
            .Where(candidate => candidate is Injury && !drawCardsBefore.Contains(candidate))
            .ToArray();
        Require(player.Creature.CurrentHp == hpBefore - 1m, "Mortis damage probe did not lose exactly 1 HP");
        Require(generatedInjuries.Length == 1, "Mortis did not create exactly one Injury per HP-loss event");
        Require(generatedInjuries[0].Owner == player, "Mortis generated Injury for the wrong owner");

        await power.AfterSideTurnEnd(choiceContext, CombatSide.Enemy, [target]);
        Require(player.Creature.GetPower<MortisPower>() is null, "Mortis did not expire at the following enemy-side end");

        await CardPileCmd.RemoveFromCombat(card, skipVisuals: false);
        await CardPileCmd.RemoveFromCombat(generatedInjuries, skipVisuals: true);
        await CreatureCmd.Heal(player.Creature, 1m, playAnim: false);
    }

    private static async Task VerifyTimorisAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        int vulnerableBefore = player.Creature.GetPower<VulnerablePower>()?.Amount ?? 0;
        TimorisCard card = combatState.CreateCard<TimorisCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        await card.OnTurnEndInHandWrapper(choiceContext);

        TimorisPower power = player.Creature.GetPower<TimorisPower>()
            ?? throw new InvalidOperationException("Timoris did not apply TimorisPower.");
        Require(power.Amount == 1, "Timoris power amount was not 1");

        await power.BeforeSideTurnStart(
            choiceContext,
            CombatSide.Player,
            [player.Creature],
            combatState);
        VulnerablePower vulnerable = player.Creature.GetPower<VulnerablePower>()
            ?? throw new InvalidOperationException("Timoris did not convert into Vulnerable.");
        Require(player.Creature.GetPower<TimorisPower>() is null, "Timoris power remained after conversion");
        Require(vulnerable.Amount == vulnerableBefore + 1, "Timoris did not preserve its amount during conversion");
        Require(!vulnerable.SkipNextDurationTick, "Timoris Vulnerable would survive the following enemy-side end");

        await PowerCmd.ModifyAmount(choiceContext, vulnerable, -1m, player.Creature, card);
        await CardPileCmd.RemoveFromCombat(card, skipVisuals: false);
    }

    private static async Task VerifyOblivionisAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.GainBlock(target, 20m, ValueProp.Unpowered, null, fast: true);

        OblivionisCard handCurse = combatState.CreateCard<OblivionisCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(handCurse, PileType.Hand, player);
        MelodyCard exhaustedAttack = await CreateAndAutoPlayAsync<MelodyCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        Require(exhaustedAttack.Pile?.Type == PileType.Exhaust, "Oblivionis in Hand did not Exhaust a played Attack");
        Require(player.Creature.GetPower<OblivionisPower>() is null, "Oblivionis used its commented-out power scaffold");
        await CardPileCmd.RemoveFromCombat([handCurse, exhaustedAttack], skipVisuals: false);

        OblivionisCard drawCurse = combatState.CreateCard<OblivionisCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(drawCurse, PileType.Draw, player, CardPilePosition.Bottom);
        MelodyCard discardedAttack = await CreateAndAutoPlayAsync<MelodyCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        Require(discardedAttack.Pile?.Type == PileType.Discard, "Oblivionis outside Hand affected a played Attack");
        await CardPileCmd.RemoveFromCombat([drawCurse, discardedAttack], skipVisuals: true);
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

    private static async Task VerifyProtectionAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        decimal amountBefore = player.Creature.GetPower<ArtifactPower>()?.Amount ?? 0m;
        ProtectionCard baseCard = await CreateAndAutoPlayAsync<ProtectionCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        ProtectionCard upgradedCard = await CreateAndAutoPlayAsync<ProtectionCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        decimal amountAfter = player.Creature.GetPower<ArtifactPower>()?.Amount ?? 0m;
        Require(amountAfter - amountBefore == 5m, "Protection did not apply Artifact once per repetition");
        Require(baseCard.Pile is null && upgradedCard.Pile is null, "Protection power cards did not leave combat piles");
    }

    private static async Task VerifyKindnessAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        StrengthPower strength = await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                player.Creature,
                4m,
                player.Creature,
                null)
            ?? throw new InvalidOperationException("Could not prepare Strength for Kindness diagnostic.");
        await PowerCmd.ModifyAmount(choiceContext, strength, -3m, player.Creature, null);

        AccuracyPower accuracy = await PowerCmd.Apply<AccuracyPower>(
                choiceContext,
                player.Creature,
                2m,
                player.Creature,
                null)
            ?? throw new InvalidOperationException("Could not prepare Accuracy for Kindness diagnostic.");
        await PowerCmd.Remove(accuracy);

        PowerChangeLedger ledger = PowerChangeLedgerService.GetLedger(combatState);
        var selection = LostPowerRestorationCommand.SelectLostBuffs(
            ledger,
            combatState.RoundNumber,
            CreatureIdentity.FromCreature(player.Creature),
            includePreviousRound: true);
        Require(selection.Single(item => item.PowerModelId == ModelDb.GetId<StrengthPower>()).Amount == 3,
            "Kindness did not select the actual partial Strength reduction");
        Require(selection.Single(item => item.PowerModelId == ModelDb.GetId<AccuracyPower>()).Amount == 2,
            "Kindness did not select the full Accuracy removal");

        KindnessCard baseCard = await CreateAndAutoPlayAsync<KindnessCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        Require(player.Creature.GetPower<StrengthPower>()?.Amount == 4,
            "base Kindness did not restore the partial Strength reduction");
        Require(player.Creature.GetPower<AccuracyPower>()?.Amount == 2,
            "base Kindness did not restore the directly removed Accuracy power");

        strength = player.Creature.GetPower<StrengthPower>()
            ?? throw new InvalidOperationException("Kindness diagnostic lost its restored Strength power.");
        await PowerCmd.ModifyAmount(choiceContext, strength, -1m, player.Creature, null);
        KindnessCard upgradedCard = await CreateAndAutoPlayAsync<KindnessCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        Require(player.Creature.GetPower<StrengthPower>()?.Amount == 7,
            "upgraded Kindness did not restore all aggregated same-turn Strength losses");
        Require(player.Creature.GetPower<AccuracyPower>()?.Amount == 4,
            "upgraded Kindness did not restore the prior full removal again");
        Require(baseCard.Pile is null && upgradedCard.Pile is null, "Kindness power cards did not leave combat piles");
    }

    private static async Task VerifyBlackAndWhiteKeysAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        decimal playerBlockBefore = player.Creature.Block;
        decimal targetBlockBefore = target.Block;
        decimal playerStrengthBefore = player.Creature.GetPower<StrengthPower>()?.Amount ?? 0m;
        decimal playerDazzlingBefore = player.Creature.GetPower<DazzlingPower>()?.Amount ?? 0m;
        decimal targetStrengthBefore = target.GetPower<StrengthPower>()?.Amount ?? 0m;
        decimal targetDazzlingBefore = target.GetPower<DazzlingPower>()?.Amount ?? 0m;

        await CreatureCmd.GainBlock(player.Creature, 100m, ValueProp.Unpowered, null, fast: true);
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);

        BlackAndWhiteKeysCard[] playedCards =
        [
            await CreateAndAutoPlayKeysAsync(combatState, player, choiceContext, target, upgraded: false, choiceIndex: 0),
            await CreateAndAutoPlayKeysAsync(combatState, player, choiceContext, target, upgraded: true, choiceIndex: 0),
            await CreateAndAutoPlayKeysAsync(combatState, player, choiceContext, target, upgraded: false, choiceIndex: 1),
            await CreateAndAutoPlayKeysAsync(combatState, player, choiceContext, target, upgraded: true, choiceIndex: 1)
        ];

        Require((player.Creature.GetPower<StrengthPower>()?.Amount ?? 0m) - playerStrengthBefore == 5m,
            "Black Keys did not apply base plus upgraded Strength to the player");
        Require((target.GetPower<DazzlingPower>()?.Amount ?? 0m) - targetDazzlingBefore == 5m,
            "Black Keys did not apply base plus upgraded Dazzling to the selected enemy");
        Require((player.Creature.GetPower<DazzlingPower>()?.Amount ?? 0m) - playerDazzlingBefore == 5m,
            "White Keys did not apply base plus upgraded Dazzling to the player");
        Require((target.GetPower<StrengthPower>()?.Amount ?? 0m) - targetStrengthBefore == 5m,
            "White Keys did not apply base plus upgraded Strength to the selected enemy");
        Require(playedCards.All(card => card.Pile?.Type == PileType.Discard),
            "Black and White Keys parent cards did not enter Discard");

        await RestorePowerAmountAsync(
            choiceContext,
            player.Creature.GetPower<DazzlingPower>(),
            playerDazzlingBefore,
            player.Creature);
        await RestorePowerAmountAsync(
            choiceContext,
            target.GetPower<DazzlingPower>(),
            targetDazzlingBefore,
            player.Creature);
        await RestorePowerAmountAsync(
            choiceContext,
            player.Creature.GetPower<StrengthPower>(),
            playerStrengthBefore,
            player.Creature);
        await RestorePowerAmountAsync(
            choiceContext,
            target.GetPower<StrengthPower>(),
            targetStrengthBefore,
            player.Creature);

        await CardPileCmd.RemoveFromCombat(playedCards, skipVisuals: true);
        await RemoveAddedBlockAsync(choiceContext, player.Creature, playerBlockBefore);
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);
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

        PowerChangeLedger ledger = PowerChangeLedgerService.GetLedger(combatState);
        int eventsBeforeVoice = ledger.Snapshot(combatState.RoundNumber).Length;
        decimal energyBeforeVoice = playerState.Energy;
        VoiceCard baseVoice = await CreateAndAutoPlayAsync<VoiceCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        VoiceCard upgradedVoice = await CreateAndAutoPlayAsync<VoiceCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        MonsterDivinityPower voiceDivinity = player.Creature.GetPower<MonsterDivinityPower>()
            ?? throw new InvalidOperationException("Voice did not enter Divinity after 10 total Melodia.");
        PowerChangeEvent[] voiceMelodiaEvents = ledger.Snapshot(combatState.RoundNumber)
            .Skip(eventsBeforeVoice)
            .Where(powerEvent => powerEvent.PowerModelId == ModelDb.GetId<MelodiaPower>())
            .ToArray();
        Require(voiceMelodiaEvents.Count(powerEvent => powerEvent.Delta == 2m) == 5,
            "Voice did not use five separate 2-Melodia native applications");
        Require(voiceMelodiaEvents.Count(powerEvent => powerEvent.Delta == -10m) == 1,
            "Voice threshold did not consume exactly 10 Melodia once");
        Require(voiceMelodiaEvents.Where(powerEvent => powerEvent.Delta > 0m)
                .All(powerEvent => powerEvent.CardSource?.ModelId == ModelDb.GetId<VoiceCard>()),
            "Voice Melodia ledger events lost their card source");
        Require(playerState.Energy - energyBeforeVoice == SakikoStanceCmd.DivinityEnergyGain,
            "Voice Divinity did not grant exactly 3 Energy");
        Require(baseVoice.Pile is null && upgradedVoice.Pile is null,
            "Voice power cards did not leave combat piles");
        await voiceDivinity.AfterSideTurnEnd(choiceContext, CombatSide.Player, [player.Creature]);

        decimal blockBeforeInnerCry = player.Creature.Block;
        InnerCryCard baseInnerCry = await CreateAndAutoPlayAsync<InnerCryCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        InnerCryCard upgradedInnerCry = await CreateAndAutoPlayAsync<InnerCryCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        Require(player.Creature.GetPower<MelodiaPower>()?.Amount == 7,
            "Inner Cry did not apply base plus upgraded Melodia");
        Require(player.Creature.Block - blockBeforeInnerCry == 18m,
            "Inner Cry did not gain base plus upgraded Block");
        Require(player.Creature.GetPower<MonsterDivinityPower>() is null,
            "Inner Cry entered Divinity below 10 Melodia");
        Require(baseInnerCry.Pile?.Type == PileType.Discard && upgradedInnerCry.Pile?.Type == PileType.Discard,
            "Inner Cry cards did not enter Discard");

        await PowerCmd.Remove(player.Creature.GetPower<MelodiaPower>());
        await CardPileCmd.RemoveFromCombat([baseInnerCry, upgradedInnerCry], skipVisuals: true);
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
        Require(amountAfter - amountBefore == 10m, "Radiance did not apply two Dazzling per repetition");
        Require(baseCard.Pile is null && upgradedCard.Pile is null, "Radiance power cards did not leave combat piles");
    }

    private static async Task VerifyMementoMoriAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        PlayerCombatState playerState = player.PlayerCombatState
            ?? throw new InvalidOperationException("Phase N5 Memento Mori diagnostic requires player combat state.");
        decimal targetBlockBefore = target.Block;
        int strengthBefore = player.Creature.GetPower<StrengthPower>()?.Amount ?? 0;
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);

        CardModel[] baseWindow = Enumerable.Range(0, 8)
            .Select(_ => combatState.CreateCard<DefendTogawaSakiko>(player))
            .ToArray();
        baseWindow[2].AddKeyword(CardKeyword.Eternal);
        await AddDrawWindowAsync(baseWindow);
        HashSet<CardModel> expectedExhausted = new(
            baseWindow.Take(7).Where(card => card != baseWindow[2]),
            ReferenceEqualityComparer.Instance);
        int exhaustEvents = 0;
        bool baseOrderingPassed = true;
        decimal blockBeforeBase = target.Block;
        void OnCardExhausted(CardModel card)
        {
            if (!expectedExhausted.Contains(card))
            {
                return;
            }

            exhaustEvents++;
            baseOrderingPassed &= player.Creature.GetPower<MonsterDivinityPower>() is null &&
                                  target.Block == blockBeforeBase;
        }

        CardPile exhaustPile = PileType.Exhaust.GetPile(player);
        exhaustPile.CardAdded += OnCardExhausted;
        MementoMoriCard baseCard;
        decimal energyBefore = playerState.Energy;
        try
        {
            baseCard = await CreateAndAutoPlayAsync<MementoMoriCard>(
                combatState,
                player,
                choiceContext,
                target,
                upgraded: false);
        }
        finally
        {
            exhaustPile.CardAdded -= OnCardExhausted;
        }

        Require(exhaustEvents == 6 && expectedExhausted.All(card => card.Pile?.Type == PileType.Exhaust),
            "base Memento Mori did not Exhaust the six removable cards in its top-seven window");
        Require(baseWindow[2].Pile?.Type == PileType.Draw && baseWindow[7].Pile?.Type == PileType.Draw,
            "base Memento Mori removed Eternal or backfilled past the top-seven window");
        Require(baseOrderingPassed, "base Memento Mori did not finish draw-pile Exhausts before Divinity and damage");
        Require(blockBeforeBase - target.Block == 21m,
            "base Memento Mori did not enter Divinity before dealing 7 damage");
        Require(playerState.Energy - energyBefore == SakikoStanceCmd.DivinityEnergyGain,
            "base Memento Mori Divinity did not grant 3 Energy");
        Require(baseCard.Pile?.Type == PileType.Discard, "base Memento Mori did not enter Discard");
        MonsterDivinityPower baseDivinity = player.Creature.GetPower<MonsterDivinityPower>()
            ?? throw new InvalidOperationException("Base Memento Mori did not enter Divinity.");
        await baseDivinity.AfterSideTurnEnd(choiceContext, CombatSide.Player, [player.Creature]);
        await CardPileCmd.RemoveFromCombat([.. baseWindow, baseCard], skipVisuals: true);

        CardModel[] persistentCards = new CardModel[7];
        for (int index = 0; index < persistentCards.Length; index++)
        {
            persistentCards[index] = await AddPersistentProbeAsync<DefendTogawaSakiko>(player);
        }
        persistentCards[2].AddKeyword(CardKeyword.Eternal);

        CardModel[] linkedCopies = persistentCards
            .Select(persistentCard =>
            {
                CardModel copy = combatState.CloneCard(persistentCard);
                copy.DeckVersion = persistentCard;
                return copy;
            })
            .ToArray();
        CardModel combatOnlyCard = combatState.CreateCard<DefendTogawaSakiko>(player);
        CardModel[] upgradedWindow =
        [
            linkedCopies[0],
            linkedCopies[1],
            linkedCopies[2],
            linkedCopies[3],
            linkedCopies[4],
            linkedCopies[5],
            combatOnlyCard,
            linkedCopies[6]
        ];
        await AddDrawWindowAsync(upgradedWindow);

        HashSet<CardModel> expectedPersistentRemovals = new(
            [persistentCards[0], persistentCards[1], persistentCards[3], persistentCards[4], persistentCards[5]],
            ReferenceEqualityComparer.Instance);
        List<PersistentDeckRemovalResult> removalEvents = [];
        bool upgradedOrderingPassed = true;
        decimal blockBeforeUpgrade = target.Block;
        void OnPersistentCardRemoved(PersistentDeckRemovalResult result)
        {
            if (!expectedPersistentRemovals.Contains(result.PersistentCard))
            {
                return;
            }

            removalEvents.Add(result);
            upgradedOrderingPassed &= player.Creature.GetPower<MonsterDivinityPower>() is null &&
                                      target.Block == blockBeforeUpgrade;
        }

        int removalHistoryBefore = GetRemovalHistoryCount(player);
        PersistentDeckMutation.PersistentCardRemoved += OnPersistentCardRemoved;
        MementoMoriCard upgradedCard;
        try
        {
            upgradedCard = await CreateAndAutoPlayAsync<MementoMoriCard>(
                combatState,
                player,
                choiceContext,
                target,
                upgraded: true);
        }
        finally
        {
            PersistentDeckMutation.PersistentCardRemoved -= OnPersistentCardRemoved;
        }

        Require(removalEvents.Count == 5 &&
                removalEvents.All(result => result.Success && result.RemovedCombatCopies.Count == 1) &&
                removalEvents.Select(result => result.PersistentCard).ToHashSet(ReferenceEqualityComparer.Instance)
                    .SetEquals(expectedPersistentRemovals),
            "upgraded Memento Mori did not purge five exact persistent cards and linked copies");
        Require(expectedPersistentRemovals.All(card => card.HasBeenRemovedFromState && card.Pile is null),
            "upgraded Memento Mori left a selected persistent card in the deck");
        Require(linkedCopies.Where((_, index) => index is 0 or 1 or 3 or 4 or 5)
                .All(card => card.HasBeenRemovedFromState && card.Pile is null),
            "upgraded Memento Mori left a selected linked combat card in combat");
        Require(combatOnlyCard.HasBeenRemovedFromState && combatOnlyCard.Pile is null,
            "upgraded Memento Mori did not purge its generated combat-only card");
        Require(persistentCards[2].Pile?.Type == PileType.Deck && linkedCopies[2].Pile?.Type == PileType.Draw,
            "upgraded Memento Mori removed the Eternal card");
        Require(persistentCards[6].Pile?.Type == PileType.Deck && linkedCopies[6].Pile?.Type == PileType.Draw,
            "upgraded Memento Mori backfilled past its top-seven window");
        Require(GetRemovalHistoryCount(player) - removalHistoryBefore == 5,
            "upgraded Memento Mori did not write exactly five native persistent-removal history entries");
        Require(upgradedOrderingPassed,
            "upgraded Memento Mori did not finish synchronized persistent removals before Divinity and damage");
        Require(blockBeforeUpgrade - target.Block == 39m,
            "upgraded Memento Mori did not enter Divinity before dealing 13 damage");
        Require(playerState.Energy - energyBefore == SakikoStanceCmd.DivinityEnergyGain * 2,
            "both Memento Mori plays did not grant 3 Energy on separate Divinity entries");
        Require(upgradedCard.Pile?.Type == PileType.Discard, "upgraded Memento Mori did not enter Discard");

        MonsterDivinityPower upgradedDivinity = player.Creature.GetPower<MonsterDivinityPower>()
            ?? throw new InvalidOperationException("Upgraded Memento Mori did not enter Divinity.");
        await upgradedDivinity.AfterSideTurnEnd(choiceContext, CombatSide.Player, [player.Creature]);
        persistentCards[2].RemoveKeyword(CardKeyword.Eternal);
        foreach (CardModel persistentCard in persistentCards.Where(card => !card.HasBeenRemovedFromState))
        {
            PersistentDeckRemovalResult cleanup = await PersistentDeckMutation.RemoveAsync(
                persistentCard,
                showPersistentPreview: false,
                skipCombatVisuals: true);
            Require(cleanup.Success, $"Memento Mori persistent cleanup failed for {persistentCard.Id}");
        }
        await CardPileCmd.RemoveFromCombat(upgradedCard, skipVisuals: true);

        if (strengthBefore != 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                player.Creature,
                strengthBefore,
                player.Creature,
                null);
        }
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);
    }

    private static async Task VerifyPersistentGenerationCommonCardsAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        int strengthBefore = player.Creature.GetPower<StrengthPower>()?.Amount ?? 0;
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());

        await VerifyBudgetBentoAsync(combatState, player, choiceContext);
        await VerifyPhantomOfSakikoAsync(combatState, player, target, choiceContext);
        await VerifyPhantomOfTakiAsync(combatState, player, target, choiceContext);
        await VerifyPhantomOfTomoriAsync(combatState, player, choiceContext);
        await VerifySymbolIIAirAsync(combatState, player, target, choiceContext);

        if (strengthBefore != 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                player.Creature,
                strengthBefore,
                player.Creature,
                null);
        }
    }

    private static async Task VerifyBudgetBentoAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        decimal regenBefore = player.Creature.GetPower<RegenPower>()?.Amount ?? 0m;
        var basePlay = await CreateAndAutoPlayWithPersistentChildAsync<BudgetBentoCard, TirednessCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        var upgradedPlay = await CreateAndAutoPlayWithPersistentChildAsync<BudgetBentoCard, TirednessCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);

        Require((player.Creature.GetPower<RegenPower>()?.Amount ?? 0m) - regenBefore == 9m,
            "Budget Bento did not apply base plus upgraded Regen");
        Require(basePlay.SourceCard.Pile?.Type == PileType.Exhaust &&
                upgradedPlay.SourceCard.Pile?.Type == PileType.Exhaust,
            "Budget Bento cards did not Exhaust");
        Require(basePlay.PersistentCard.CurrentUpgradeLevel == 0 &&
                basePlay.CombatCard.CurrentUpgradeLevel == 0 &&
                upgradedPlay.PersistentCard.CurrentUpgradeLevel == 0 &&
                upgradedPlay.CombatCard.CurrentUpgradeLevel == 0,
            "Budget Bento incorrectly propagated its upgrade to Tiredness");

        await CleanupPersistentChildAsync(basePlay.PersistentCard, basePlay.CombatCard);
        await CleanupPersistentChildAsync(upgradedPlay.PersistentCard, upgradedPlay.CombatCard);
        await CardPileCmd.RemoveFromCombat(
            [basePlay.SourceCard, upgradedPlay.SourceCard],
            skipVisuals: true);
        await RestorePowerAmountAsync(
            choiceContext,
            player.Creature.GetPower<RegenPower>(),
            regenBefore,
            player.Creature);
    }

    private static async Task VerifyPhantomOfSakikoAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        decimal blockBefore = target.Block;
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal preparedBlock = target.Block;
        var basePlay = await CreateAndAutoPlayWithPersistentChildAsync<PhantomOfSakikoCard, RadianceCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        var upgradedPlay = await CreateAndAutoPlayWithPersistentChildAsync<PhantomOfSakikoCard, RadianceCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: true);

        Require(preparedBlock - target.Block == 24m,
            "Phantom of Sakiko did not deal two sets of three 4-damage hits");
        Require(basePlay.SourceCard.Pile?.Type == PileType.Exhaust &&
                upgradedPlay.SourceCard.Pile?.Type == PileType.Exhaust,
            "Phantom of Sakiko cards did not Exhaust");
        Require(basePlay.PersistentCard.CurrentUpgradeLevel == 0 &&
                basePlay.CombatCard.CurrentUpgradeLevel == 0 &&
                upgradedPlay.PersistentCard.CurrentUpgradeLevel == 1 &&
                upgradedPlay.CombatCard.CurrentUpgradeLevel == 1,
            "Phantom of Sakiko did not propagate its upgrade to Radiance");

        await CleanupPersistentChildAsync(basePlay.PersistentCard, basePlay.CombatCard);
        await CleanupPersistentChildAsync(upgradedPlay.PersistentCard, upgradedPlay.CombatCard);
        await CardPileCmd.RemoveFromCombat(
            [basePlay.SourceCard, upgradedPlay.SourceCard],
            skipVisuals: true);
        await RemoveAddedBlockAsync(choiceContext, target, blockBefore);
    }

    private static async Task VerifyPhantomOfTakiAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        decimal blockBefore = target.Block;
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal preparedBlock = target.Block;
        var basePlay = await CreateAndAutoPlayWithPersistentChildAsync<PhantomOfTakiCard, IdealCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        var upgradedPlay = await CreateAndAutoPlayWithPersistentChildAsync<PhantomOfTakiCard, IdealCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: true);

        Require(preparedBlock - target.Block == 24m,
            "Phantom of Taki did not deal 12 damage twice");
        Require(basePlay.SourceCard.Pile?.Type == PileType.Exhaust &&
                upgradedPlay.SourceCard.Pile?.Type == PileType.Exhaust,
            "Phantom of Taki cards did not Exhaust");
        Require(basePlay.PersistentCard.CurrentUpgradeLevel == 0 &&
                basePlay.CombatCard.CurrentUpgradeLevel == 0 &&
                upgradedPlay.PersistentCard.CurrentUpgradeLevel == 1 &&
                upgradedPlay.CombatCard.CurrentUpgradeLevel == 1,
            "Phantom of Taki did not propagate its upgrade to Ideal");

        await CleanupPersistentChildAsync(basePlay.PersistentCard, basePlay.CombatCard);
        await CleanupPersistentChildAsync(upgradedPlay.PersistentCard, upgradedPlay.CombatCard);
        await CardPileCmd.RemoveFromCombat(
            [basePlay.SourceCard, upgradedPlay.SourceCard],
            skipVisuals: true);
        await RemoveAddedBlockAsync(choiceContext, target, blockBefore);
    }

    private static async Task VerifyPhantomOfTomoriAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        Creature[] opponents = combatState.GetOpponentsOf(player.Creature)
            .Where(creature => creature.IsHittable)
            .ToArray();
        Dictionary<Creature, int> blockBefore = opponents.ToDictionary(creature => creature, creature => creature.Block);
        foreach (Creature opponent in opponents)
        {
            await CreatureCmd.GainBlock(opponent, 100m, ValueProp.Unpowered, null, fast: true);
        }
        decimal preparedBlock = opponents.Sum(opponent => opponent.Block);

        var basePlay = await CreateAndAutoPlayWithPersistentChildAsync<PhantomOfTomoriCard, VoiceCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        var upgradedPlay = await CreateAndAutoPlayWithPersistentChildAsync<PhantomOfTomoriCard, VoiceCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);

        Require(preparedBlock - opponents.Sum(opponent => opponent.Block) == 28m,
            "Phantom of Tomori did not deal 14 random-enemy damage twice");
        Require(basePlay.SourceCard.Pile?.Type == PileType.Exhaust &&
                upgradedPlay.SourceCard.Pile?.Type == PileType.Exhaust,
            "Phantom of Tomori cards did not Exhaust");
        Require(basePlay.PersistentCard.CurrentUpgradeLevel == 0 &&
                basePlay.CombatCard.CurrentUpgradeLevel == 0 &&
                upgradedPlay.PersistentCard.CurrentUpgradeLevel == 1 &&
                upgradedPlay.CombatCard.CurrentUpgradeLevel == 1,
            "Phantom of Tomori did not propagate its upgrade to Voice");

        await CleanupPersistentChildAsync(basePlay.PersistentCard, basePlay.CombatCard);
        await CleanupPersistentChildAsync(upgradedPlay.PersistentCard, upgradedPlay.CombatCard);
        await CardPileCmd.RemoveFromCombat(
            [basePlay.SourceCard, upgradedPlay.SourceCard],
            skipVisuals: true);
        foreach (Creature opponent in opponents)
        {
            await RemoveAddedBlockAsync(choiceContext, opponent, blockBefore[opponent]);
        }
    }

    private static async Task VerifySymbolIIAirAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        CardModel[] originalHand = PileType.Hand.GetPile(player).Cards.ToArray();
        foreach (CardModel card in originalHand)
        {
            CardPileAddResult move = await CardPileCmd.Add(
                card,
                PileType.Discard,
                CardPilePosition.Bottom,
                skipVisuals: false);
            Require(move.success && card.Pile?.Type == PileType.Discard,
                $"Symbol II: Air setup could not clear {card.Id} from Hand");
        }

        CardModel[] drawProbes = Enumerable.Range(0, 7)
            .Select(_ => combatState.CreateCard<DefendTogawaSakiko>(player))
            .ToArray();
        await AddDrawWindowAsync(drawProbes);

        decimal blockBefore = target.Block;
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal preparedBlock = target.Block;
        var basePlay = await CreateAndAutoPlayWithPersistentChildAsync<SymbolIIAirCard, AmorisCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        var upgradedPlay = await CreateAndAutoPlayWithPersistentChildAsync<SymbolIIAirCard, AmorisCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: true);

        Require(preparedBlock - target.Block == 28m,
            "Symbol II: Air did not deal 14 damage twice");
        Require(drawProbes.All(card => card.Pile?.Type == PileType.Hand),
            "Symbol II: Air did not draw its exact base plus upgraded seven-card window");
        Require(basePlay.SourceCard.Pile?.Type == PileType.Discard &&
                upgradedPlay.SourceCard.Pile?.Type == PileType.Discard,
            "Symbol II: Air cards did not enter Discard");
        Require(basePlay.PersistentCard.CurrentUpgradeLevel == 0 &&
                basePlay.CombatCard.CurrentUpgradeLevel == 0 &&
                upgradedPlay.PersistentCard.CurrentUpgradeLevel == 0 &&
                upgradedPlay.CombatCard.CurrentUpgradeLevel == 0,
            "Symbol II: Air incorrectly upgraded Amoris");

        await CleanupPersistentChildAsync(basePlay.PersistentCard, basePlay.CombatCard);
        await CleanupPersistentChildAsync(upgradedPlay.PersistentCard, upgradedPlay.CombatCard);
        await CardPileCmd.RemoveFromCombat(
            drawProbes.Concat<CardModel>([basePlay.SourceCard, upgradedPlay.SourceCard]),
            skipVisuals: false);
        foreach (CardModel card in originalHand)
        {
            CardPileAddResult restore = await CardPileCmd.Add(
                card,
                PileType.Hand,
                CardPilePosition.Bottom,
                skipVisuals: false);
            Require(restore.success && card.Pile?.Type == PileType.Hand,
                $"Symbol II: Air cleanup could not restore {card.Id} to Hand");
        }
        await RemoveAddedBlockAsync(choiceContext, target, blockBefore);
    }

    private static async Task VerifyStrengthTradeAndDeckBlockCardsAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        await VerifyDarkHeavenAsync(combatState, player, target, choiceContext);
        await VerifyGeorgetteMeGeorgetteYouAsync(combatState, player, target, choiceContext);
        await VerifyHeartsBarrierAsync(combatState, player, choiceContext);
    }

    private static async Task VerifyDarkHeavenAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        int playerStrengthBefore = player.Creature.GetPower<StrengthPower>()?.Amount ?? 0;
        int targetStrengthBefore = target.GetPower<StrengthPower>()?.Amount ?? 0;
        decimal blockBefore = target.Block;
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        await PowerCmd.Remove(target.GetPower<StrengthPower>());
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal preparedBlock = target.Block;
        await PowerCmd.Apply<StrengthPower>(choiceContext, target, 3m, target, null);

        PowerChangeLedger ledger = PowerChangeLedgerService.GetLedger(combatState);
        int eventsBefore = ledger.Snapshot(combatState.RoundNumber).Length;
        DarkHeavenCard baseCard = await CreateAndAutoPlayAsync<DarkHeavenCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        Require(target.GetPower<StrengthPower>()?.Amount == 2,
            "base Dark Heaven did not reduce positive enemy Strength by one");
        Require(player.Creature.GetPower<StrengthPower>()?.Amount == 1,
            "base Dark Heaven did not grant one advertised Strength");
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        await PowerCmd.ModifyAmount(
            choiceContext,
            target.GetPower<StrengthPower>()!,
            -1m,
            target,
            null);
        DarkHeavenCard upgradedCard = await CreateAndAutoPlayAsync<DarkHeavenCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: true);
        Require(target.GetPower<StrengthPower>() is null,
            "upgraded Dark Heaven did not fully remove one remaining enemy Strength");
        Require(player.Creature.GetPower<StrengthPower>()?.Amount == 2,
            "upgraded Dark Heaven did not grant two Strength after removing only one");

        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        DarkHeavenCard noStrengthCard = await CreateAndAutoPlayAsync<DarkHeavenCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        Require(player.Creature.GetPower<StrengthPower>() is null,
            "Dark Heaven granted Strength when the enemy had no positive Strength");
        Require(preparedBlock - target.Block == 27m,
            "Dark Heaven did not deal unchanged 9 damage across base and upgraded plays");
        Require(baseCard.Pile?.Type == PileType.Discard &&
                upgradedCard.Pile?.Type == PileType.Discard &&
                noStrengthCard.Pile?.Type == PileType.Discard,
            "Dark Heaven cards did not enter Discard");

        PowerChangeEvent[] cardEvents = ledger.Snapshot(combatState.RoundNumber)
            .Skip(eventsBefore)
            .Where(powerEvent => powerEvent.PowerModelId == ModelDb.GetId<StrengthPower>() &&
                                 powerEvent.CardSource?.ModelId == ModelDb.GetId<DarkHeavenCard>())
            .ToArray();
        CreatureIdentity playerIdentity = CreatureIdentity.FromCreature(player.Creature);
        CreatureIdentity targetIdentity = CreatureIdentity.FromCreature(target);
        Require(cardEvents.Where(powerEvent => powerEvent.Target == targetIdentity)
                .Select(powerEvent => powerEvent.Delta)
                .SequenceEqual([-1m, -1m]),
            "Dark Heaven ledger did not record actual enemy Strength reductions");
        Require(cardEvents.Where(powerEvent => powerEvent.Target == playerIdentity)
                .Select(powerEvent => powerEvent.Delta)
                .SequenceEqual([1m, 2m]),
            "Dark Heaven ledger did not retain its card source on Strength gains");

        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
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
        await CardPileCmd.RemoveFromCombat(
            [baseCard, upgradedCard, noStrengthCard],
            skipVisuals: true);
        await RemoveAddedBlockAsync(choiceContext, target, blockBefore);
    }

    private static async Task VerifyGeorgetteMeGeorgetteYouAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        int playerStrengthBefore = player.Creature.GetPower<StrengthPower>()?.Amount ?? 0;
        int targetStrengthBefore = target.GetPower<StrengthPower>()?.Amount ?? 0;
        int targetHypeBefore = target.GetPower<HypePower>()?.Amount ?? 0;
        decimal blockBefore = target.Block;
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        await PowerCmd.Remove(target.GetPower<StrengthPower>());
        await PowerCmd.Remove(target.GetPower<HypePower>());
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal preparedBlock = target.Block;

        PowerChangeLedger ledger = PowerChangeLedgerService.GetLedger(combatState);
        int eventsBefore = ledger.Snapshot(combatState.RoundNumber).Length;
        GeorgetteMeGeorgetteYouCard baseCard =
            await CreateAndAutoPlayAsync<GeorgetteMeGeorgetteYouCard>(
                combatState,
                player,
                choiceContext,
                target,
                upgraded: false);
        GeorgetteMeGeorgetteYouCard upgradedCard =
            await CreateAndAutoPlayAsync<GeorgetteMeGeorgetteYouCard>(
                combatState,
                player,
                choiceContext,
                target,
                upgraded: true);

        Require(preparedBlock - target.Block == 27m,
            "Georgette Me, Georgette You did not deal three base and three upgraded hits");
        Require(target.GetPower<StrengthPower>()?.Amount == 2,
            "Georgette Me, Georgette You did not grant one enemy Strength per play");
        Require(target.GetPower<HypePower>()?.Amount == 1,
            "upgraded Georgette Me, Georgette You did not grant one enemy Hype");
        Require(baseCard.Pile?.Type == PileType.Discard && upgradedCard.Pile?.Type == PileType.Discard,
            "Georgette Me, Georgette You cards did not enter Discard");

        PowerChangeEvent[] cardEvents = ledger.Snapshot(combatState.RoundNumber)
            .Skip(eventsBefore)
            .Where(powerEvent => powerEvent.CardSource?.ModelId == ModelDb.GetId<GeorgetteMeGeorgetteYouCard>())
            .ToArray();
        Require(cardEvents.Count(powerEvent =>
                powerEvent.PowerModelId == ModelDb.GetId<StrengthPower>() && powerEvent.Delta == 1m) == 2,
            "Georgette Me, Georgette You Strength events lost their actual delta or card source");
        Require(cardEvents.Count(powerEvent =>
                powerEvent.PowerModelId == ModelDb.GetId<HypePower>() && powerEvent.Delta == 1m) == 1,
            "Georgette Me, Georgette You Hype event lost its actual delta or card source");

        await PowerCmd.Remove(target.GetPower<HypePower>());
        await PowerCmd.Remove(target.GetPower<StrengthPower>());
        if (targetHypeBefore != 0)
        {
            await PowerCmd.Apply<HypePower>(choiceContext, target, targetHypeBefore, target, null);
        }
        if (targetStrengthBefore != 0)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, target, targetStrengthBefore, target, null);
        }
        if (playerStrengthBefore != 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                player.Creature,
                playerStrengthBefore,
                player.Creature,
                null);
        }
        await CardPileCmd.RemoveFromCombat([baseCard, upgradedCard], skipVisuals: true);
        await RemoveAddedBlockAsync(choiceContext, target, blockBefore);
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
        Require(cleanup.Success && cleanup.RemovedCombatCopies.Count == 0,
            "Heart's Barrier persistent deck probe cleanup failed");
        await CardPileCmd.RemoveFromCombat([baseCard, upgradedCard], skipVisuals: true);
        await RemoveAddedBlockAsync(choiceContext, player.Creature, blockBefore);
    }

    private static async Task VerifySelectionAndRetrievalCommonCardsAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        int strengthBefore = player.Creature.GetPower<StrengthPower>()?.Amount ?? 0;
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());

        await VerifyDatenAsync(combatState, player, target, choiceContext);
        await VerifyKillKiSSAsync(combatState, player, target, choiceContext);
        await VerifySymbolIVEarthAsync(combatState, player, target, choiceContext);

        if (strengthBefore != 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                player.Creature,
                strengthBefore,
                player.Creature,
                null);
        }
    }

    private static async Task VerifyDatenAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        CombatCardLocation[] cardsMovedAside =
        [
            .. PileType.Hand.GetPile(player).Cards.Select(card => new CombatCardLocation(card, PileType.Hand)),
            .. PileType.Discard.GetPile(player).Cards.Select(card => new CombatCardLocation(card, PileType.Discard)),
            .. PileType.Draw.GetPile(player).Cards.Select(card => new CombatCardLocation(card, PileType.Draw))
        ];
        foreach (CombatCardLocation location in cardsMovedAside)
        {
            CardPileAddResult move = await CardPileCmd.Add(
                location.Card,
                PileType.Exhaust,
                CardPilePosition.Bottom,
                skipVisuals: false);
            Require(move.success && location.Card.Pile?.Type == PileType.Exhaust,
                $"Daten setup could not move {location.Card.Id} out of its selection pool");
        }

        decimal blockBefore = target.Block;
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal preparedBlock = target.Block;
        List<DatenCard> sourceCards = [];

        foreach (bool upgraded in new[] { false, true })
        {
            CardModel[] persistentCards = new CardModel[DatenCard.CandidateCount];
            for (int index = 0; index < persistentCards.Length; index++)
            {
                persistentCards[index] = await AddPersistentProbeAsync<DefendTogawaSakiko>(player);
            }

            CardModel[] combatCopies = persistentCards
                .Select(persistentCard =>
                {
                    CardModel copy = combatState.CloneCard(persistentCard);
                    copy.DeckVersion = persistentCard;
                    return copy;
                })
                .ToArray();
            PileType[] candidatePiles = [PileType.Hand, PileType.Draw, PileType.Discard];
            for (int index = 0; index < combatCopies.Length; index++)
            {
                CardPileAddResult add = await CardPileCmd.Add(
                    combatCopies[index],
                    candidatePiles[index],
                    CardPilePosition.Bottom,
                    skipVisuals: false);
                Require(add.success && combatCopies[index].Pile?.Type == candidatePiles[index],
                    $"Daten setup could not add candidate {index} to {candidatePiles[index]}");
            }

            List<PersistentDeckRemovalResult> removalEvents = [];
            decimal targetBlockBeforePlay = target.Block;
            bool removalBeforeDamage = true;
            void OnPersistentCardRemoved(PersistentDeckRemovalResult result)
            {
                if (!persistentCards.Contains(result.PersistentCard, ReferenceEqualityComparer.Instance))
                {
                    return;
                }

                removalEvents.Add(result);
                removalBeforeDamage &= target.Block == targetBlockBeforePlay;
            }

            int removalHistoryBefore = GetRemovalHistoryCount(player);
            TestCardSelector selector = new();
            selector.PrepareToSelect([0]);
            DatenCard sourceCard;
            PersistentDeckMutation.PersistentCardRemoved += OnPersistentCardRemoved;
            try
            {
                using (CardSelectCmd.PushSelector(selector))
                {
                    sourceCard = await CreateAndAutoPlayAsync<DatenCard>(
                        combatState,
                        player,
                        choiceContext,
                        target,
                        upgraded);
                }
            }
            finally
            {
                PersistentDeckMutation.PersistentCardRemoved -= OnPersistentCardRemoved;
            }
            sourceCards.Add(sourceCard);

            Require(removalEvents.Count == 1, "Daten did not remove exactly one selected persistent card");
            PersistentDeckRemovalResult removal = removalEvents.Single();
            int removedIndex = Array.FindIndex(
                persistentCards,
                persistentCard => ReferenceEquals(persistentCard, removal.PersistentCard));
            Require(removedIndex >= 0 && removal.Success,
                "Daten did not report a successful removal for one of its three candidates");
            Require(removal.RemovedCombatCopies.Count == 1 &&
                    ReferenceEquals(removal.RemovedCombatCopies.Single(), combatCopies[removedIndex]),
                "Daten did not synchronize the exact selected DeckVersion combat copy");
            Require(removalBeforeDamage, "Daten dealt damage before completing its selected-card purge");
            Require(GetRemovalHistoryCount(player) - removalHistoryBefore == 1,
                "Daten did not write exactly one native persistent-removal history entry");
            Require(sourceCard.Pile?.Type == PileType.Exhaust, "Daten did not Exhaust after play");
            Require(persistentCards.Where((_, index) => index != removedIndex)
                    .All(card => card.Pile?.Type == PileType.Deck) &&
                    combatCopies.Where((_, index) => index != removedIndex)
                        .All(card => card.Pile is not null && !card.HasBeenRemovedFromState),
                "Daten removed an unselected persistent card or combat copy");

            for (int index = 0; index < persistentCards.Length; index++)
            {
                if (index == removedIndex)
                {
                    continue;
                }

                PersistentDeckRemovalResult cleanup = await PersistentDeckMutation.RemoveAsync(
                    persistentCards[index],
                    showPersistentPreview: false,
                    skipCombatVisuals: false);
                Require(cleanup.Success && cleanup.RemovedCombatCopies.Count == 1 &&
                        ReferenceEquals(cleanup.RemovedCombatCopies.Single(), combatCopies[index]),
                    $"Daten cleanup did not remove surviving candidate {index}");
            }
        }

        Require(preparedBlock - target.Block == 30m, "Daten did not deal base plus upgraded 30 damage");
        await CardPileCmd.RemoveFromCombat(sourceCards, skipVisuals: true);
        foreach (CombatCardLocation location in cardsMovedAside)
        {
            CardPileAddResult restore = await CardPileCmd.Add(
                location.Card,
                location.PileType,
                CardPilePosition.Bottom,
                skipVisuals: false);
            Require(restore.success && location.Card.Pile?.Type == location.PileType,
                $"Daten cleanup could not restore {location.Card.Id} to {location.PileType}");
        }
        await RemoveAddedBlockAsync(choiceContext, target, blockBefore);
    }

    private static async Task VerifyKillKiSSAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        CombatCardLocation[] cardsMovedAside = PileType.Hand.GetPile(player).Cards
            .Select(card => new CombatCardLocation(card, PileType.Hand))
            .Concat(PileType.Draw.GetPile(player).Cards
                .Where(card => card is DesireCard)
                .Select(card => new CombatCardLocation(card, PileType.Draw)))
            .Concat(PileType.Discard.GetPile(player).Cards
                .Where(card => card is DesireCard)
                .Select(card => new CombatCardLocation(card, PileType.Discard)))
            .ToArray();
        foreach (CombatCardLocation location in cardsMovedAside)
        {
            CardPileAddResult move = await CardPileCmd.Add(
                location.Card,
                PileType.Exhaust,
                CardPilePosition.Bottom,
                skipVisuals: false);
            Require(move.success && location.Card.Pile?.Type == PileType.Exhaust,
                $"Kill KiSS setup could not move {location.Card.Id} aside");
        }

        DesireCard drawDesire = combatState.CreateCard<DesireCard>(player);
        DesireCard discardDesireOne = combatState.CreateCard<DesireCard>(player);
        DesireCard discardDesireTwo = combatState.CreateCard<DesireCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(drawDesire, PileType.Draw, player);
        await CardPileCmd.AddGeneratedCardToCombat(discardDesireOne, PileType.Discard, player);
        await CardPileCmd.AddGeneratedCardToCombat(discardDesireTwo, PileType.Discard, player);

        int deckSizeBefore = player.Deck.Cards.Count;
        int gainHistoryBefore = GetGainHistoryCount(player);
        decimal blockBefore = target.Block;
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal preparedBlock = target.Block;

        KillKiSSCard baseCard = await CreateAndAutoPlayAsync<KillKiSSCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        Require(drawDesire.Pile?.Type == PileType.Hand &&
                discardDesireOne.Pile?.Type == PileType.Discard &&
                discardDesireTwo.Pile?.Type == PileType.Discard,
            "base Kill KiSS did not prioritize its Draw-pile Desire");

        KillKiSSCard upgradedCard = await CreateAndAutoPlayAsync<KillKiSSCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: true);
        CardModel[] desires = [drawDesire, discardDesireOne, discardDesireTwo];
        Require(desires.All(card => card.Pile?.Type == PileType.Hand),
            "upgraded Kill KiSS did not retrieve two Desires from Discard");
        Require(preparedBlock - target.Block == 19m, "Kill KiSS did not deal base plus upgraded 19 damage");
        Require(baseCard.Pile?.Type == PileType.Discard && upgradedCard.Pile?.Type == PileType.Discard,
            "Kill KiSS cards did not enter Discard");
        Require(player.Deck.Cards.Count == deckSizeBefore && GetGainHistoryCount(player) == gainHistoryBefore,
            "Kill KiSS generated a new Desire or changed the persistent deck");

        await CardPileCmd.RemoveFromCombat(desires, skipVisuals: false);
        await CardPileCmd.RemoveFromCombat([baseCard, upgradedCard], skipVisuals: true);
        foreach (CombatCardLocation location in cardsMovedAside)
        {
            CardPileAddResult restore = await CardPileCmd.Add(
                location.Card,
                location.PileType,
                CardPilePosition.Bottom,
                skipVisuals: false);
            Require(restore.success && location.Card.Pile?.Type == location.PileType,
                $"Kill KiSS cleanup could not restore {location.Card.Id} to {location.PileType}");
        }
        await RemoveAddedBlockAsync(choiceContext, target, blockBefore);
    }

    private static async Task VerifySymbolIVEarthAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        decimal playerBlockBefore = player.Creature.Block;
        decimal targetBlockBefore = target.Block;
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal preparedTargetBlock = target.Block;

        var basePlay = await CreateAndAutoPlayWithPersistentChildAsync<SymbolIVEarthCard, MortisCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        decimal playerBlockAfterBase = player.Creature.Block;
        var upgradedPlay = await CreateAndAutoPlayWithPersistentChildAsync<SymbolIVEarthCard, MortisCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: true);

        decimal expectedDamage = playerBlockBefore + 15m + playerBlockAfterBase + 20m;
        Require(preparedTargetBlock - target.Block == expectedDamage,
            "Symbol IV: Earth did not snapshot current plus incoming Block before each attack");
        Require(player.Creature.Block - playerBlockBefore == 35m,
            "Symbol IV: Earth did not gain base plus upgraded 35 Block");
        Require(basePlay.SourceCard.Pile?.Type == PileType.Discard &&
                upgradedPlay.SourceCard.Pile?.Type == PileType.Discard,
            "Symbol IV: Earth cards did not enter Discard");
        Require(basePlay.PersistentCard.CurrentUpgradeLevel == 0 &&
                basePlay.CombatCard.CurrentUpgradeLevel == 0 &&
                upgradedPlay.PersistentCard.CurrentUpgradeLevel == 0 &&
                upgradedPlay.CombatCard.CurrentUpgradeLevel == 0,
            "Symbol IV: Earth incorrectly upgraded Mortis");

        await CleanupPersistentChildAsync(basePlay.PersistentCard, basePlay.CombatCard);
        await CleanupPersistentChildAsync(upgradedPlay.PersistentCard, upgradedPlay.CombatCard);
        await CardPileCmd.RemoveFromCombat(
            [basePlay.SourceCard, upgradedPlay.SourceCard],
            skipVisuals: true);
        await RemoveAddedBlockAsync(choiceContext, player.Creature, playerBlockBefore);
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);
    }

    private static async Task VerifyQuaerereLuminaAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        CombatCardLocation[] cardsMovedAside = PileType.Draw.GetPile(player).Cards
            .Select(card => new CombatCardLocation(card, PileType.Draw))
            .ToArray();
        foreach (CombatCardLocation location in cardsMovedAside)
        {
            CardPileAddResult move = await CardPileCmd.Add(
                location.Card,
                PileType.Exhaust,
                CardPilePosition.Bottom,
                skipVisuals: false);
            Require(move.success && location.Card.Pile?.Type == PileType.Exhaust,
                $"Quaerere Lumina setup could not move {location.Card.Id} aside");
        }

        CardModel[] baseWindow = Enumerable.Range(0, 9)
            .Select(_ => (CardModel)combatState.CreateCard<DefendTogawaSakiko>(player))
            .ToArray();
        await AddDrawWindowAsync(baseWindow);
        decimal blockBefore = player.Creature.Block;
        TestCardSelector baseSelector = new();
        baseSelector.PrepareToSelect([0, 2, 6]);
        QuaerereLuminaCard baseCard;
        using (CardSelectCmd.PushSelector(baseSelector))
        {
            baseCard = await CreateAndAutoPlayAsync<QuaerereLuminaCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: false);
        }

        Require(baseWindow.Take(7).Count(card => card.Pile?.Type == PileType.Discard) == 3,
            "base Quaerere Lumina did not discard three selected cards from its top-seven window");
        Require(baseWindow.Skip(7).All(card => card.Pile?.Type == PileType.Draw),
            "base Quaerere Lumina exposed cards below its top-seven window");

        foreach (CardModel card in baseWindow)
        {
            await CardPileCmd.Add(card, PileType.Exhaust, CardPilePosition.Bottom, skipVisuals: true);
        }

        CardModel[] upgradedWindow = Enumerable.Range(0, 5)
            .Select(_ => (CardModel)combatState.CreateCard<DefendTogawaSakiko>(player))
            .ToArray();
        await AddDrawWindowAsync(upgradedWindow);
        TestCardSelector upgradedSelector = new();
        upgradedSelector.PrepareToSelect([0, 1, 3, 4]);
        QuaerereLuminaCard upgradedCard;
        using (CardSelectCmd.PushSelector(upgradedSelector))
        {
            upgradedCard = await CreateAndAutoPlayAsync<QuaerereLuminaCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: true);
        }

        Require(upgradedWindow.Count(card => card.Pile?.Type == PileType.Discard) == 4 &&
                upgradedWindow.Count(card => card.Pile?.Type == PileType.Draw) == 1,
            "upgraded Quaerere Lumina did not cap its top-nine window at the five available cards");
        Require(player.Creature.Block - blockBefore == 7m,
            "Quaerere Lumina did not gain Block equal to the seven cards actually discarded");
        Require(baseCard.Pile?.Type == PileType.Discard && upgradedCard.Pile?.Type == PileType.Discard,
            "Quaerere Lumina cards did not enter Discard");

        await CardPileCmd.RemoveFromCombat(
            baseWindow.Concat(upgradedWindow).Append(baseCard).Append(upgradedCard),
            skipVisuals: true);
        await AddDrawWindowAsync(cardsMovedAside.Select(location => location.Card).ToArray());
        await RemoveAddedBlockAsync(choiceContext, player.Creature, blockBefore);
    }

    private static async Task VerifyKingsAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Remove(player.Creature.GetPower<KingsPower>());
        int strengthBefore = player.Creature.GetPower<StrengthPower>()?.Amount ?? 0;
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        decimal targetBlockBefore = target.Block;
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal preparedBlock = target.Block;

        KingsCard baseCard = await CreateAndAutoPlayAsync<KingsCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        KingsPower power = player.Creature.GetPower<KingsPower>()
            ?? throw new InvalidOperationException("base Kings did not apply KingsPower.");
        KingsCard upgradedCard = await CreateAndAutoPlayAsync<KingsCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: true);

        decimal actualDamage = preparedBlock - target.Block;
        Require(
            actualDamage == 34m,
            $"Kings did not deal base plus upgraded 34 damage; actual={actualDamage}, " +
            $"base={baseCard.DynamicVars.Damage.BaseValue}, upgraded={upgradedCard.DynamicVars.Damage.BaseValue}");
        Require(ReferenceEquals(player.Creature.GetPower<KingsPower>(), power) && power.Amount == 1,
            "Kings applied more than one nonstacking power instance");
        Require(baseCard.Pile?.Type == PileType.Discard && upgradedCard.Pile?.Type == PileType.Discard,
            "Kings cards did not enter Discard");

        StarterRelicTogawaSakiko carrier = player.Relics.OfType<StarterRelicTogawaSakiko>().Single();
        CombatRoom room = player.RunState.CurrentRoom as CombatRoom
            ?? throw new InvalidOperationException("Kings diagnostic requires a live CombatRoom.");
        await carrier.AfterCombatEnd(room);
        Require(KingsRewardState.IsPending(player), "Kings combat-end hook did not mark its reward state pending");

        SavedProperties carrierSave = SavedProperties.From(carrier)
            ?? throw new InvalidOperationException("Kings pending carrier did not produce saved properties.");
        StarterRelicTogawaSakiko restoredCarrier =
            (StarterRelicTogawaSakiko)ModelDb.Relic<StarterRelicTogawaSakiko>().ToMutable();
        carrierSave.Fill(restoredCarrier);
        Require(restoredCarrier.KingsRewardPending,
            "Kings pending state did not survive a native SavedProperties round trip");

        CardModel[] rewardCandidates =
        [
            combatState.CreateCard<DefendTogawaSakiko>(player),
            combatState.CreateCard<StrikeTogawaSakiko>(player),
            combatState.CreateCard<DesireCard>(player)
        ];
        CardCreationOptions rerollOptions = CardCreationOptions.ForRoom(player, RoomType.Monster)
            .WithFlags(CardCreationFlags.IsFromCombat);
        CardReward firstReward = new(
            rewardCandidates,
            CardCreationSource.Encounter,
            player,
            rerollOptions);
        firstReward.Populate();
        Require(firstReward.Cards.Count() == 2,
            "Kings did not reduce the following combat card reward from three choices to two");

        CardReward repeatedReward = new(
            rewardCandidates,
            CardCreationSource.Encounter,
            player,
            rerollOptions);
        repeatedReward.Populate();
        Require(repeatedReward.Cards.Count() == 2 && KingsRewardState.IsPending(player),
            "Kings did not remain active for regenerated or rerolled reward options");

        CardReward nonCombatReward = new(
            rewardCandidates,
            CardCreationSource.Other,
            player,
            rerollOptions);
        nonCombatReward.Populate();
        Require(nonCombatReward.Cards.Count() == 3,
            "Kings modified a non-combat card reward");

        await Hook.AfterRewardTaken(player.RunState, player, repeatedReward);
        CardReward clearedReward = new(
            rewardCandidates,
            CardCreationSource.Encounter,
            player,
            rerollOptions);
        clearedReward.Populate();
        Require(clearedReward.Cards.Count() == 3 && !KingsRewardState.IsPending(player),
            "Kings did not clear after a card reward was taken");

        carrier.SetKingsRewardPending(true);
        await carrier.BeforeCombatStart();
        Require(!KingsRewardState.IsPending(player), "Kings did not reset safely before the next combat");

        foreach (CardModel candidate in rewardCandidates)
        {
            await CardPileCmd.Add(candidate, PileType.Exhaust, CardPilePosition.Bottom, skipVisuals: true);
        }
        await CardPileCmd.RemoveFromCombat(
            rewardCandidates.Append(baseCard).Append(upgradedCard),
            skipVisuals: true);
        await PowerCmd.Remove(power);
        if (strengthBefore != 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                player.Creature,
                strengthBefore,
                player.Creature,
                null);
        }
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);
    }

    private static async Task VerifyAccompliceAndCarefreeAsync(
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

        CardModel[] baseDrawCards =
        [
            combatState.CreateCard<DefendTogawaSakiko>(player),
            combatState.CreateCard<StrikeTogawaSakiko>(player)
        ];
        await AddDrawWindowAsync(baseDrawCards);
        TestCardSelector baseSelector = new();
        baseSelector.PrepareToSelect([0]);
        CarefreeCard baseCarefree;
        using (CardSelectCmd.PushSelector(baseSelector))
        {
            baseCarefree = await CreateAndAutoPlayAsync<CarefreeCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: false);
        }
        Require(baseDrawCards.All(card => card.Pile?.Type == PileType.Hand),
            "base Carefree did not draw both prepared cards");
        Require(baseDrawCards.Count(card => card.Keywords.Contains(CardKeyword.Retain)) == 1,
            "base Carefree did not grant combat-long Retain to exactly one selected card");
        await RemoveCombatCardsAsync(baseDrawCards.Append(baseCarefree));

        CardModel[] upgradedDrawCards =
        [
            combatState.CreateCard<DefendTogawaSakiko>(player),
            combatState.CreateCard<StrikeTogawaSakiko>(player),
            combatState.CreateCard<DefendTogawaSakiko>(player)
        ];
        await AddDrawWindowAsync(upgradedDrawCards);
        TestCardSelector upgradedSelector = new();
        upgradedSelector.PrepareToSelect([1]);
        CarefreeCard upgradedCarefree;
        using (CardSelectCmd.PushSelector(upgradedSelector))
        {
            upgradedCarefree = await CreateAndAutoPlayAsync<CarefreeCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: true);
        }
        Require(upgradedDrawCards.All(card => card.Pile?.Type == PileType.Hand),
            "upgraded Carefree did not draw all three prepared cards");
        Require(upgradedDrawCards.Count(card => card.Keywords.Contains(CardKeyword.Retain)) == 1,
            "upgraded Carefree did not grant combat-long Retain to exactly one selected card");
        Require(baseCarefree.Pile is null && upgradedCarefree.Pile?.Type == PileType.Discard,
            "Carefree source-card cleanup or result pile was incorrect");
        Require(player.Deck.Cards.Count == deckSizeBefore && GetGainHistoryCount(player) == gainHistoryBefore,
            "Carefree changed the persistent deck");

        await RemoveCombatCardsAsync(upgradedDrawCards.Append(upgradedCarefree));
        await RestoreCombatCardsAsync(cardsMovedAside);
    }

    private static async Task VerifyDesuWaAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        CombatCardLocation[] cardsMovedAside = await MoveCombatPilesAsideAsync(
            player,
            PileType.Hand,
            PileType.Draw,
            PileType.Discard);
        int strengthBefore = player.Creature.GetPower<StrengthPower>()?.Amount ?? 0;
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        decimal targetBlockBefore = target.Block;
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal preparedBlock = target.Block;

        CardModel drawAttack = combatState.CreateCard<StrikeTogawaSakiko>(player);
        CardModel drawSkill = combatState.CreateCard<DefendTogawaSakiko>(player);
        await CardPileCmd.AddGeneratedCardToCombat(drawAttack, PileType.Draw, player);
        await CardPileCmd.AddGeneratedCardToCombat(drawSkill, PileType.Draw, player);
        MelodyCard firstPreviousAttack = await CreateAndAutoPlayAsync<MelodyCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        DesuWaCard baseCard = await CreateAndAutoPlayAsync<DesuWaCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        Require(drawAttack.Pile?.Type == PileType.Hand && drawSkill.Pile?.Type == PileType.Draw,
            "base Desu Wa did not retrieve the matching Attack from Draw first");
        Require(firstPreviousAttack.Pile?.Type == PileType.Discard,
            "base Desu Wa incorrectly preferred a matching Discard card while Draw had one");
        await RemoveCombatCardsAsync([drawAttack, drawSkill, firstPreviousAttack, baseCard]);

        CardModel secondDrawAttack = combatState.CreateCard<StrikeTogawaSakiko>(player);
        CardModel preparedDiscardAttack = combatState.CreateCard<MelodyCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(secondDrawAttack, PileType.Draw, player);
        await CardPileCmd.AddGeneratedCardToCombat(preparedDiscardAttack, PileType.Discard, player);
        MelodyCard secondPreviousAttack = await CreateAndAutoPlayAsync<MelodyCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        DesuWaCard upgradedCard = await CreateAndAutoPlayAsync<DesuWaCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        CardModel[] discardCandidates = [preparedDiscardAttack, secondPreviousAttack];
        Require(secondDrawAttack.Pile?.Type == PileType.Hand,
            "upgraded Desu Wa did not retrieve its first matching card from Draw");
        Require(discardCandidates.Count(card => card.Pile?.Type == PileType.Hand) == 1,
            "upgraded Desu Wa did not fall back to exactly one matching Discard card");
        Require(PileType.Hand.GetPile(player).Cards
                .Where(card => ReferenceEquals(card, secondDrawAttack) || discardCandidates.Contains(card))
                .All(card => card.Type == CardType.Attack),
            "Desu Wa retrieved a card that did not match the previous Attack type");
        Require(upgradedCard.Pile?.Type == PileType.Discard,
            "upgraded Desu Wa did not enter Discard");
        Require(preparedBlock - target.Block == 12m,
            "Desu Wa setup attacks did not preserve exact command ordering");

        await RemoveCombatCardsAsync(discardCandidates.Append(secondDrawAttack).Append(upgradedCard));
        await RestoreCombatCardsAsync(cardsMovedAside);
        if (strengthBefore != 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                player.Creature,
                strengthBefore,
                player.Creature,
                null);
        }
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);
    }

    private static async Task VerifyEdgeMasqueradeAndWeaknessAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        CombatCardLocation[] cardsMovedAside = await MoveCombatPilesAsideAsync(
            player,
            PileType.Hand,
            PileType.Draw,
            PileType.Discard);
        int deckSizeBefore = player.Deck.Cards.Count;
        int gainHistoryBefore = GetGainHistoryCount(player);
        int removalHistoryBefore = GetRemovalHistoryCount(player);
        int strengthBefore = player.Creature.GetPower<StrengthPower>()?.Amount ?? 0;
        int frailBefore = player.Creature.GetPower<FrailPower>()?.Amount ?? 0;
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        await PowerCmd.Remove(player.Creature.GetPower<FrailPower>());

        CardPileAddResult basePersistentResult = await PersistentDeckMutation
            .AddCanonicalAsync<MasqueradeRhapsodyRequestCard>(player, skipVisuals: true);
        CardPileAddResult upgradedPersistentResult = await PersistentDeckMutation
            .AddCanonicalAsync<MasqueradeRhapsodyRequestCard>(player, skipVisuals: true, upgradeLevel: 1);
        Require(basePersistentResult.success && upgradedPersistentResult.success,
            "Masquerade persistent setup failed");
        MasqueradeRhapsodyRequestCard basePersistent =
            (MasqueradeRhapsodyRequestCard)basePersistentResult.cardAdded;
        MasqueradeRhapsodyRequestCard upgradedPersistent =
            (MasqueradeRhapsodyRequestCard)upgradedPersistentResult.cardAdded;
        MasqueradeRhapsodyRequestCard baseCombat =
            (MasqueradeRhapsodyRequestCard)combatState.CloneCard(basePersistent);
        MasqueradeRhapsodyRequestCard upgradedCombat =
            (MasqueradeRhapsodyRequestCard)combatState.CloneCard(upgradedPersistent);
        baseCombat.DeckVersion = basePersistent;
        upgradedCombat.DeckVersion = upgradedPersistent;
        await CardPileCmd.Add(baseCombat, PileType.Hand, skipVisuals: true);
        await CardPileCmd.Add(upgradedCombat, PileType.Hand, skipVisuals: true);

        CardModel generatedTarget = combatState.CreateCard<DefendTogawaSakiko>(player);
        await CardPileCmd.AddGeneratedCardToCombat(generatedTarget, PileType.Discard, player);
        EdgeOfBreakdownCard baseEdge = await CreateAndAutoPlayAsync<EdgeOfBreakdownCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        Require(generatedTarget.HasBeenRemovedFromState && generatedTarget.Pile is null,
            "Edge of Breakdown did not purge its generated Discard target");
        Require(basePersistent.PermanentDamageIncrease == 1 &&
                baseCombat.PermanentDamageIncrease == 1 &&
                baseCombat.DynamicVars.Damage.BaseValue == 2m,
            "base Masquerade did not persist and project +1 damage after a generated-card purge");
        Require(upgradedPersistent.PermanentDamageIncrease == 2 &&
                upgradedCombat.PermanentDamageIncrease == 2 &&
                upgradedCombat.DynamicVars.Damage.BaseValue == 3m,
            "upgraded Masquerade did not persist and project +2 damage after a generated-card purge");

        CardModel persistentTarget = await AddPersistentProbeAsync<DefendTogawaSakiko>(player);
        CardModel combatTarget = combatState.CloneCard(persistentTarget);
        combatTarget.DeckVersion = persistentTarget;
        await CardPileCmd.Add(combatTarget, PileType.Discard, skipVisuals: true);
        List<PersistentDeckRemovalResult> removalEvents = [];
        bool removalBeforeFrail = true;
        void OnPersistentCardRemoved(PersistentDeckRemovalResult result)
        {
            if (!ReferenceEquals(result.PersistentCard, persistentTarget))
            {
                return;
            }

            removalEvents.Add(result);
            removalBeforeFrail &= player.Creature.GetPower<FrailPower>()?.Amount == 2m;
        }

        EdgeOfBreakdownCard upgradedEdge;
        PersistentDeckMutation.PersistentCardRemoved += OnPersistentCardRemoved;
        try
        {
            upgradedEdge = await CreateAndAutoPlayAsync<EdgeOfBreakdownCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: true);
        }
        finally
        {
            PersistentDeckMutation.PersistentCardRemoved -= OnPersistentCardRemoved;
        }

        Require(removalEvents.Count == 1 && removalEvents.Single().Success,
            "Edge of Breakdown did not issue one successful persistent purge");
        Require(removalEvents.Single().RemovedCombatCopies.Count == 1 &&
                ReferenceEquals(removalEvents.Single().RemovedCombatCopies.Single(), combatTarget),
            "Edge of Breakdown did not remove the exact linked Discard combat copy");
        Require(removalBeforeFrail,
            "Edge of Breakdown applied its upgraded Frail before completing the selected purge");
        Require(basePersistent.PermanentDamageIncrease == 2 &&
                baseCombat.PermanentDamageIncrease == 2 &&
                baseCombat.DynamicVars.Damage.BaseValue == 3m,
            "base Masquerade did not grow exactly once per successful purge");
        Require(upgradedPersistent.PermanentDamageIncrease == 4 &&
                upgradedCombat.PermanentDamageIncrease == 4 &&
                upgradedCombat.DynamicVars.Damage.BaseValue == 5m,
            "upgraded Masquerade did not use its upgraded +2 permanent purge growth");
        Require(player.Creature.GetPower<FrailPower>()?.Amount == 3m,
            "Edge of Breakdown did not apply base two plus upgraded one Frail");
        Require(baseEdge.Pile?.Type == PileType.Exhaust && upgradedEdge.Pile?.Type == PileType.Exhaust,
            "Edge of Breakdown cards did not Exhaust");

        SavedProperties masqueradeSave = SavedProperties.From(upgradedPersistent)
            ?? throw new InvalidOperationException("Masquerade actual-game growth did not serialize.");
        Require(masqueradeSave.ints?.Single(property =>
                property.name == nameof(MasqueradeRhapsodyRequestCard.PermanentDamageIncrease)).value == 4,
            "Masquerade actual-game permanent growth was not present in native save properties");

        decimal targetBlockBefore = target.Block;
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal preparedBlock = target.Block;
        await CardCmd.AutoPlay(
            choiceContext,
            baseCombat,
            target,
            AutoPlayType.Default,
            skipCardPileVisuals: true);
        await CardCmd.AutoPlay(
            choiceContext,
            upgradedCombat,
            target,
            AutoPlayType.Default,
            skipCardPileVisuals: true);
        Require(preparedBlock - target.Block == 8m,
            "Masquerade did not deal its persisted base three plus upgraded five damage");
        Require(baseCombat.Pile?.Type == PileType.Discard && upgradedCombat.Pile?.Type == PileType.Discard,
            "Masquerade cards did not enter Discard after play");

        WeaknessCard weakness = combatState.CreateCard<WeaknessCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(weakness, PileType.Draw, player);
        await weakness.BeforeCombatStart();
        Require(weakness.Pile?.Type == PileType.Discard,
            "Weakness did not move from Draw to Discard during its combat-start hook");

        await RemoveCombatCardsAsync([baseEdge, upgradedEdge, weakness]);
        PersistentDeckRemovalResult baseCleanup = await PersistentDeckMutation.RemoveAsync(
            basePersistent,
            showPersistentPreview: false,
            skipCombatVisuals: true);
        PersistentDeckRemovalResult upgradedCleanup = await PersistentDeckMutation.RemoveAsync(
            upgradedPersistent,
            showPersistentPreview: false,
            skipCombatVisuals: true);
        Require(baseCleanup.Success && upgradedCleanup.Success,
            "Masquerade persistent cleanup failed");
        Require(player.Deck.Cards.Count == deckSizeBefore,
            "common-tail diagnostics did not restore the persistent deck size");
        Require(GetGainHistoryCount(player) - gainHistoryBefore == 3,
            "common-tail diagnostics did not record exactly three native persistent additions");
        Require(GetRemovalHistoryCount(player) - removalHistoryBefore == 3,
            "common-tail diagnostics did not record exactly three native persistent removals");

        await RestoreCombatCardsAsync(cardsMovedAside);
        await RestorePowerAmountAsync(
            choiceContext,
            player.Creature.GetPower<FrailPower>(),
            frailBefore,
            player.Creature);
        if (strengthBefore != 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                player.Creature,
                strengthBefore,
                player.Creature,
                null);
        }
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);
    }

    private static async Task VerifyUncommonDirectCardsAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        CombatCardLocation[] cardsMovedAside = await MoveCombatPilesAsideAsync(
            player,
            PileType.Hand,
            PileType.Draw,
            PileType.Discard);
        Creature[] opponents = combatState.GetOpponentsOf(player.Creature)
            .Where(creature => creature.IsHittable)
            .ToArray();
        Dictionary<Creature, int> opponentBlockBefore = opponents.ToDictionary(
            creature => creature,
            creature => creature.Block);
        foreach (Creature opponent in opponents)
        {
            await CreatureCmd.GainBlock(opponent, 600m, ValueProp.Unpowered, null, fast: true);
        }

        int deckSizeBefore = player.Deck.Cards.Count;
        int gainHistoryBefore = GetGainHistoryCount(player);
        int removalHistoryBefore = GetRemovalHistoryCount(player);
        int goldBefore = player.Gold;
        decimal playerBlockBefore = player.Creature.Block;
        decimal dazzlingBefore = player.Creature.GetPower<DazzlingPower>()?.Amount ?? 0m;
        decimal platingBefore = player.Creature.GetPower<PlatingPower>()?.Amount ?? 0m;
        decimal dexterityBefore = player.Creature.GetPower<DexterityPower>()?.Amount ?? 0m;
        decimal energyNextTurnBefore = player.Creature.GetPower<EnergyNextTurnPower>()?.Amount ?? 0m;
        decimal strengthBefore = player.Creature.GetPower<StrengthPower>()?.Amount ?? 0m;
        decimal weakBefore = player.Creature.GetPower<WeakPower>()?.Amount ?? 0m;
        Dictionary<Creature, decimal> vulnerableBefore = opponents.ToDictionary(
            creature => creature,
            creature => creature.GetPower<VulnerablePower>()?.Amount ?? 0m);
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        await PowerCmd.Remove(player.Creature.GetPower<PlatingPower>());
        await PowerCmd.Remove(player.Creature.GetPower<DexterityPower>());
        await PowerCmd.Remove(player.Creature.GetPower<EnergyNextTurnPower>());
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        await PowerCmd.Remove(player.Creature.GetPower<WeakPower>());
        foreach (Creature opponent in opponents)
        {
            await PowerCmd.Remove(opponent.GetPower<VulnerablePower>());
        }

        (ClockOutCard baseClockOut, TirednessCard baseTiredness, TirednessCard baseTirednessCombat) =
            await CreateAndAutoPlayWithPersistentChildAsync<ClockOutCard, TirednessCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: false);
        (ClockOutCard upgradedClockOut, TirednessCard upgradedTiredness, TirednessCard upgradedTirednessCombat) =
            await CreateAndAutoPlayWithPersistentChildAsync<ClockOutCard, TirednessCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: true);
        Require(player.Gold - goldBefore == 35, "Clock Out did not gain base 15 plus upgraded 20 Gold");
        Require(!baseTiredness.IsUpgraded && !upgradedTiredness.IsUpgraded,
            "Clock Out unexpectedly upgraded its generated Tiredness");
        Require(baseClockOut.Pile?.Type == PileType.Exhaust && upgradedClockOut.Pile?.Type == PileType.Exhaust,
            "Clock Out cards did not Exhaust");

        FallenFlowersCard baseFallen = await CreateAndAutoPlayAsync<FallenFlowersCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        FallenFlowersCard upgradedFallen = await CreateAndAutoPlayAsync<FallenFlowersCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        Require(player.Creature.GetPower<DazzlingPower>()?.Amount == 16m,
            "Fallen Flowers did not apply base seven plus upgraded nine Dazzling");
        Require(baseFallen.Pile?.Type == PileType.Discard && upgradedFallen.Pile?.Type == PileType.Discard,
            "played Fallen Flowers cards did not enter Discard");
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());

        decimal targetBlockBeforeHachibousei = target.Block;
        HachibouseiDanceCard baseHachibousei = await CreateAndAutoPlayAsync<HachibouseiDanceCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        HachibouseiDanceCard upgradedHachibousei = await CreateAndAutoPlayAsync<HachibouseiDanceCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: true);
        decimal hachibouseiDamage = targetBlockBeforeHachibousei - target.Block;
        Require(hachibouseiDamage == 16m,
            $"Hachibousei Dance did not deal eight damage twice; observed {hachibouseiDamage}");
        Require(player.Creature.GetPower<PlatingPower>()?.Amount == 16m,
            "Hachibousei Dance did not apply eight Plating twice");
        Require(baseHachibousei.Pile?.Type == PileType.Discard &&
                upgradedHachibousei.Pile?.Type == PileType.Discard,
            "Hachibousei Dance cards did not enter Discard");
        await PowerCmd.Remove(player.Creature.GetPower<PlatingPower>());

        decimal targetBlockBeforeMutsumi = target.Block;
        decimal blockBeforeMutsumi = player.Creature.Block;
        (PhantomOfMutsumiCard baseMutsumi, ProtectionCard baseProtection, ProtectionCard baseProtectionCombat) =
            await CreateAndAutoPlayWithPersistentChildAsync<PhantomOfMutsumiCard, ProtectionCard>(
                combatState,
                player,
                choiceContext,
                target,
                upgraded: false);
        (PhantomOfMutsumiCard upgradedMutsumi, ProtectionCard upgradedProtection, ProtectionCard upgradedProtectionCombat) =
            await CreateAndAutoPlayWithPersistentChildAsync<PhantomOfMutsumiCard, ProtectionCard>(
                combatState,
                player,
                choiceContext,
                target,
                upgraded: true);
        Require(targetBlockBeforeMutsumi - target.Block == 12m,
            "Phantom of Mutsumi did not deal six damage twice");
        Require(player.Creature.Block - blockBeforeMutsumi == 12m,
            "Phantom of Mutsumi did not gain six Block twice");
        Require(!baseProtection.IsUpgraded && upgradedProtection.IsUpgraded &&
                !baseProtectionCombat.IsUpgraded && upgradedProtectionCombat.IsUpgraded,
            "Phantom of Mutsumi did not propagate its upgrade to Protection");
        Require(baseMutsumi.Pile?.Type == PileType.Exhaust && upgradedMutsumi.Pile?.Type == PileType.Exhaust,
            "Phantom of Mutsumi cards did not Exhaust");

        Dictionary<Creature, int> beforeSoyo = opponents.ToDictionary(
            creature => creature,
            creature => creature.Block);
        (PhantomOfSoyoCard baseSoyo, KindnessCard baseKindness, KindnessCard baseKindnessCombat) =
            await CreateAndAutoPlayWithPersistentChildAsync<PhantomOfSoyoCard, KindnessCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: false);
        (PhantomOfSoyoCard upgradedSoyo, KindnessCard upgradedKindness, KindnessCard upgradedKindnessCombat) =
            await CreateAndAutoPlayWithPersistentChildAsync<PhantomOfSoyoCard, KindnessCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: true);
        Require(opponents.All(opponent => beforeSoyo[opponent] - opponent.Block == 14m),
            "Phantom of Soyo did not deal seven damage twice to every opponent");
        Require(!baseKindness.IsUpgraded && upgradedKindness.IsUpgraded &&
                !baseKindnessCombat.IsUpgraded && upgradedKindnessCombat.IsUpgraded,
            "Phantom of Soyo did not propagate its upgrade to Kindness");
        Require(baseSoyo.Pile?.Type == PileType.Exhaust && upgradedSoyo.Pile?.Type == PileType.Exhaust,
            "Phantom of Soyo cards did not Exhaust");

        await PowerCmd.Apply<DexterityPower>(choiceContext, player.Creature, 5m, player.Creature, null);
        await PowerCmd.Apply<DazzlingPower>(choiceContext, player.Creature, 7m, player.Creature, null);
        decimal blockBeforeRhinoceros = player.Creature.Block;
        RhinocerosBeetleCard baseRhinoceros = await CreateAndAutoPlayAsync<RhinocerosBeetleCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        RhinocerosBeetleCard upgradedRhinoceros = await CreateAndAutoPlayAsync<RhinocerosBeetleCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        Require(player.Creature.Block - blockBeforeRhinoceros == 17m,
            "Rhinoceros Beetle did not use base Block plus floor(Dazzling / 2) while bypassing Dexterity");
        Require(baseRhinoceros.Pile?.Type == PileType.Discard &&
                upgradedRhinoceros.Pile?.Type == PileType.Discard,
            "Rhinoceros Beetle cards did not enter Discard");
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        await PowerCmd.Remove(player.Creature.GetPower<DexterityPower>());

        int buffTypesBeforeCounting = player.Creature.Powers.Count(
            power => power.TypeForCurrentAmount == PowerType.Buff);
        CountingStarsCard baseCounting = await CreateAndAutoPlayAsync<CountingStarsCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        int expectedBaseCountingDazzling = buffTypesBeforeCounting + 1;
        Require(player.Creature.GetPower<DazzlingPower>()?.Amount == expectedBaseCountingDazzling,
            "base Counting Stars did not count buff types after applying next-turn Energy");
        int buffTypesBeforeUpgradedCounting = player.Creature.Powers.Count(
            power => power.TypeForCurrentAmount == PowerType.Buff);
        CountingStarsCard upgradedCounting = await CreateAndAutoPlayAsync<CountingStarsCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        decimal expectedCountingDazzling = expectedBaseCountingDazzling + buffTypesBeforeUpgradedCounting * 2m;
        Require(player.Creature.GetPower<DazzlingPower>()?.Amount == expectedCountingDazzling,
            "upgraded Counting Stars did not apply two Dazzling per current buff type");
        Require(player.Creature.GetPower<EnergyNextTurnPower>()?.Amount == 2m,
            "Counting Stars did not apply one next-turn Energy twice");
        Require(baseCounting.Pile?.Type == PileType.Discard && upgradedCounting.Pile?.Type == PileType.Discard,
            "Counting Stars cards did not enter Discard");

        await CleanupPersistentChildAsync(baseTiredness, baseTirednessCombat);
        await CleanupPersistentChildAsync(upgradedTiredness, upgradedTirednessCombat);
        await CleanupPersistentChildAsync(baseProtection, baseProtectionCombat);
        await CleanupPersistentChildAsync(upgradedProtection, upgradedProtectionCombat);
        await CleanupPersistentChildAsync(baseKindness, baseKindnessCombat);
        await CleanupPersistentChildAsync(upgradedKindness, upgradedKindnessCombat);
        await RemoveCombatCardsAsync(
        [
            baseClockOut,
            upgradedClockOut,
            baseFallen,
            upgradedFallen,
            baseHachibousei,
            upgradedHachibousei,
            baseMutsumi,
            upgradedMutsumi,
            baseSoyo,
            upgradedSoyo,
            baseRhinoceros,
            upgradedRhinoceros,
            baseCounting,
            upgradedCounting
        ]);
        Require(player.Deck.Cards.Count == deckSizeBefore,
            "uncommon-direct diagnostics did not restore the persistent deck size");
        Require(GetGainHistoryCount(player) - gainHistoryBefore == 6,
            "uncommon-direct diagnostics did not record six native persistent additions");
        Require(GetRemovalHistoryCount(player) - removalHistoryBefore == 6,
            "uncommon-direct diagnostics did not record six native persistent removals");

        await PlayerCmd.SetGold(goldBefore, player);
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        await PowerCmd.Remove(player.Creature.GetPower<EnergyNextTurnPower>());
        if (platingBefore != 0m)
        {
            await PowerCmd.Apply<PlatingPower>(choiceContext, player.Creature, platingBefore, player.Creature, null);
        }
        if (dexterityBefore != 0m)
        {
            await PowerCmd.Apply<DexterityPower>(choiceContext, player.Creature, dexterityBefore, player.Creature, null);
        }
        if (energyNextTurnBefore != 0m)
        {
            await PowerCmd.Apply<EnergyNextTurnPower>(
                choiceContext,
                player.Creature,
                energyNextTurnBefore,
                player.Creature,
                null);
        }
        if (strengthBefore != 0m)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, player.Creature, strengthBefore, player.Creature, null);
        }
        if (weakBefore != 0m)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, player.Creature, weakBefore, player.Creature, null);
        }
        foreach (Creature opponent in opponents)
        {
            if (vulnerableBefore[opponent] != 0m)
            {
                await PowerCmd.Apply<VulnerablePower>(
                    choiceContext,
                    opponent,
                    vulnerableBefore[opponent],
                    player.Creature,
                    null);
            }
        }
        if (dazzlingBefore != 0m)
        {
            await PowerCmd.Apply<DazzlingPower>(choiceContext, player.Creature, dazzlingBefore, player.Creature, null);
        }
        await RemoveAddedBlockAsync(choiceContext, player.Creature, playerBlockBefore);
        foreach (Creature opponent in opponents)
        {
            await RemoveAddedBlockAsync(choiceContext, opponent, opponentBlockBefore[opponent]);
        }
        await RestoreCombatCardsAsync(cardsMovedAside);
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

    private static async Task<(TSource SourceCard, TChild PersistentCard, TChild CombatCard)>
        CreateAndAutoPlayWithPersistentChildAsync<TSource, TChild>(
            CombatState combatState,
            Player player,
            PlayerChoiceContext choiceContext,
            Creature? target,
            bool upgraded)
        where TSource : CardModel
        where TChild : CardModel
    {
        int eventOrder = 0;
        List<(TChild Card, int Order)> persistentAdds = [];
        List<(TChild Card, int Order)> combatAdds = [];
        int historyBefore = GetGainHistoryCount(player);

        void OnPersistentCardAdded(CardModel card)
        {
            int order = ++eventOrder;
            if (card is TChild child)
            {
                persistentAdds.Add((child, order));
            }
        }

        void OnDiscardCardAdded(CardModel card)
        {
            int order = ++eventOrder;
            if (card is TChild child)
            {
                combatAdds.Add((child, order));
            }
        }

        player.Deck.CardAdded += OnPersistentCardAdded;
        CardPile discard = PileType.Discard.GetPile(player);
        discard.CardAdded += OnDiscardCardAdded;
        TSource sourceCard;
        try
        {
            sourceCard = await CreateAndAutoPlayAsync<TSource>(
                combatState,
                player,
                choiceContext,
                target,
                upgraded);
        }
        finally
        {
            player.Deck.CardAdded -= OnPersistentCardAdded;
            discard.CardAdded -= OnDiscardCardAdded;
        }

        Require(persistentAdds.Count == 1,
            $"{typeof(TSource).Name} did not add exactly one {typeof(TChild).Name} to the persistent deck");
        Require(combatAdds.Count == 1,
            $"{typeof(TSource).Name} did not add exactly one {typeof(TChild).Name} combat copy to Discard");
        TChild persistentCard = persistentAdds.Single().Card;
        TChild combatCard = combatAdds.Single().Card;
        Require(persistentCard.Pile?.Type == PileType.Deck && combatCard.Pile?.Type == PileType.Discard,
            $"{typeof(TSource).Name} child cards entered the wrong piles");
        Require(ReferenceEquals(combatCard.DeckVersion, persistentCard),
            $"{typeof(TSource).Name} combat child did not retain exact DeckVersion identity");
        Require(persistentAdds.Single().Order < combatAdds.Single().Order,
            $"{typeof(TSource).Name} added its combat child before its persistent deck version");
        Require(GetGainHistoryCount(player) - historyBefore == 1,
            $"{typeof(TSource).Name} did not write exactly one native persistent-gain history entry");
        return (sourceCard, persistentCard, combatCard);
    }

    private static async Task CleanupPersistentChildAsync(CardModel persistentCard, CardModel combatCard)
    {
        PersistentDeckRemovalResult removal = await PersistentDeckMutation.RemoveAsync(
            persistentCard,
            showPersistentPreview: false,
            skipCombatVisuals: true);
        Require(removal.Success &&
                removal.RemovedCombatCopies.Count == 1 &&
                ReferenceEquals(removal.RemovedCombatCopies.Single(), combatCard),
            $"persistent cleanup did not remove the exact linked combat copy for {persistentCard.Id}");
        Require(persistentCard.HasBeenRemovedFromState && persistentCard.Pile is null &&
                combatCard.HasBeenRemovedFromState && combatCard.Pile is null,
            $"persistent cleanup left {persistentCard.Id} or its linked combat copy in state");
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

    private static int GetRemovalHistoryCount(Player player)
    {
        return player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).CardsRemoved.Count
            ?? throw new InvalidOperationException("Phase N5 persistent-removal history is unavailable.");
    }

    private static int GetGainHistoryCount(Player player)
    {
        return player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).CardsGained.Count
            ?? throw new InvalidOperationException("Phase N5 persistent-gain history is unavailable.");
    }

    private static async Task<BlackAndWhiteKeysCard> CreateAndAutoPlayKeysAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext,
        Creature target,
        bool upgraded,
        int choiceIndex)
    {
        TestCardSelector selector = new();
        selector.PrepareToSelect(new[] { choiceIndex });
        using (CardSelectCmd.PushSelector(selector))
        {
            return await CreateAndAutoPlayAsync<BlackAndWhiteKeysCard>(
                combatState,
                player,
                choiceContext,
                target,
                upgraded);
        }
    }

    private static async Task RestorePowerAmountAsync(
        PlayerChoiceContext choiceContext,
        PowerModel? power,
        decimal amountBefore,
        Creature applier)
    {
        if (power is null)
        {
            Require(amountBefore == 0m, "diagnostic cleanup could not find a pre-existing power");
            return;
        }
        if (amountBefore == 0m)
        {
            await PowerCmd.Remove(power);
            return;
        }

        decimal adjustment = amountBefore - power.Amount;
        if (adjustment != 0m)
        {
            await PowerCmd.ModifyAmount(choiceContext, power, adjustment, applier, null);
        }
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
