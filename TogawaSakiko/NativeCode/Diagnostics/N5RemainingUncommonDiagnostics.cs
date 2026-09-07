using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static partial class N5BatchDiagnostics
{
    private static async Task VerifyRemainingUncommonCoreAsync(
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
        decimal playerBlockBefore = player.Creature.Block;
        decimal targetBlockBefore = target.Block;
        int playerStrengthBefore = await RemoveAndRememberPowerAsync<StrengthPower>(player.Creature);
        int playerWeakBefore = await RemoveAndRememberPowerAsync<WeakPower>(player.Creature);
        int playerArtifactBefore = await RemoveAndRememberPowerAsync<ArtifactPower>(player.Creature);
        int targetStrengthBefore = await RemoveAndRememberPowerAsync<StrengthPower>(target);
        int targetPlatingBefore = await RemoveAndRememberPowerAsync<PlatingPower>(target);
        int targetVulnerableBefore = await RemoveAndRememberPowerAsync<VulnerablePower>(target);
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        await PowerCmd.Remove(player.Creature.GetPower<CuriosityPower>());
        await PowerCmd.Remove(player.Creature.GetPower<EndurancePower>());
        await PowerCmd.Remove(player.Creature.GetPower<FearlessPower>());
        await PowerCmd.Remove(player.Creature.GetPower<GodsCreationPower>());
        await PowerCmd.Remove(target.GetPower<GodsCreationPower>());
        await CreatureCmd.GainBlock(target, 1000m, ValueProp.Unpowered, null, fast: true);

        List<CardModel> cardsToRemove = [];

        CardModel baseHandProbe = combatState.CreateCard<DefendTogawaSakiko>(player);
        CardModel baseDrawProbe = combatState.CreateCard<DefendTogawaSakiko>(player);
        await CardPileCmd.AddGeneratedCardToCombat(baseHandProbe, PileType.Hand, player);
        await CardPileCmd.AddGeneratedCardToCombat(baseDrawProbe, PileType.Draw, player);
        AleaIactaEstCard baseAlea = await CreateAndAutoPlayAsync<AleaIactaEstCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        Require(baseHandProbe.IsUpgraded && !baseDrawProbe.IsUpgraded,
            "base Alea Iacta Est did not upgrade Hand while leaving Draw unchanged");
        Require(player.Creature.GetPower<NoDrawPower>() is not null,
            "base Alea Iacta Est did not apply No Draw");
        await PowerCmd.Remove(player.Creature.GetPower<NoDrawPower>());
        cardsToRemove.AddRange([baseHandProbe, baseDrawProbe, baseAlea]);

        CardModel upgradedHandProbe = combatState.CreateCard<DefendTogawaSakiko>(player);
        CardModel upgradedDrawProbe = combatState.CreateCard<DefendTogawaSakiko>(player);
        await CardPileCmd.AddGeneratedCardToCombat(upgradedHandProbe, PileType.Hand, player);
        await CardPileCmd.AddGeneratedCardToCombat(upgradedDrawProbe, PileType.Draw, player);
        AleaIactaEstCard upgradedAlea = await CreateAndAutoPlayAsync<AleaIactaEstCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        Require(upgradedHandProbe.IsUpgraded && upgradedDrawProbe.IsUpgraded,
            "upgraded Alea Iacta Est did not upgrade both Hand and Draw");
        Require(player.Creature.GetPower<NoDrawPower>() is not null,
            "upgraded Alea Iacta Est did not apply No Draw");
        await PowerCmd.Remove(player.Creature.GetPower<NoDrawPower>());
        cardsToRemove.AddRange([upgradedHandProbe, upgradedDrawProbe, upgradedAlea]);

        decimal targetBlockBeforeBaseAngles = target.Block;
        AnglesCard baseAngles = await CreateAndAutoPlayAsync<AnglesCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        Require(targetBlockBeforeBaseAngles - target.Block == 9m,
            "base Angles did not deal nine damage before applying God's Creation");
        Require(player.Creature.GetPower<GodsCreationPower>()?.Amount == 1 &&
                target.GetPower<GodsCreationPower>()?.Amount == 1,
            "Angles did not apply one God's Creation to both creatures");
        decimal targetBlockBeforeGodsProbe = target.Block;
        await CreatureCmd.Damage(
            choiceContext,
            target,
            5m,
            ValueProp.Unpowered,
            player.Creature);
        Require(targetBlockBeforeGodsProbe - target.Block == 4m,
            "God's Creation did not reduce a positive incoming damage instance by one");
        await CreatureCmd.GainBlock(player.Creature, 5m, ValueProp.Unpowered, null, fast: true);
        decimal playerBlockBeforeGodsProbe = player.Creature.Block;
        await CreatureCmd.Damage(
            choiceContext,
            player.Creature,
            5m,
            ValueProp.Unpowered,
            target);
        Require(playerBlockBeforeGodsProbe - player.Creature.Block == 4m,
            "player God's Creation did not reduce a positive incoming damage instance by one");
        await PowerCmd.Remove(player.Creature.GetPower<GodsCreationPower>());
        await PowerCmd.Remove(target.GetPower<GodsCreationPower>());
        decimal targetBlockBeforeUpgradedAngles = target.Block;
        AnglesCard upgradedAngles = await CreateAndAutoPlayAsync<AnglesCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: true);
        Require(targetBlockBeforeUpgradedAngles - target.Block == 12m,
            "upgraded Angles did not deal twelve damage");
        await PowerCmd.Remove(player.Creature.GetPower<GodsCreationPower>());
        await PowerCmd.Remove(target.GetPower<GodsCreationPower>());
        cardsToRemove.AddRange([baseAngles, upgradedAngles]);

        ChoirSChoirCard choir = combatState.CreateCard<ChoirSChoirCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(choir, PileType.Hand, player);
        Require(choir.ShouldRetainThisTurn,
            "Choir's Choir did not use native Retain while in Hand");
        DesireCard desire = await CreateAndAutoPlayAsync<DesireCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        Require(choir.EnergyCost.GetWithModifiers(CostModifiers.Local) == 2,
            "Choir's Choir did not reduce the exact Hand copy by one after Desire");
        decimal targetBlockBeforeChoir = target.Block;
        await CardCmd.AutoPlay(
            choiceContext,
            choir,
            target,
            AutoPlayType.Default,
            skipCardPileVisuals: true);
        Require(targetBlockBeforeChoir - target.Block == 18m,
            "Choir's Choir did not deal six damage three times");
        cardsToRemove.AddRange([choir, desire]);

        CrucifixXCard crucifix = combatState.CreateCard<CrucifixXCard>(player);
        crucifix.EnergyCost.CapturedXValue = 2;
        await CardPileCmd.AddGeneratedCardToCombat(crucifix, PileType.Draw, player);
        decimal targetBlockBeforeCrucifix = target.Block;
        decimal playerBlockBeforeCrucifix = player.Creature.Block;
        await CardCmd.AutoPlay(
            choiceContext,
            crucifix,
            target,
            AutoPlayType.Default,
            skipXCapture: true,
            skipCardPileVisuals: true);
        Require(targetBlockBeforeCrucifix - target.Block == 8m &&
                player.Creature.Block - playerBlockBeforeCrucifix == 8m,
            "Crucifix X did not alternate four damage and four Block twice for captured X=2");
        cardsToRemove.Add(crucifix);

        CuriosityCard firstCuriosity = await CreateAndAutoPlayAsync<CuriosityCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        EnduranceCard enduranceCard = await CreateAndAutoPlayAsync<EnduranceCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        Require(player.Creature.GetPower<StrengthPower>()?.Amount == 1 &&
                player.Creature.GetPower<EndurancePower>()?.Amount == 5,
            "Curiosity did not grant one Strength after Endurance was played");
        CuriosityCard secondCuriosity = await CreateAndAutoPlayAsync<CuriosityCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        FearlessCard fearlessCard = await CreateAndAutoPlayAsync<FearlessCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        Require(player.Creature.GetPower<CuriosityPower>()?.Amount == 2 &&
                player.Creature.GetPower<StrengthPower>()?.Amount == 4 &&
                player.Creature.GetPower<FearlessPower>()?.Amount == 2,
            "stacked Curiosity did not capture pre-play amounts for subsequent Power cards");
        cardsToRemove.AddRange([firstCuriosity, enduranceCard, secondCuriosity, fearlessCard]);

        decimal blockBeforeEndurance = player.Creature.Block;
        PersistentDeckAndCombatAddResult enduranceAddition =
            await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<DefendTogawaSakiko>(
                player,
                combatState,
                PileType.Discard,
                skipPersistentVisuals: true);
        Require(enduranceAddition.Success && player.Creature.Block - blockBeforeEndurance == 5m,
            "Endurance did not gain Block exactly once for a synchronized persistent add");
        PersistentDeckRemovalResult enduranceRemoval = await PersistentDeckMutation.RemoveAsync(
            enduranceAddition.PersistentCard!,
            showPersistentPreview: false,
            skipCombatVisuals: true);
        Require(enduranceRemoval.Success &&
                enduranceRemoval.RemovedCombatCopies.Count == 1 &&
                ReferenceEquals(enduranceRemoval.RemovedCombatCopies.Single(), enduranceAddition.CombatCard) &&
                player.Creature.Block - blockBeforeEndurance == 10m,
            "Endurance did not gain Block exactly once for synchronized persistent removal");

        PersistentDeckAndCombatAddResult fearlessAddition =
            await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<DefendTogawaSakiko>(
                player,
                combatState,
                PileType.Hand,
                skipPersistentVisuals: true);
        Require(fearlessAddition.Success,
            "Fearless setup could not add an exact linked Hand card");
        FearlessPower fearless = player.Creature.GetPower<FearlessPower>()
            ?? throw new InvalidOperationException("Fearless power disappeared before its diagnostic.");
        TestCardSelector purgeSelector = new();
        purgeSelector.PrepareToSelect([fearlessAddition.CombatCard!]);
        using (CardSelectCmd.PushSelector(purgeSelector))
        {
            await fearless.BeforeSideTurnEnd(
                choiceContext,
                CombatSide.Player,
                [player.Creature]);
        }
        Require(fearless.Amount == 1 &&
                fearlessAddition.PersistentCard!.HasBeenRemovedFromState &&
                fearlessAddition.CombatCard!.HasBeenRemovedFromState,
            "Fearless did not purge the selected exact DeckVersion pair before decrementing");
        CardModel cancelCandidate = combatState.CreateCard<DefendTogawaSakiko>(player);
        await CardPileCmd.AddGeneratedCardToCombat(cancelCandidate, PileType.Hand, player);
        TestCardSelector cancelSelector = new();
        cancelSelector.PrepareToSelect(Array.Empty<int>());
        using (CardSelectCmd.PushSelector(cancelSelector))
        {
            await fearless.BeforeSideTurnEnd(
                choiceContext,
                CombatSide.Player,
                [player.Creature]);
        }
        Require(player.Creature.GetPower<FearlessPower>() is null &&
                cancelCandidate.Pile?.Type == PileType.Hand,
            "Fearless did not decrement after a native optional-selection cancellation");
        cardsToRemove.Add(cancelCandidate);

        await PowerCmd.Remove(player.Creature.GetPower<CuriosityPower>());
        await PowerCmd.Remove(player.Creature.GetPower<EndurancePower>());
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());

        KaoCard kao = combatState.CreateCard<KaoCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(kao, PileType.Hand, player);
        await PowerCmd.Apply<StrengthPower>(choiceContext, target, 1m, target, null);
        Require(kao.DynamicVars.Damage.BaseValue == 9m,
            "Kao did not gain four combat damage from a positive enemy Buff while in Hand");
        await CardPileCmd.Add(kao, PileType.Discard, skipVisuals: true);
        await PowerCmd.Apply<PlatingPower>(choiceContext, target, 1m, target, null);
        Require(kao.DynamicVars.Damage.BaseValue == 9m,
            "Kao grew from an enemy Buff while outside Hand");
        await CardPileCmd.Add(kao, PileType.Hand, skipVisuals: true);
        decimal targetBlockBeforeKao = target.Block;
        await CardCmd.AutoPlay(
            choiceContext,
            kao,
            target,
            AutoPlayType.Default,
            skipCardPileVisuals: true);
        Require(targetBlockBeforeKao - target.Block == 9m,
            "Kao did not deal its retained combat-grown damage");
        cardsToRemove.Add(kao);

        await PowerCmd.Remove(target.GetPower<StrengthPower>());
        await PowerCmd.Remove(target.GetPower<PlatingPower>());
        await RemoveCombatCardsAsync(cardsToRemove);
        await RemoveAddedBlockAsync(choiceContext, player.Creature, playerBlockBefore);
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);
        await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, player.Creature, playerStrengthBefore);
        await RestoreRememberedPowerAsync<WeakPower>(choiceContext, player.Creature, playerWeakBefore);
        await RestoreRememberedPowerAsync<ArtifactPower>(choiceContext, player.Creature, playerArtifactBefore);
        await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, target, targetStrengthBefore);
        await RestoreRememberedPowerAsync<PlatingPower>(choiceContext, target, targetPlatingBefore);
        await RestoreRememberedPowerAsync<VulnerablePower>(choiceContext, target, targetVulnerableBefore);
        await RestoreCombatCardsAsync(cardsMovedAside);
    }

    private static async Task VerifyRemainingUncommonHooksAsync(
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
        decimal playerBlockBefore = player.Creature.Block;
        decimal targetBlockBefore = target.Block;
        int energyBefore = player.PlayerCombatState?.Energy
            ?? throw new InvalidOperationException("remaining uncommon diagnostics require player combat state");
        int playerStrengthBefore = await RemoveAndRememberPowerAsync<StrengthPower>(player.Creature);
        int playerWeakBefore = await RemoveAndRememberPowerAsync<WeakPower>(player.Creature);
        int targetVulnerableBefore = await RemoveAndRememberPowerAsync<VulnerablePower>(target);
        int hypeBefore = await RemoveAndRememberPowerAsync<HypePower>(player.Creature);
        int platingBefore = await RemoveAndRememberPowerAsync<PlatingPower>(player.Creature);
        int dexterityBefore = await RemoveAndRememberPowerAsync<DexterityPower>(player.Creature);
        int dazzlingBefore = await RemoveAndRememberPowerAsync<DazzlingPower>(player.Creature);
        int targetExistingBlock = target.Block;
        if (targetExistingBlock > 0)
        {
            await CreatureCmd.LoseBlock(choiceContext, target, targetExistingBlock, null);
        }
        await CreatureCmd.Heal(target, target.MaxHp, playAnim: false);

        List<CardModel> cardsToRemove = [];
        OurSongCard ourSong = await CreateAndAutoPlayAsync<OurSongCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        decimal blockBeforeWish = player.Creature.Block;
        WishToBecomeHumanCard wishHuman = await CreateAndAutoPlayAsync<WishToBecomeHumanCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        int wishDazzling = player.Creature.GetPower<DazzlingPower>()?.Amount ?? 0;
        Require(wishDazzling > 0 && player.Creature.Block - blockBeforeWish == wishDazzling,
            "Our Song did not gain the next Attack's actual unblocked damage as Block");
        Require(player.Creature.GetPower<OurSongPower>() is null,
            "Our Song did not consume exactly once after the next Attack");
        Require(wishDazzling == 4,
            "Wish to Become Human did not convert its two unblocked two-damage hits into four Dazzling");
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        cardsToRemove.AddRange([ourSong, wishHuman]);

        CardPileAddResult perderePersistentResult = await PersistentDeckMutation.AddCanonicalAsync<AmorisCard>(
            player,
            skipVisuals: true);
        Require(perderePersistentResult.success,
            "Perdere Omnia setup could not add a persistent Unplayable card");
        CardModel perderePersistent = perderePersistentResult.cardAdded;
        CardModel perdereLinked = combatState.CloneCard(perderePersistent);
        perdereLinked.DeckVersion = perderePersistent;
        CardModel replacement = combatState.CreateCard<DefendTogawaSakiko>(player);
        PerdereOmniaCard perdere = combatState.CreateCard<PerdereOmniaCard>(player);
        await AddDrawWindowAsync([perdere, perdereLinked, replacement]);
        int removalHistoryBeforePerdere = GetRemovalHistoryCount(player);
        await CardPileCmd.Draw(choiceContext, 1, player);
        Require(perdere.Pile?.Type == PileType.Hand &&
                player.Creature.GetPower<PerdereOmniaPower>()?.Amount == 1,
            "drawing Perdere Omnia did not apply its one-use power");
        await CardPileCmd.Draw(choiceContext, 1, player);
        Require(perderePersistent.HasBeenRemovedFromState &&
                perdereLinked.HasBeenRemovedFromState &&
                replacement.Pile?.Type == PileType.Hand &&
                player.Creature.GetPower<PerdereOmniaPower>() is null &&
                GetRemovalHistoryCount(player) - removalHistoryBeforePerdere == 1,
            "Perdere Omnia did not purge the exact linked Unplayable draw and draw one replacement");
        cardsToRemove.AddRange([perdere, replacement]);

        PrimoDieInScaenaCard primoCard = await CreateAndAutoPlayAsync<PrimoDieInScaenaCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        PrimoDieInScaenaPower primo = player.Creature.GetPower<PrimoDieInScaenaPower>()
            ?? throw new InvalidOperationException("Primo Die in Scaena did not apply its power.");
        Require(primoCard.Keywords.Contains(CardKeyword.Innate),
            "upgraded Primo Die in Scaena did not retain native Innate metadata");
        int energyBeforePrimoHook = player.PlayerCombatState.Energy;
        await primo.AfterPlayerTurnStart(choiceContext, player);
        Require(player.PlayerCombatState.Energy - energyBeforePrimoHook == 1,
            "Primo Die in Scaena did not gain one Energy after draw at player turn start");
        await primo.AfterShuffle(choiceContext, player);
        Require(player.Creature.GetPower<PrimoDieInScaenaPower>() is null,
            "Primo Die in Scaena did not remove itself after the owner's shuffle");
        cardsToRemove.Add(primoCard);

        CardModel sharedDrawOne = combatState.CreateCard<DefendTogawaSakiko>(player);
        CardModel sharedDrawTwo = combatState.CreateCard<StrikeTogawaSakiko>(player);
        await AddDrawWindowAsync([sharedDrawOne, sharedDrawTwo]);
        SharedDestinyCard sharedCard = await CreateAndAutoPlayAsync<SharedDestinyCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        Require(player.Creature.GetPower<SharedDestinyPower>()?.Amount == 2,
            "upgraded Shared Destiny did not apply two stacks");
        await PowerCmd.Apply<PlatingPower>(choiceContext, player.Creature, 1m, player.Creature, null);
        Require(sharedDrawOne.Pile?.Type == PileType.Hand && sharedDrawTwo.Pile?.Type == PileType.Hand,
            "Shared Destiny did not draw two for a newly applied positive Buff type");
        CardModel sharedNoStackDraw = combatState.CreateCard<DefendTogawaSakiko>(player);
        await AddDrawWindowAsync([sharedNoStackDraw]);
        await PowerCmd.Apply<PlatingPower>(choiceContext, player.Creature, 1m, player.Creature, null);
        Require(sharedNoStackDraw.Pile?.Type == PileType.Draw,
            "Shared Destiny drew for stacking an existing Buff type");
        CardModel sharedNoAmbergrisDraw = combatState.CreateCard<DefendTogawaSakiko>(player);
        await AddDrawWindowAsync([sharedNoAmbergrisDraw]);
        await PowerCmd.Apply<AmbergrisPower>(choiceContext, player.Creature, 1m, player.Creature, null);
        Require(sharedNoAmbergrisDraw.Pile?.Type == PileType.Draw,
            "Shared Destiny reacted to the hidden native Ambergris marker");
        await PowerCmd.Remove(player.Creature.GetPower<SharedDestinyPower>());
        await PowerCmd.Remove(player.Creature.GetPower<PlatingPower>());
        await PowerCmd.Remove(player.Creature.GetPower<AmbergrisPower>());
        cardsToRemove.AddRange(
            [sharedCard, sharedDrawOne, sharedDrawTwo, sharedNoStackDraw, sharedNoAmbergrisDraw]);

        await CreatureCmd.GainBlock(target, 200m, ValueProp.Unpowered, null, fast: true);
        await PowerCmd.Apply<DazzlingPower>(choiceContext, player.Creature, 2m, player.Creature, null);
        decimal targetBlockBeforeGirl = target.Block;
        decimal playerBlockBeforeGirl = player.Creature.Block;
        TheGirlWithFlaxenHairCard girlCard = await CreateAndAutoPlayAsync<TheGirlWithFlaxenHairCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        Require(targetBlockBeforeGirl - target.Block == 9m &&
                player.Creature.Block - playerBlockBeforeGirl == 3m &&
                player.Creature.GetPower<GirlOfSpringPower>()?.Amount == 3,
            "The Girl with Flaxen Hair did not combine seven damage with Dazzling-triggered three Block");
        decimal targetBlockBeforeGirlBuff = target.Block;
        decimal playerBlockBeforeGirlBuff = player.Creature.Block;
        await PowerCmd.Apply<DexterityPower>(choiceContext, player.Creature, 1m, player.Creature, null);
        Require(targetBlockBeforeGirlBuff - target.Block == 2m &&
                player.Creature.Block - playerBlockBeforeGirlBuff == 3m,
            "Girl of Spring did not gain three unpowered Block after a later Dazzling trigger");
        GirlOfSpringPower girl = player.Creature.GetPower<GirlOfSpringPower>()
            ?? throw new InvalidOperationException("Girl of Spring disappeared before its expiration check.");
        await girl.AfterPlayerTurnStart(choiceContext, player);
        Require(player.Creature.GetPower<GirlOfSpringPower>() is null,
            "Girl of Spring did not expire at the next owner player-turn start");
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        await PowerCmd.Remove(player.Creature.GetPower<DexterityPower>());
        cardsToRemove.Add(girlCard);

        CardPileAddResult seizePersistentResult = await PersistentDeckMutation.AddCanonicalAsync<SeizeTheFateCard>(
            player,
            skipVisuals: true);
        Require(seizePersistentResult.success,
            "Seize the Fate setup could not add a persistent card");
        SeizeTheFateCard seizePersistent = (SeizeTheFateCard)seizePersistentResult.cardAdded;
        List<SeizeTheFateCard> seizeCombatCards = [];
        for (int playIndex = 0; playIndex < 2; playIndex++)
        {
            SeizeTheFateCard combatCopy = (SeizeTheFateCard)combatState.CloneCard(seizePersistent);
            combatCopy.DeckVersion = seizePersistent;
            await CardPileCmd.Add(combatCopy, PileType.Draw, skipVisuals: true);
            await CardCmd.AutoPlay(
                choiceContext,
                combatCopy,
                null,
                AutoPlayType.Default,
                skipCardPileVisuals: true);
            seizeCombatCards.Add(combatCopy);
        }
        Require(seizePersistent.TimesPlayed == 2 &&
                player.Creature.GetPower<HypePower>()?.Amount == 11,
            "Seize the Fate did not grant six then five Hype while persisting two plays");
        CardCmd.Upgrade(seizePersistent, CardPreviewStyle.None);
        SeizeTheFateCard frozenCopy = (SeizeTheFateCard)combatState.CloneCard(seizePersistent);
        frozenCopy.DeckVersion = seizePersistent;
        await CardPileCmd.Add(frozenCopy, PileType.Draw, skipVisuals: true);
        await CardCmd.AutoPlay(
            choiceContext,
            frozenCopy,
            null,
            AutoPlayType.Default,
            skipCardPileVisuals: true);
        seizeCombatCards.Add(frozenCopy);
        Require(seizePersistent.TimesPlayed == 2 &&
                player.Creature.GetPower<HypePower>()?.Amount == 15,
            "upgraded Seize the Fate did not freeze persisted decay and grant four Hype");
        SavedProperties seizeSave = SavedProperties.From(seizePersistent)
            ?? throw new InvalidOperationException("Seize the Fate did not serialize its persistent state.");
        Require(seizeSave.ints?.Any(property =>
                property.name == nameof(SeizeTheFateCard.TimesPlayed) && property.value == 2) == true,
            "Seize the Fate did not serialize TimesPlayed=2");

        await PowerCmd.Apply<SeizeTheFatePower>(choiceContext, player.Creature, 2m, player.Creature, null);
        SeizeTheFatePower seizePower = player.Creature.GetPower<SeizeTheFatePower>()
            ?? throw new InvalidOperationException("Seize the Fate support power was not applied.");
        await seizePower.BeforeSideTurnEnd(choiceContext, CombatSide.Player, [player.Creature]);
        Require(player.Creature.GetPower<HypePower>()?.Amount == 17,
            "Seize the Fate support power did not grant its amount as Hype at owner turn end");
        await PowerCmd.Remove(seizePower);
        await PowerCmd.Remove(player.Creature.GetPower<HypePower>());
        PersistentDeckRemovalResult seizeCleanup = await PersistentDeckMutation.RemoveAsync(
            seizePersistent,
            showPersistentPreview: false,
            skipCombatVisuals: true);
        Require(seizeCleanup.Success,
            "Seize the Fate persistent cleanup failed");
        cardsToRemove.AddRange(seizeCombatCards);

        await RemoveCombatCardsAsync(cardsToRemove);
        await PlayerCmd.SetEnergy(energyBefore, player);
        await RemoveAddedBlockAsync(choiceContext, player.Creature, playerBlockBefore);
        if (target.Block > 0)
        {
            await CreatureCmd.LoseBlock(choiceContext, target, target.Block, null);
        }
        if (targetBlockBefore > 0)
        {
            await CreatureCmd.GainBlock(target, targetBlockBefore, ValueProp.Unpowered, null, fast: true);
        }
        await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, player.Creature, playerStrengthBefore);
        await RestoreRememberedPowerAsync<WeakPower>(choiceContext, player.Creature, playerWeakBefore);
        await RestoreRememberedPowerAsync<VulnerablePower>(choiceContext, target, targetVulnerableBefore);
        await RestoreRememberedPowerAsync<HypePower>(choiceContext, player.Creature, hypeBefore);
        await RestoreRememberedPowerAsync<PlatingPower>(choiceContext, player.Creature, platingBefore);
        await RestoreRememberedPowerAsync<DexterityPower>(choiceContext, player.Creature, dexterityBefore);
        await RestoreRememberedPowerAsync<DazzlingPower>(choiceContext, player.Creature, dazzlingBefore);
        await RestoreCombatCardsAsync(cardsMovedAside);
    }

    private static async Task VerifyRemainingUncommonMutationsAsync(
        CombatState combatState,
        Player player,
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
        Dictionary<Creature, int> blockBefore = opponents.ToDictionary(
            opponent => opponent,
            opponent => opponent.Block);
        int playerStrengthBefore = await RemoveAndRememberPowerAsync<StrengthPower>(player.Creature);
        int playerWeakBefore = await RemoveAndRememberPowerAsync<WeakPower>(player.Creature);
        Dictionary<Creature, int> vulnerableBefore = new();
        foreach (Creature opponent in opponents)
        {
            vulnerableBefore[opponent] = await RemoveAndRememberPowerAsync<VulnerablePower>(opponent);
            await CreatureCmd.GainBlock(opponent, 1000m, ValueProp.Unpowered, null, fast: true);
        }

        int deckSizeBefore = player.Deck.Cards.Count;
        int gainHistoryBefore = GetGainHistoryCount(player);
        int removalHistoryBefore = GetRemovalHistoryCount(player);
        List<CardModel> cardsToRemove = [];

        MelodyCard previousAttack = await CreateAndAutoPlayAsync<MelodyCard>(
            combatState,
            player,
            choiceContext,
            opponents[0],
            upgraded: false);
        decimal blockBeforeBaseFire = opponents.Sum(opponent => opponent.Block);
        var baseFire = await CreateAndAutoPlayWithPersistentChildAsync<SymbolIFireCard, TimorisCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        decimal expectedBaseFireDamage = opponents.Length * 20m + 6m;
        Require(blockBeforeBaseFire - opponents.Sum(opponent => opponent.Block) == expectedBaseFireDamage,
            "base Symbol I: Fire did not deal its AoE and replay the latest owner Attack once");
        decimal blockBeforeUpgradedFire = opponents.Sum(opponent => opponent.Block);
        var upgradedFire = await CreateAndAutoPlayWithPersistentChildAsync<SymbolIFireCard, TimorisCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        decimal expectedUpgradedFireDamage = opponents.Length * 20m + 12m;
        Require(blockBeforeUpgradedFire - opponents.Sum(opponent => opponent.Block) == expectedUpgradedFireDamage,
            "upgraded Symbol I: Fire did not replay the latest owner Attack twice");
        await CleanupPersistentChildAsync(baseFire.PersistentCard, baseFire.CombatCard);
        await CleanupPersistentChildAsync(upgradedFire.PersistentCard, upgradedFire.CombatCard);
        cardsToRemove.AddRange([previousAttack, baseFire.SourceCard, upgradedFire.SourceCard]);

        foreach (Creature opponent in opponents)
        {
            await CreatureCmd.Heal(opponent, opponent.MaxHp, playAnim: false);
        }
        PersistentDeckAndCombatAddResult utopiaAddition =
            await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<UtopiaCard>(
                player,
                combatState,
                PileType.Hand,
                skipPersistentVisuals: true);
        Require(utopiaAddition.Success,
            "Utopia setup could not add an exact persistent and Hand pair");
        CardModel utopiaPersistent = utopiaAddition.PersistentCard
            ?? throw new InvalidOperationException("Utopia persistent setup result was empty.");
        CardModel utopiaCombat = utopiaAddition.CombatCard
            ?? throw new InvalidOperationException("Utopia combat setup result was empty.");
        int opponentHpBeforeUtopia = opponents.Sum(opponent => opponent.CurrentHp);
        int opponentBlockBeforeUtopia = opponents.Sum(opponent => opponent.Block);
        SakikoPurgeResult utopiaPurge = await SakikoPurgeCommand.RemoveAsync(
            utopiaCombat,
            showPersistentPreview: false,
            skipCombatVisuals: true,
            choiceContext: choiceContext);
        Require(utopiaPurge.Success && utopiaPurge.ReplacedByPlay &&
                !utopiaPersistent.HasBeenRemovedFromState &&
                utopiaPersistent.Pile?.Type == PileType.Deck,
            "Utopia did not replace a combat-pile purge with native play while preserving its persistent card");
        Require(opponentHpBeforeUtopia - opponents.Sum(opponent => opponent.CurrentHp) == 15 &&
                opponents.Sum(opponent => opponent.Block) == opponentBlockBeforeUtopia,
            "Utopia did not deal exactly fifteen unblockable HP loss without consuming Block");
        PersistentDeckRemovalResult utopiaCleanup = await PersistentDeckMutation.RemoveAsync(
            utopiaPersistent,
            showPersistentPreview: false,
            skipCombatVisuals: true);
        Require(utopiaCleanup.Success &&
                utopiaCleanup.RemovedCombatCopies.Count == 1 &&
                ReferenceEquals(utopiaCleanup.RemovedCombatCopies.Single(), utopiaCombat),
            "Utopia cleanup did not remove the exact played combat copy");

        VeritasCard veritas = combatState.CreateCard<VeritasCard>(player);
        CardModel exhaustProbe = combatState.CreateCard<DefendTogawaSakiko>(player);
        await CardPileCmd.AddGeneratedCardToCombat(veritas, PileType.Discard, player);
        await CardPileCmd.AddGeneratedCardToCombat(exhaustProbe, PileType.Hand, player);
        await CardCmd.Exhaust(choiceContext, exhaustProbe, skipVisuals: true);
        Require(veritas.Pile?.Type == PileType.Hand,
            "Veritas did not return its exact Discard copy to Hand after a same-owner Exhaust");
        CardModel[] veritasDraws =
        [
            combatState.CreateCard<DefendTogawaSakiko>(player),
            combatState.CreateCard<StrikeTogawaSakiko>(player),
            combatState.CreateCard<DefendTogawaSakiko>(player)
        ];
        await AddDrawWindowAsync(veritasDraws);
        await CardCmd.AutoPlay(
            choiceContext,
            veritas,
            null,
            AutoPlayType.Default,
            skipCardPileVisuals: true);
        Require(veritasDraws.All(card => card.Pile?.Type == PileType.Hand) &&
                veritas.Pile?.Type == PileType.Discard,
            "Veritas did not draw three before returning to Discard");
        cardsToRemove.AddRange(veritasDraws.Append(veritas).Append(exhaustProbe));

        PersistentDeckAndCombatAddResult wishTarget =
            await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<DefendTogawaSakiko>(
                player,
                combatState,
                PileType.Hand,
                skipPersistentVisuals: true);
        Require(wishTarget.Success,
            "Wish Fulfilled setup could not add an exact persistent and Hand pair");
        TestCardSelector wishSelector = new();
        wishSelector.PrepareToSelect([wishTarget.CombatCard!]);
        WishFulfilledCard wish;
        using (CardSelectCmd.PushSelector(wishSelector))
        {
            wish = await CreateAndAutoPlayAsync<WishFulfilledCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: false);
        }
        Require(wishTarget.PersistentCard!.HasBeenRemovedFromState &&
                wishTarget.CombatCard!.HasBeenRemovedFromState &&
                wish.Pile?.Type == PileType.Exhaust,
            "Wish Fulfilled did not purge the selected exact DeckVersion pair and Exhaust");
        cardsToRemove.Add(wish);

        Require(player.Deck.Cards.Count == deckSizeBefore,
            "remaining uncommon mutation diagnostics did not restore persistent deck size");
        Require(GetGainHistoryCount(player) - gainHistoryBefore == 4 &&
                GetRemovalHistoryCount(player) - removalHistoryBefore == 4,
            "remaining uncommon mutation diagnostics did not record four native adds and removals");

        await RemoveCombatCardsAsync(cardsToRemove);
        foreach (Creature opponent in opponents)
        {
            await RemoveAddedBlockAsync(choiceContext, opponent, blockBefore[opponent]);
            await RestoreRememberedPowerAsync<VulnerablePower>(
                choiceContext,
                opponent,
                vulnerableBefore[opponent]);
        }
        await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, player.Creature, playerStrengthBefore);
        await RestoreRememberedPowerAsync<WeakPower>(choiceContext, player.Creature, playerWeakBefore);
        await RestoreCombatCardsAsync(cardsMovedAside);
    }

    private static async Task VerifySymbolIIIWaterAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext,
        Session session)
    {
        Creature[] opponents = combatState.GetOpponentsOf(player.Creature)
            .Where(creature => creature.IsHittable)
            .ToArray();
        Dictionary<Creature, int> blockBefore = opponents.ToDictionary(
            opponent => opponent,
            opponent => opponent.Block);
        int playerStrengthBefore = await RemoveAndRememberPowerAsync<StrengthPower>(player.Creature);
        int playerWeakBefore = await RemoveAndRememberPowerAsync<WeakPower>(player.Creature);
        Dictionary<Creature, int> vulnerableBefore = new();
        foreach (Creature opponent in opponents)
        {
            vulnerableBefore[opponent] = await RemoveAndRememberPowerAsync<VulnerablePower>(opponent);
            await CreatureCmd.GainBlock(opponent, 100m, ValueProp.Unpowered, null, fast: true);
        }

        int ambergrisBefore = player.Creature.GetPower<AmbergrisPower>()?.Amount ?? 0;
        int turnNumberBefore = player.PlayerCombatState?.TurnNumber
            ?? throw new InvalidOperationException("Symbol III: Water diagnostic requires player combat state");
        int roundNumberBefore = combatState.RoundNumber;
        decimal totalBlockBefore = opponents.Sum(opponent => opponent.Block);
        CombatCardLocation[] cardsMovedAside = await MoveCombatPilesAsideAsync(
            player,
            PileType.Hand,
            PileType.Draw,
            PileType.Discard);
        var water = await CreateAndAutoPlayWithPersistentChildAsync<SymbolIIIWaterCard, DolorisCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        Require(totalBlockBefore - opponents.Sum(opponent => opponent.Block) == 16m,
            "Symbol III: Water did not deal sixteen damage to one random opponent");
        Require(player.Creature.GetPower<AmbergrisPower>()?.Amount == ambergrisBefore + 1,
            "Symbol III: Water did not apply one hidden native Ambergris extra-turn marker");
        Require(CombatManager.Instance.IsPlayerReadyToEndTurn(player),
            "Symbol III: Water did not request native non-cancelable turn end");
        await CleanupPersistentChildAsync(water.PersistentCard, water.CombatCard);
        await RemoveCombatCardsAsync([water.SourceCard]);
        foreach (Creature opponent in opponents)
        {
            await RemoveAddedBlockAsync(choiceContext, opponent, blockBefore[opponent]);
            await RestoreRememberedPowerAsync<VulnerablePower>(
                choiceContext,
                opponent,
                vulnerableBefore[opponent]);
        }
        await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, player.Creature, playerStrengthBefore);
        await RestoreRememberedPowerAsync<WeakPower>(choiceContext, player.Creature, playerWeakBefore);

        session.WaterOwner = player;
        session.WaterTurnNumberBefore = turnNumberBefore;
        session.WaterRoundNumberBefore = roundNumberBefore;
        session.WaterAmbergrisAmountBefore = ambergrisBefore;
        session.WaterCardsMovedAside = cardsMovedAside
            .Select(location => new CombatCardLocation(location.Card, PileType.Discard))
            .ToArray();
        session.WaterExtraTurnHookSeen = false;
        session.WaterExtraTurnArmed = true;
    }

    public static Task ObserveAfterTakingExtraTurnAsync(Player player, CardModel observer)
    {
        if (!NativeSmokeTrace.N5BatchEnabled ||
            observer.CombatState is not CombatState combatState ||
            !Sessions.TryGetValue(combatState, out Session? session) ||
            !session.WaterExtraTurnArmed ||
            !ReferenceEquals(session.WaterOwner, player))
        {
            return Task.CompletedTask;
        }

        Require(combatState.RoundNumber == session.WaterRoundNumberBefore,
            "Symbol III: Water advanced the combat round before its extra turn");
        Require(player.PlayerCombatState?.TurnNumber == session.WaterTurnNumberBefore + 1,
            "Symbol III: Water did not increment the owner's player-turn number for its extra turn");
        session.WaterExtraTurnHookSeen = true;
        return Task.CompletedTask;
    }

    public static async Task ObserveAfterPlayerTurnStartAsync(Player player, CardModel observer)
    {
        if (!NativeSmokeTrace.N5BatchEnabled ||
            observer.CombatState is not CombatState combatState ||
            !Sessions.TryGetValue(combatState, out Session? session) ||
            !session.WaterExtraTurnArmed ||
            !ReferenceEquals(session.WaterOwner, player))
        {
            return;
        }

        Require(session.WaterExtraTurnHookSeen,
            "Symbol III: Water reached player-turn start without the native extra-turn hook");
        Require(combatState.RoundNumber == session.WaterRoundNumberBefore,
            "Symbol III: Water advanced the combat round during its extra-turn transition");
        Require(player.PlayerCombatState?.TurnNumber == session.WaterTurnNumberBefore + 1,
            "Symbol III: Water extra turn started with the wrong player-turn number");
        int ambergrisAfter = player.Creature.GetPower<AmbergrisPower>()?.Amount ?? 0;
        Require(ambergrisAfter == session.WaterAmbergrisAmountBefore,
            "native Ambergris did not consume exactly one marker for Symbol III: Water's extra turn");

        await RestoreCombatCardsAsync(session.WaterCardsMovedAside);
        session.WaterCardsMovedAside = [];
        session.WaterExtraTurnArmed = false;
        NativeSmokeTrace.N5Info(
            "Symbol III Water extra-turn lifecycle passed. TurnIncrement=1, RoundPreserved=true, AmbergrisConsumed=1.");
    }

    private static async Task<int> RemoveAndRememberPowerAsync<TPower>(Creature creature)
        where TPower : PowerModel
    {
        TPower? power = creature.GetPower<TPower>();
        int amount = power?.Amount ?? 0;
        await PowerCmd.Remove(power);
        return amount;
    }

    private static async Task RestoreRememberedPowerAsync<TPower>(
        PlayerChoiceContext choiceContext,
        Creature creature,
        int amount)
        where TPower : PowerModel
    {
        if (amount != 0)
        {
            await PowerCmd.Apply<TPower>(choiceContext, creature, amount, creature, null);
        }
    }
}
