using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using SakikoCrueltyPower = TogawaSakiko.NativeCode.Models.Powers.CrueltyPower;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static partial class N5BatchDiagnostics
{
    private sealed class AveMujicaDiagnosticSelector : ICardSelector
    {
        public IReadOnlyList<CardModel> OfferedCards { get; private set; } = [];

        public CardModel? SelectedCard { get; private set; }

        public Task<IEnumerable<CardModel>> GetSelectedCards(
            IEnumerable<CardModel> options,
            int minSelect,
            int maxSelect)
        {
            CardModel[] offered = options.ToArray();
            OfferedCards = offered;
            SelectedCard = offered.FirstOrDefault(card => card is SymbolIIAirCard)
                ?? offered.FirstOrDefault(card => card is SymbolIVEarthCard)
                ?? offered.FirstOrDefault(card => card is EtherCard)
                ?? offered.FirstOrDefault(card => card is SymbolIFireCard)
                ?? offered.FirstOrDefault();
            return Task.FromResult<IEnumerable<CardModel>>(
                SelectedCard is null ? [] : [SelectedCard]);
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

    private static async Task VerifyRareFinaleAsync(
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
        Require(opponents.Length > 0, "rare finale diagnostics require at least one hittable opponent");

        decimal playerBlockBefore = player.Creature.Block;
        Dictionary<Creature, int> opponentBlockBefore = opponents.ToDictionary(
            opponent => opponent,
            opponent => opponent.Block);
        Dictionary<Creature, int> vulnerableBefore = new();
        foreach (Creature opponent in opponents)
        {
            vulnerableBefore[opponent] = await RemoveAndRememberPowerAsync<VulnerablePower>(opponent);
            await CreatureCmd.GainBlock(opponent, 2000m, ValueProp.Unpowered, null, fast: true);
        }

        await VerifyAveMujicaVariantAsync(
            combatState,
            player,
            opponents[0],
            choiceContext,
            upgraded: false);
        await VerifyAveMujicaVariantAsync(
            combatState,
            player,
            opponents[0],
            choiceContext,
            upgraded: true);

        PowerModel[] preExistingRemovable = BlackBirthdayCard.GetRemovablePowers(player.Creature.Powers);
        foreach (PowerModel power in preExistingRemovable)
        {
            await PowerCmd.Remove(power);
        }
        Require(BlackBirthdayCard.GetRemovablePowers(player.Creature.Powers).Length == 0,
            "Black Birthday diagnostic isolation retained a removable player power");

        await VerifyBlackBirthdayVariantAsync(
            combatState,
            player,
            opponents,
            choiceContext,
            upgraded: false,
            expectedBlockLoss: 28m);
        await VerifyBlackBirthdayVariantAsync(
            combatState,
            player,
            opponents,
            choiceContext,
            upgraded: true,
            expectedBlockLoss: 36m);

        await RemoveAddedBlockAsync(choiceContext, player.Creature, playerBlockBefore);
        foreach (Creature opponent in opponents)
        {
            await RemoveAddedBlockAsync(choiceContext, opponent, opponentBlockBefore[opponent]);
            await RestoreRememberedPowerAsync<VulnerablePower>(
                choiceContext,
                opponent,
                vulnerableBefore[opponent]);
        }
        await RestoreCombatCardsAsync(cardsMovedAside);

        NativeSmokeTrace.N5Info(
            "rare finale passed. AveMujica=Base+Upgrade+PersistentSelection, BlackBirthday=Base28+Upgrade36+AllPowerRemoval.");
    }

    private static async Task VerifyAveMujicaVariantAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext,
        bool upgraded)
    {
        HashSet<CardModel> persistentBefore = PileType.Deck.GetPile(player).Cards
            .ToHashSet<CardModel>(ReferenceEqualityComparer.Instance);
        int gainHistoryBefore = GetGainHistoryCount(player);
        AveMujicaDiagnosticSelector selector = new();
        AveMujicaCard source;
        using (CardSelectCmd.PushSelector(selector))
        {
            source = await CreateAndAutoPlayAsync<AveMujicaCard>(
                combatState,
                player,
                choiceContext,
                target,
                upgraded);
        }

        CardModel selected = selector.SelectedCard
            ?? throw new InvalidOperationException("Ave Mujica diagnostic did not select an offered card.");
        Require(selector.OfferedCards.Count == AveMujicaCard.ChoiceCount &&
                selector.OfferedCards.Distinct<CardModel>(ReferenceEqualityComparer.Instance).Count() ==
                    AveMujicaCard.ChoiceCount,
            "Ave Mujica did not offer three distinct cards");
        Require(selector.OfferedCards.Contains(selected, ReferenceEqualityComparer.Instance) &&
                selected is SymbolIIAirCard or SymbolIVEarthCard or EtherCard,
            "Ave Mujica diagnostic did not select a safe card from its actual generated options");

        CardModel[] persistentAdds = PileType.Deck.GetPile(player).Cards
            .Where(card => !persistentBefore.Contains(card))
            .ToArray();
        CardModel selectedPersistent = persistentAdds.Single(card => card.Id == selected.Id);
        Require(persistentAdds.Length == 2 &&
                selectedPersistent.Pile?.Type == PileType.Deck &&
                selectedPersistent.CurrentUpgradeLevel == selected.CurrentUpgradeLevel &&
                selected.IsUpgraded == upgraded,
            $"{(upgraded ? "upgraded" : "base")} Ave Mujica did not persist a stat-equivalent selected card");
        Require(GetGainHistoryCount(player) - gainHistoryBefore == 2,
            $"{(upgraded ? "upgraded" : "base")} Ave Mujica did not record its selected card and generated curse");
        Require(source.Pile?.Type == PileType.Discard && selected.Pile?.Type == PileType.Discard,
            $"{(upgraded ? "upgraded" : "base")} Ave Mujica did not finish both attack cards in Discard");

        int linkedCombatCopiesRemoved = 0;
        foreach (CardModel persistentCard in persistentAdds)
        {
            PersistentDeckRemovalResult cleanup = await PersistentDeckMutation.RemoveAsync(
                persistentCard,
                showPersistentPreview: false,
                skipCombatVisuals: true);
            Require(cleanup.Success,
                $"{(upgraded ? "upgraded" : "base")} Ave Mujica persistent cleanup failed for {persistentCard.Id}");
            linkedCombatCopiesRemoved += cleanup.RemovedCombatCopies.Count;
        }
        Require(linkedCombatCopiesRemoved == 1,
            $"{(upgraded ? "upgraded" : "base")} Ave Mujica did not create exactly one linked curse combat copy");
        await RemoveCombatCardsAsync([source, selected]);
    }

    private static async Task VerifyBlackBirthdayVariantAsync(
        CombatState combatState,
        Player player,
        IReadOnlyList<Creature> opponents,
        PlayerChoiceContext choiceContext,
        bool upgraded,
        decimal expectedBlockLoss)
    {
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            player.Creature,
            2m,
            player.Creature,
            null);
        await PowerCmd.Apply<DexterityPower>(
            choiceContext,
            player.Creature,
            1m,
            player.Creature,
            null);
        PowerModel[] removable = BlackBirthdayCard.GetRemovablePowers(player.Creature.Powers);
        Require(removable.Length == 2 &&
                removable.Any(power => power is StrengthPower { Amount: 2 }) &&
                removable.Any(power => power is DexterityPower { Amount: 1 }),
            "Black Birthday setup did not contain exactly the two intended removable powers");

        Dictionary<Creature, int> blockBefore = opponents.ToDictionary(
            opponent => opponent,
            opponent => opponent.Block);
        BlackBirthdayCard source = await CreateAndAutoPlayAsync<BlackBirthdayCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded);
        Require(opponents.All(opponent => blockBefore[opponent] - opponent.Block == expectedBlockLoss),
            $"{(upgraded ? "upgraded" : "base")} Black Birthday did not deal {expectedBlockLoss} to every opponent");
        Require(BlackBirthdayCard.GetRemovablePowers(player.Creature.Powers).Length == 0,
            $"{(upgraded ? "upgraded" : "base")} Black Birthday did not remove every supported player power");
        Require(source.Pile?.Type == PileType.Exhaust,
            $"{(upgraded ? "upgraded" : "base")} Black Birthday did not enter Exhaust");
        await RemoveCombatCardsAsync([source]);
    }

    private static async Task VerifyRareMutationsAsync(
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
        List<CardModel> cardsToRemove = [];
        decimal targetBlockBefore = target.Block;
        int playerStrengthBefore = await RemoveAndRememberPowerAsync<StrengthPower>(player.Creature);
        int playerWeakBefore = await RemoveAndRememberPowerAsync<WeakPower>(player.Creature);
        int playerDazzlingBefore = await RemoveAndRememberPowerAsync<DazzlingPower>(player.Creature);
        int targetVulnerableBefore = await RemoveAndRememberPowerAsync<VulnerablePower>(target);
        await CreatureCmd.GainBlock(target, 500m, ValueProp.Unpowered, null, fast: true);

        CardPileAddResult sourcePersistentResult = await PersistentDeckMutation.AddCanonicalAsync<MasqueradeRhapsodyRequestCard>(
            player,
            skipVisuals: true);
        Require(sourcePersistentResult.success,
            "As Your Heart Desires setup could not add its persistent source card");
        MasqueradeRhapsodyRequestCard sourcePersistent =
            (MasqueradeRhapsodyRequestCard)sourcePersistentResult.cardAdded;
        CardCmd.Upgrade(sourcePersistent, CardPreviewStyle.None);
        sourcePersistent.IncreaseFromPurge(4);
        MasqueradeRhapsodyRequestCard sourceCombat =
            (MasqueradeRhapsodyRequestCard)combatState.CloneCard(sourcePersistent);
        sourceCombat.DeckVersion = sourcePersistent;
        await CardPileCmd.AddGeneratedCardToCombat(sourceCombat, PileType.Hand, player);
        List<CardModel> copiedPersistentCards = [];
        void OnAsYourHeartDesiresCopy(CardModel card)
        {
            if (!ReferenceEquals(card, sourcePersistent) &&
                card is MasqueradeRhapsodyRequestCard)
            {
                copiedPersistentCards.Add(card);
            }
        }
        int gainHistoryBeforeCopy = GetGainHistoryCount(player);
        player.Deck.CardAdded += OnAsYourHeartDesiresCopy;
        AsYourHeartDesiresCard asYourHeartDesires;
        try
        {
            TestCardSelector copySelector = new();
            copySelector.PrepareToSelect([sourceCombat]);
            using (CardSelectCmd.PushSelector(copySelector))
            {
                asYourHeartDesires = await CreateAndAutoPlayAsync<AsYourHeartDesiresCard>(
                    combatState,
                    player,
                    choiceContext,
                    null,
                    upgraded: true);
            }
        }
        finally
        {
            player.Deck.CardAdded -= OnAsYourHeartDesiresCopy;
        }
        MasqueradeRhapsodyRequestCard copiedPersistent =
            (MasqueradeRhapsodyRequestCard)copiedPersistentCards.Single();
        Require(copiedPersistent.Pile?.Type == PileType.Deck &&
                copiedPersistent.IsUpgraded &&
                copiedPersistent.PermanentDamageIncrease == 4,
            "As Your Heart Desires did not preserve upgrade and saved stat state in its persistent copy");
        Require(sourceCombat.Pile?.Type == PileType.Hand &&
                asYourHeartDesires.Pile?.Type == PileType.Exhaust &&
                GetGainHistoryCount(player) - gainHistoryBeforeCopy == 1,
            "As Your Heart Desires did not preserve the selected combat card, Exhaust, and record one deck gain");
        PersistentDeckRemovalResult copiedCleanup = await PersistentDeckMutation.RemoveAsync(
            copiedPersistent,
            showPersistentPreview: false,
            skipCombatVisuals: true);
        Require(copiedCleanup.Success && copiedCleanup.RemovedCombatCopies.Count == 0,
            "As Your Heart Desires copied-card cleanup did not use the native persistent command");
        PersistentDeckRemovalResult sourceCleanup = await PersistentDeckMutation.RemoveAsync(
            sourcePersistent,
            showPersistentPreview: false,
            skipCombatVisuals: true);
        Require(sourceCleanup.Success &&
                sourceCleanup.RemovedCombatCopies.Count == 1 &&
                ReferenceEquals(sourceCleanup.RemovedCombatCopies.Single(), sourceCombat),
            "As Your Heart Desires source cleanup did not remove its exact DeckVersion combat copy");
        cardsToRemove.Add(asYourHeartDesires);

        CardModel baseEtherSkill = combatState.CreateCard<DefendTogawaSakiko>(player);
        CardModel baseEtherCurse = combatState.CreateCard<AmorisCard>(player);
        CardModel baseEtherAttack = combatState.CreateCard<DarkHeavenCard>(player);
        await AddDrawWindowAsync([baseEtherSkill, baseEtherCurse, baseEtherAttack]);
        decimal blockBeforeBaseEther = target.Block;
        var baseEther = await CreateAndAutoPlayWithPersistentChildAsync<EtherCard, OblivionisCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        Require(baseEtherSkill.Pile?.Type == PileType.Exhaust &&
                baseEtherCurse.Pile?.Type == PileType.Exhaust &&
                baseEtherAttack.Pile?.Type == PileType.Draw,
            "base Ether did not exhaust only the non-Attacks from Draw");
        Require(blockBeforeBaseEther - target.Block == 4m,
            "base Ether did not deal two damage for each of two successful exhausts");
        await CleanupPersistentChildAsync(baseEther.PersistentCard, baseEther.CombatCard);
        await RemoveCombatCardsAsync(
            [baseEther.SourceCard, baseEtherSkill, baseEtherCurse, baseEtherAttack]);

        CardModel upgradedEtherSkillOne = combatState.CreateCard<DefendTogawaSakiko>(player);
        CardModel upgradedEtherSkillTwo = combatState.CreateCard<GreetingsCard>(player);
        CardModel upgradedEtherCurse = combatState.CreateCard<AmorisCard>(player);
        CardModel upgradedEtherAttack = combatState.CreateCard<DarkHeavenCard>(player);
        await AddDrawWindowAsync(
            [upgradedEtherSkillOne, upgradedEtherSkillTwo, upgradedEtherCurse, upgradedEtherAttack]);
        decimal blockBeforeUpgradedEther = target.Block;
        var upgradedEther = await CreateAndAutoPlayWithPersistentChildAsync<EtherCard, OblivionisCard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: true);
        Require(new[] { upgradedEtherSkillOne, upgradedEtherSkillTwo, upgradedEtherCurse }
                .All(card => card.Pile?.Type == PileType.Exhaust) &&
                upgradedEtherAttack.Pile?.Type == PileType.Draw,
            "upgraded Ether did not exhaust exactly three non-Attacks from Draw");
        Require(blockBeforeUpgradedEther - target.Block == 9m,
            "upgraded Ether did not deal three damage for each of three successful exhausts");
        await CleanupPersistentChildAsync(upgradedEther.PersistentCard, upgradedEther.CombatCard);
        await RemoveCombatCardsAsync(
            [upgradedEther.SourceCard, upgradedEtherSkillOne, upgradedEtherSkillTwo, upgradedEtherCurse, upgradedEtherAttack]);

        await PowerCmd.Apply<DazzlingPower>(
            choiceContext,
            player.Creature,
            5m,
            player.Creature,
            null);
        CardModel masksSelection = combatState.CreateCard<DefendTogawaSakiko>(player);
        await CardPileCmd.AddGeneratedCardToCombat(masksSelection, PileType.Hand, player);
        TestCardSelector masksSelector = new();
        masksSelector.PrepareToSelect([masksSelection]);
        MasksCard masks;
        using (CardSelectCmd.PushSelector(masksSelector))
        {
            masks = await CreateAndAutoPlayAsync<MasksCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: true);
        }
        MasksCard masksReplacement = PileType.Hand.GetPile(player).Cards.OfType<MasksCard>().Single();
        Require(masksSelection.HasBeenRemovedFromState && masksSelection.Pile is null,
            "Masks did not remove the exact selected Hand card through the native command");
        Require(masksReplacement.IsUpgraded &&
                masksReplacement.Keywords.Contains(CardKeyword.Retain),
            "upgraded Masks did not create an upgraded Retain replacement in Hand");
        Require(player.Creature.GetPower<DazzlingPower>()?.Amount == 3 &&
                player.Creature.GetPower<StrengthPower>()?.Amount == 2,
            "Masks did not reduce Dazzling by two and gain two Strength");
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        await RemoveCombatCardsAsync([masks, masksReplacement]);

        int baselineDeckSize = player.Deck.Cards.Count;
        int baselineSpringCost = SpringSunlightCard.CalculateCost(baselineDeckSize);
        SpringSunlightCard spring = combatState.CreateCard<SpringSunlightCard>(player);
        CardCmd.Upgrade(spring, CardPreviewStyle.None);
        await CardPileCmd.AddGeneratedCardToCombat(spring, PileType.Hand, player);
        Require(spring.EnergyCost.GetWithModifiers(CostModifiers.None) == baselineSpringCost,
            "Spring Sunlight did not initialize its live cost from persistent deck size");
        int nextThreshold = (baselineDeckSize / 6 + 1) * 6;
        int probeCount = nextThreshold - baselineDeckSize;
        List<CardModel> springPersistentProbes = [];
        for (int index = 0; index < probeCount; index++)
        {
            springPersistentProbes.Add(await AddPersistentProbeAsync<DefendTogawaSakiko>(player));
        }
        Require(player.Deck.Cards.Count == nextThreshold &&
                spring.EnergyCost.GetWithModifiers(CostModifiers.None) == baselineSpringCost + 1,
            "Spring Sunlight did not increase cost immediately at the next six-card threshold");
        foreach (CardModel probe in springPersistentProbes)
        {
            PersistentDeckRemovalResult removal = await PersistentDeckMutation.RemoveAsync(
                probe,
                showPersistentPreview: false,
                skipCombatVisuals: true);
            Require(removal.Success,
                "Spring Sunlight threshold cleanup failed to remove a persistent probe");
        }
        Require(player.Deck.Cards.Count == baselineDeckSize &&
                spring.EnergyCost.GetWithModifiers(CostModifiers.None) == baselineSpringCost,
            "Spring Sunlight did not restore cost after persistent deck removal");
        decimal blockBeforeSpring = target.Block;
        await CardCmd.AutoPlay(
            choiceContext,
            spring,
            target,
            AutoPlayType.Default,
            skipCardPileVisuals: true);
        Require(blockBeforeSpring - target.Block == 40m &&
                spring.Pile?.Type == PileType.Discard,
            "upgraded Spring Sunlight did not deal forty damage and enter Discard");
        cardsToRemove.Add(spring);

        await RemoveCombatCardsAsync(cardsToRemove);
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, player.Creature, playerStrengthBefore);
        await RestoreRememberedPowerAsync<WeakPower>(choiceContext, player.Creature, playerWeakBefore);
        await RestoreRememberedPowerAsync<DazzlingPower>(choiceContext, player.Creature, playerDazzlingBefore);
        await RestoreRememberedPowerAsync<VulnerablePower>(choiceContext, target, targetVulnerableBefore);
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);
        await RestoreCombatCardsAsync(cardsMovedAside);

        NativeSmokeTrace.N5Info(
            "rare mutations passed. AsYourHeartDesires=SavedStatCopy+History+ExactDeckVersionCleanup, Ether=Base4+Upgrade9+LinkedOblivionis, Masks=DazzlingMinus2+Strength2+ExactTransform, SpringSunlight=LiveThresholdCost+Damage40.");
    }

    private static async Task VerifyRarePowersAsync(
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
        List<CardModel> cardsToRemove = [];
        decimal playerBlockBefore = player.Creature.Block;
        decimal targetBlockBefore = target.Block;
        decimal playerHpBefore = player.Creature.CurrentHp;
        int playerStrengthBefore = await RemoveAndRememberPowerAsync<StrengthPower>(player.Creature);
        int playerWeakBefore = await RemoveAndRememberPowerAsync<WeakPower>(player.Creature);
        int playerVulnerableBefore = await RemoveAndRememberPowerAsync<VulnerablePower>(player.Creature);
        int playerHypeBefore = await RemoveAndRememberPowerAsync<HypePower>(player.Creature);
        int playerThornsBefore = await RemoveAndRememberPowerAsync<ThornsPower>(player.Creature);
        int playerArtifactBefore = await RemoveAndRememberPowerAsync<ArtifactPower>(player.Creature);
        int targetStrengthBefore = await RemoveAndRememberPowerAsync<StrengthPower>(target);
        int targetVulnerableBefore = await RemoveAndRememberPowerAsync<VulnerablePower>(target);
        Creature[] powerTargets = combatState.GetOpponentsOf(player.Creature)
            .Where(creature => creature.CanReceivePowers)
            .ToArray();
        Dictionary<Creature, int> targetHypeBefore = [];
        Dictionary<Creature, int> targetArtifactBefore = [];
        foreach (Creature opponent in powerTargets)
        {
            targetHypeBefore[opponent] = await RemoveAndRememberPowerAsync<HypePower>(opponent);
            targetArtifactBefore[opponent] = await RemoveAndRememberPowerAsync<ArtifactPower>(opponent);
        }
        Require(player.Creature.GetPower<CharismaticFormPower>() is null &&
                player.Creature.GetPower<SakikoCrueltyPower>() is null &&
                player.Creature.GetPower<CrychicPower>() is null &&
                player.Creature.GetPower<WishYouGoodLuckPower>() is null &&
                player.Creature.GetPower<WorldviewPower>() is null &&
                player.Creature.GetPower<PridePower>() is null,
            "rare power diagnostics started with an unexpected rare power already active");
        await CreatureCmd.GainBlock(target, 500m, ValueProp.Unpowered, null, fast: true);

        CharismaticFormCard charismatic = await CreateAndAutoPlayAsync<CharismaticFormCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        Require(player.Creature.GetPower<CharismaticFormPower>()?.Amount == 1,
            "Charismatic Form did not apply its single copy listener");
        Require(powerTargets.All(opponent => opponent.GetPower<HypePower>()?.Amount == 2),
            "Charismatic Form did not give every eligible enemy two Hype");
        Require(player.Creature.GetPower<HypePower>()?.Amount == powerTargets.Length * 2,
            "Charismatic Form did not copy each enemy Hype gain by its actual amount");
        await PowerCmd.Apply<StrengthPower>(choiceContext, target, 3m, target, null);
        Require(target.GetPower<StrengthPower>()?.Amount == 3 &&
                player.Creature.GetPower<StrengthPower>()?.Amount == 3,
            "Charismatic Form did not copy an enemy's actual three-Strength gain");
        await PowerCmd.Apply<PridePower>(choiceContext, target, 1m, target, null);
        Require(player.Creature.GetPower<PridePower>() is null,
            "Charismatic Form copied unsupported Pride internal state");
        await PowerCmd.Remove(target.GetPower<PridePower>());
        await PowerCmd.Remove(player.Creature.GetPower<CharismaticFormPower>());
        await PowerCmd.Remove(player.Creature.GetPower<HypePower>());
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        await PowerCmd.Remove(target.GetPower<StrengthPower>());
        foreach (Creature opponent in powerTargets)
        {
            await PowerCmd.Remove(opponent.GetPower<HypePower>());
        }
        cardsToRemove.Add(charismatic);

        CrueltyCard baseCruelty = await CreateAndAutoPlayAsync<CrueltyCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        Require(player.Creature.GetPower<VulnerablePower>()?.Amount == 2 &&
                player.Creature.GetPower<SakikoCrueltyPower>()?.Amount == 2,
            "base Cruelty did not apply two Vulnerable and two draw triggers");
        await PowerCmd.Remove(player.Creature.GetPower<SakikoCrueltyPower>());
        await PowerCmd.Remove(player.Creature.GetPower<VulnerablePower>());
        cardsToRemove.Add(baseCruelty);

        CardModel crueltyDrawOne = combatState.CreateCard<DefendTogawaSakiko>(player);
        CardModel crueltyDrawTwo = combatState.CreateCard<StrikeTogawaSakiko>(player);
        await AddDrawWindowAsync([crueltyDrawOne, crueltyDrawTwo]);
        CrueltyCard upgradedCruelty = await CreateAndAutoPlayAsync<CrueltyCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        Require(player.Creature.GetPower<VulnerablePower>()?.Amount == 1 &&
                player.Creature.GetPower<SakikoCrueltyPower>()?.Amount == 2,
            "upgraded Cruelty did not apply one Vulnerable and preserve two draw triggers");
        DesireCard crueltyDesire = await CreateAndAutoPlayAsync<DesireCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        Require(crueltyDrawOne.Pile?.Type == PileType.Hand &&
                crueltyDrawTwo.Pile?.Type == PileType.Hand,
            "Cruelty did not draw two cards after the owner played Desire");
        await PowerCmd.Remove(player.Creature.GetPower<SakikoCrueltyPower>());
        await PowerCmd.Remove(player.Creature.GetPower<VulnerablePower>());
        await RemoveCombatCardsAsync(
            [upgradedCruelty, crueltyDesire, crueltyDrawOne, crueltyDrawTwo]);

        WishYouGoodLuckCard wishGoodLuck = await CreateAndAutoPlayAsync<WishYouGoodLuckCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        Require(player.Creature.GetPower<WishYouGoodLuckPower>()?.Amount == 2,
            "upgraded Wish You Good Luck did not apply two stacks");
        await CreatureCmd.GainBlock(player.Creature, 10m, ValueProp.Unpowered, null, fast: true);
        await CreatureCmd.Damage(
            choiceContext,
            player.Creature,
            3m,
            ValueProp.Move,
            target);
        Require(player.Creature.GetPower<ThornsPower>()?.Amount == 2,
            "Wish You Good Luck did not gain two Thorns after a fully blocked powered attack");
        decimal remainingBlock = player.Creature.Block;
        if (remainingBlock > 0)
        {
            await CreatureCmd.LoseBlock(choiceContext, player.Creature, remainingBlock, null);
        }
        await CreatureCmd.GainBlock(player.Creature, 1m, ValueProp.Unpowered, null, fast: true);
        await CreatureCmd.Damage(
            choiceContext,
            player.Creature,
            3m,
            ValueProp.Move,
            target);
        Require(player.Creature.GetPower<ThornsPower>()?.Amount == 2,
            "Wish You Good Luck triggered when powered damage was not fully blocked");
        await PowerCmd.Remove(player.Creature.GetPower<WishYouGoodLuckPower>());
        await PowerCmd.Remove(player.Creature.GetPower<ThornsPower>());
        cardsToRemove.Add(wishGoodLuck);

        WorldviewCard worldview = await CreateAndAutoPlayAsync<WorldviewCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        Require(player.Creature.GetPower<WorldviewPower>()?.Amount == 1,
            "upgraded Worldview did not apply its single power");
        CardModel worldviewUnplayable = combatState.CreateCard<AmorisCard>(player);
        await AddDrawWindowAsync([worldviewUnplayable]);
        await CardPileCmd.Draw(choiceContext, 1m, player);
        CardModel worldviewReplacement = PileType.Hand.GetPile(player).Cards.Single();
        Require(worldviewUnplayable.HasBeenRemovedFromState && worldviewUnplayable.Pile is null,
            "Worldview did not remove the exact Unplayable card drawn");
        Require(worldviewReplacement.Type == CardType.Attack &&
                !worldviewReplacement.Keywords.Contains(CardKeyword.Unplayable),
            "Worldview did not replace the Unplayable draw with a playable Attack");
        await PowerCmd.Remove(player.Creature.GetPower<WorldviewPower>());
        await RemoveCombatCardsAsync([worldview, worldviewReplacement]);

        CrychicPower crychicPower = await PowerCmd.Apply<CrychicPower>(
                choiceContext,
                player.Creature,
                1m,
                player.Creature,
                null)
            ?? throw new InvalidOperationException("Crychic power diagnostic could not apply its power");
        await crychicPower.AfterPlayerTurnStart(choiceContext, player);
        CardModel crychicTurnPhantom = PileType.Hand.GetPile(player).Cards.Single();
        Require(IsRareDiagnosticPhantom(crychicTurnPhantom) &&
                crychicTurnPhantom.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0,
            "Crychic power did not generate one free phantom at player turn start");
        Require(player.Creature.GetPower<CrychicPower>() is null,
            "Crychic power did not decrement and fully remove its final stack");
        await RemoveCombatCardsAsync([crychicTurnPhantom]);

        CardModel prideDrawOne = combatState.CreateCard<StrikeTogawaSakiko>(player);
        CardModel prideDrawTwo = combatState.CreateCard<DarkHeavenCard>(player);
        CardModel prideDiscardAttack = combatState.CreateCard<KingsCard>(player);
        await AddDrawWindowAsync([prideDrawOne, prideDrawTwo]);
        await CardPileCmd.AddGeneratedCardToCombat(
            prideDiscardAttack,
            PileType.Discard,
            player,
            CardPilePosition.Bottom);
        PrideCard pride = await CreateAndAutoPlayAsync<PrideCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        Require(prideDrawOne.Pile?.Type == PileType.Hand &&
                prideDrawTwo.Pile?.Type == PileType.Hand &&
                prideDiscardAttack.Pile?.Type == PileType.Discard,
            "upgraded Pride did not take two Attacks from Draw before considering Discard");
        PridePower appliedPride = player.Creature.GetPower<PridePower>()
            ?? throw new InvalidOperationException("Pride did not apply its delayed-return power");
        Require(appliedPride.RegisteredCopyCount == 1 && pride.Pile?.Type == PileType.Exhaust,
            "Pride did not register one per-instance snapshot and enter Exhaust");
        await appliedPride.AfterSideTurnEnd(
            choiceContext,
            CombatSide.Player,
            [player.Creature]);
        PrideCard returnedPride = PileType.Draw.GetPile(player).Cards
            .OfType<PrideCard>()
            .First();
        Require(ReferenceEquals(PileType.Draw.GetPile(player).Cards.First(), returnedPride) &&
                returnedPride.IsUpgraded &&
                player.Creature.GetPower<PridePower>() is null,
            "Pride did not return its upgraded snapshot to the top of Draw and remove its power");
        cardsToRemove.AddRange(
            [pride, returnedPride, prideDrawOne, prideDrawTwo, prideDiscardAttack]);

        await RemoveCombatCardsAsync(cardsToRemove);
        if (player.Creature.CurrentHp < playerHpBefore)
        {
            await CreatureCmd.Heal(
                player.Creature,
                playerHpBefore - player.Creature.CurrentHp,
                playAnim: false);
        }
        await RemoveAddedBlockAsync(choiceContext, player.Creature, playerBlockBefore);
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);
        await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, player.Creature, playerStrengthBefore);
        await RestoreRememberedPowerAsync<WeakPower>(choiceContext, player.Creature, playerWeakBefore);
        await RestoreRememberedPowerAsync<VulnerablePower>(choiceContext, player.Creature, playerVulnerableBefore);
        await RestoreRememberedPowerAsync<HypePower>(choiceContext, player.Creature, playerHypeBefore);
        await RestoreRememberedPowerAsync<ThornsPower>(choiceContext, player.Creature, playerThornsBefore);
        await RestoreRememberedPowerAsync<ArtifactPower>(choiceContext, player.Creature, playerArtifactBefore);
        await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, target, targetStrengthBefore);
        await RestoreRememberedPowerAsync<VulnerablePower>(choiceContext, target, targetVulnerableBefore);
        foreach (Creature opponent in powerTargets)
        {
            await RestoreRememberedPowerAsync<HypePower>(
                choiceContext,
                opponent,
                targetHypeBefore[opponent]);
            await RestoreRememberedPowerAsync<ArtifactPower>(
                choiceContext,
                opponent,
                targetArtifactBefore[opponent]);
        }
        await RestoreCombatCardsAsync(cardsMovedAside);

        NativeSmokeTrace.N5Info(
            "rare powers passed. Charismatic=EnemyHype+ActualStrengthCopy+PrideRejected, Cruelty=Vulnerable2+1+DesireDraw2, WishGoodLuck=FullBlockThorns2, Worldview=ExactUnplayableReplacement, CrychicPower=FreePhantom+Decrement, Pride=DrawPriority2+DelayedTopReturn.");
    }

    private static async Task VerifyRareCoreAsync(
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
        List<CardModel> cardsToRemove = [];
        decimal targetBlockBefore = target.Block;
        int energyBefore = player.PlayerCombatState?.Energy
            ?? throw new InvalidOperationException("rare diagnostics require player combat state");
        int playerStrengthBefore = await RemoveAndRememberPowerAsync<StrengthPower>(player.Creature);
        int playerWeakBefore = await RemoveAndRememberPowerAsync<WeakPower>(player.Creature);
        int targetVulnerableBefore = await RemoveAndRememberPowerAsync<VulnerablePower>(target);
        await CreatureCmd.GainBlock(target, 500m, ValueProp.Unpowered, null, fast: true);

        CardModel baseZero = combatState.CreateCard<GreetingsCard>(player);
        CardModel baseOne = combatState.CreateCard<DarkHeavenCard>(player);
        CardModel baseThree = combatState.CreateCard<SoraNoMusicaCard>(player);
        await AddDrawWindowAsync([baseZero, baseOne, baseThree]);
        BandInvitationCard baseInvitation = await CreateAndAutoPlayAsync<BandInvitationCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: false);
        Require(
            new[] { baseZero, baseOne, baseThree }.All(card => card.Pile?.Type == PileType.Hand),
            "base Band Invitation did not draw through a positive energy total of four");
        Require(baseInvitation.Pile?.Type == PileType.Discard,
            "base Band Invitation did not enter Discard");
        await RemoveCombatCardsAsync([baseInvitation, baseZero, baseOne, baseThree]);

        CardModel upgradedZero = combatState.CreateCard<GreetingsCard>(player);
        CardModel upgradedOneA = combatState.CreateCard<DarkHeavenCard>(player);
        CardModel upgradedOneB = combatState.CreateCard<DarkHeavenCard>(player);
        CardModel upgradedThree = combatState.CreateCard<SoraNoMusicaCard>(player);
        await AddDrawWindowAsync([upgradedZero, upgradedOneA, upgradedOneB, upgradedThree]);
        BandInvitationCard upgradedInvitation = await CreateAndAutoPlayAsync<BandInvitationCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        Require(
            new[] { upgradedZero, upgradedOneA, upgradedOneB, upgradedThree }
                .All(card => card.Pile?.Type == PileType.Hand),
            "upgraded Band Invitation did not draw through a positive energy total of five");
        Require(upgradedInvitation.Pile?.Type == PileType.Discard,
            "upgraded Band Invitation did not enter Discard");
        await RemoveCombatCardsAsync(
            [upgradedInvitation, upgradedZero, upgradedOneA, upgradedOneB, upgradedThree]);

        CrychicCard baseCrychic = combatState.CreateCard<CrychicCard>(player);
        baseCrychic.EnergyCost.CapturedXValue = 1;
        await CardPileCmd.AddGeneratedCardToCombat(
            baseCrychic,
            PileType.Draw,
            player,
            CardPilePosition.Top);
        await CardCmd.AutoPlay(
            choiceContext,
            baseCrychic,
            null,
            AutoPlayType.Default,
            skipXCapture: true,
            skipCardPileVisuals: true);
        CardModel[] basePhantoms = PileType.Hand.GetPile(player).Cards.ToArray();
        Require(basePhantoms.Length == 1 && IsRareDiagnosticPhantom(basePhantoms[0]),
            "base Crychic did not generate exactly one phantom for captured X=1");
        Require(!basePhantoms[0].IsUpgraded &&
                basePhantoms[0].EnergyCost.GetWithModifiers(CostModifiers.Local) == 0,
            "base Crychic phantom did not preserve base upgrade state and zero-until-played cost");
        Require(baseCrychic.Pile?.Type == PileType.Exhaust,
            "base Crychic did not enter Exhaust");
        await RemoveCombatCardsAsync(basePhantoms.Append(baseCrychic));

        CrychicCard upgradedCrychic = combatState.CreateCard<CrychicCard>(player);
        CardCmd.Upgrade(upgradedCrychic, CardPreviewStyle.None);
        upgradedCrychic.EnergyCost.CapturedXValue = 2;
        await CardPileCmd.AddGeneratedCardToCombat(
            upgradedCrychic,
            PileType.Draw,
            player,
            CardPilePosition.Top);
        await CardCmd.AutoPlay(
            choiceContext,
            upgradedCrychic,
            null,
            AutoPlayType.Default,
            skipXCapture: true,
            skipCardPileVisuals: true);
        CardModel[] upgradedPhantoms = PileType.Hand.GetPile(player).Cards.ToArray();
        Require(upgradedPhantoms.Length == 2 && upgradedPhantoms.All(IsRareDiagnosticPhantom),
            "upgraded Crychic did not generate exactly two phantoms for captured X=2");
        Require(upgradedPhantoms.All(card =>
                card.IsUpgraded && card.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0),
            "upgraded Crychic did not upgrade every generated phantom and make it free until played");
        Require(upgradedCrychic.Pile?.Type == PileType.Exhaust,
            "upgraded Crychic did not enter Exhaust");
        await RemoveCombatCardsAsync(upgradedPhantoms.Append(upgradedCrychic));

        int desiresPlayed = ImprisonedXIICard.CountDesiresPlayed(
            CombatManager.Instance.History.CardPlaysFinished,
            player);
        decimal blockBeforeBaseImprisoned = target.Block;
        int energyBeforeBaseImprisoned = player.PlayerCombatState.Energy;
        ImprisonedXIICard baseImprisoned = await CreateAndAutoPlayAsync<ImprisonedXIICard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: false);
        Require(blockBeforeBaseImprisoned - target.Block == 8m,
            "base Imprisoned XII did not deal eight damage");
        Require(player.PlayerCombatState.Energy - energyBeforeBaseImprisoned == desiresPlayed,
            $"base Imprisoned XII expected {desiresPlayed} energy from Desire history");
        await PlayerCmd.SetEnergy(energyBeforeBaseImprisoned, player);

        decimal blockBeforeUpgradedImprisoned = target.Block;
        int energyBeforeUpgradedImprisoned = player.PlayerCombatState.Energy;
        ImprisonedXIICard upgradedImprisoned = await CreateAndAutoPlayAsync<ImprisonedXIICard>(
            combatState,
            player,
            choiceContext,
            target,
            upgraded: true);
        Require(blockBeforeUpgradedImprisoned - target.Block == 8m &&
                upgradedImprisoned.Keywords.Contains(CardKeyword.Retain),
            "upgraded Imprisoned XII did not preserve eight damage and add Retain");
        Require(player.PlayerCombatState.Energy - energyBeforeUpgradedImprisoned == desiresPlayed,
            $"upgraded Imprisoned XII expected {desiresPlayed} energy from Desire history");
        await PlayerCmd.SetEnergy(energyBeforeUpgradedImprisoned, player);
        cardsToRemove.AddRange([baseImprisoned, upgradedImprisoned]);

        TestCardSelector perfectionSelector = new();
        perfectionSelector.PrepareToSelect([0]);
        PerfectionCard basePerfection;
        using (CardSelectCmd.PushSelector(perfectionSelector))
        {
            basePerfection = await CreateAndAutoPlayAsync<PerfectionCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: false);
        }
        CardModel basePerfectionChoice = PileType.Hand.GetPile(player).Cards.Single();
        Require(basePerfectionChoice.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0 &&
                basePerfection.Pile?.Type == PileType.Exhaust,
            "base Perfection did not add its selected card free this turn and Exhaust");
        await RemoveCombatCardsAsync([basePerfection, basePerfectionChoice]);

        TestCardSelector upgradedPerfectionSelector = new();
        upgradedPerfectionSelector.PrepareToSelect([0]);
        PerfectionCard upgradedPerfection;
        using (CardSelectCmd.PushSelector(upgradedPerfectionSelector))
        {
            upgradedPerfection = await CreateAndAutoPlayAsync<PerfectionCard>(
                combatState,
                player,
                choiceContext,
                null,
                upgraded: true);
        }
        CardModel upgradedPerfectionChoice = PileType.Hand.GetPile(player).Cards.Single();
        Require(upgradedPerfectionChoice.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0 &&
                upgradedPerfection.EnergyCost.GetWithModifiers(CostModifiers.None) == 2 &&
                upgradedPerfection.Pile?.Type == PileType.Exhaust,
            "upgraded Perfection did not preserve free selection, two cost, and Exhaust");
        await RemoveCombatCardsAsync([upgradedPerfection, upgradedPerfectionChoice]);

        CardModel soraFirst = combatState.CreateCard<DefendTogawaSakiko>(player);
        CardModel soraSecond = combatState.CreateCard<DarkHeavenCard>(player);
        CardModel soraThird = combatState.CreateCard<GreetingsCard>(player);
        await AddDrawWindowAsync([soraFirst, soraSecond, soraThird]);
        TestCardSelector soraSelector = new();
        soraSelector.PrepareToSelect([soraThird, soraFirst]);
        decimal blockBeforeSora = target.Block;
        SoraNoMusicaCard sora;
        using (CardSelectCmd.PushSelector(soraSelector))
        {
            sora = await CreateAndAutoPlayAsync<SoraNoMusicaCard>(
                combatState,
                player,
                choiceContext,
                target,
                upgraded: true);
        }
        Require(blockBeforeSora - target.Block == 12m,
            "upgraded Sora no Musica did not deal twelve damage");
        Require(PileType.Draw.GetPile(player).Cards.Take(2).SequenceEqual([soraThird, soraFirst]),
            "Sora no Musica did not preserve the selected top-deck order");
        Require(sora.Pile?.Type == PileType.Exhaust,
            "Sora no Musica did not enter Exhaust");
        await RemoveCombatCardsAsync([sora, soraFirst, soraSecond, soraThird]);

        Require(player.HasOpenPotionSlots,
            "Stay Elegance diagnostic requires one open potion slot");
        PotionModel[] potionsBefore = player.PotionSlots
            .OfType<PotionModel>()
            .ToArray();
        StayEleganceCard stayElegance = await CreateAndAutoPlayAsync<StayEleganceCard>(
            combatState,
            player,
            choiceContext,
            null,
            upgraded: true);
        PotionModel generatedPotion = player.PotionSlots
            .OfType<PotionModel>()
            .Single(potion => !potionsBefore.Any(existing => ReferenceEquals(existing, potion)));
        Require(stayElegance.EnergyCost.GetWithModifiers(CostModifiers.None) == 0 &&
                stayElegance.Pile?.Type == PileType.Exhaust,
            "upgraded Stay Elegance did not procure a potion, cost zero, and Exhaust");
        await PotionCmd.Discard(generatedPotion);
        cardsToRemove.Add(stayElegance);

        await RemoveCombatCardsAsync(cardsToRemove);
        await PlayerCmd.SetEnergy(energyBefore, player);
        await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, player.Creature, playerStrengthBefore);
        await RestoreRememberedPowerAsync<WeakPower>(choiceContext, player.Creature, playerWeakBefore);
        await RestoreRememberedPowerAsync<VulnerablePower>(choiceContext, target, targetVulnerableBefore);
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);
        await RestoreCombatCardsAsync(cardsMovedAside);

        NativeSmokeTrace.N5Info(
            "rare core passed. BandInvitation=4+5, Crychic=X1+X2Upgrade, Imprisoned=Damage8+DesireEnergy, Perfection=FreeSelection, Sora=Damage12+OrderedTop2, StayElegance=PotionProcured.");
    }

    private static bool IsRareDiagnosticPhantom(CardModel card)
    {
        return card is PhantomOfMutsumiCard or
            PhantomOfSakikoCard or
            PhantomOfSoyoCard or
            PhantomOfTakiCard or
            PhantomOfTomoriCard;
    }
}
