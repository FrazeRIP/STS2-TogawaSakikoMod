using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Potions;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class N6BatchDiagnostics
{
    private sealed class Session;

    private sealed class ProbeReward : Reward
    {
        private readonly bool _selectionSucceeds;

        public ProbeReward(Player player, bool selectionSucceeds)
            : base(player)
        {
            _selectionSucceeds = selectionSucceeds;
        }

        protected override RewardType RewardType => RewardType.Gold;

        public override int RewardsSetIndex => 0;

        public override LocString Description => new("rewards", "N6_DIAGNOSTIC_REWARD");

        public override bool IsPopulated => true;

        public override void Populate()
        {
        }

        protected override Task<bool> OnSelect()
        {
            return Task.FromResult(_selectionSucceeds);
        }

        public override void MarkContentAsSeen()
        {
        }
    }

    private static readonly ConditionalWeakTable<CombatState, Session> Sessions = new();

    public static async Task RunInCombatAsync(PlayerChoiceContext choiceContext, CardModel sourceCard)
    {
        if (!NativeSmokeTrace.N6ContractEnabled)
        {
            return;
        }

        CombatState combatState = sourceCard.CombatState as CombatState
            ?? sourceCard.Owner.Creature.CombatState as CombatState
            ?? throw Failure("could not resolve the active combat");
        if (Sessions.TryGetValue(combatState, out _))
        {
            return;
        }
        Sessions.Add(combatState, new Session());

        Player player = sourceCard.Owner;
        CombatRoom room = player.RunState.CurrentRoom as CombatRoom
            ?? throw Failure("the current room was not a CombatRoom");
        Creature target = combatState.GetOpponentsOf(player.Creature)
            .FirstOrDefault(creature => creature.IsHittable)
            ?? throw Failure("the combat had no hittable enemy");

        await VerifyBlazingHairbandAsync(combatState, player, room);
        await VerifyColorfulNotebookAsync(choiceContext, player, room);
        await VerifyPotionsAsync(combatState, choiceContext, player, target);
        await VerifyFountainDrinkAsync(player);
        await VerifyCompassAsync(choiceContext, player, target);
        GoldenPocketWatch watch = await VerifyGoldenPocketWatchAsync(combatState, choiceContext, player);
        TheDoll doll = await VerifyDollAsync(combatState, choiceContext, player);
        TheThirdMovement movement = await VerifyThirdMovementAsync(combatState, choiceContext, player, target, room);
        await VerifyPorcelainCupAsync(player, room);
        MasqueradeMask mask = await VerifyMasqueradeMaskAsync(combatState, choiceContext, player, room);
        CuteAnimalBandAid bandAid = await RelicCmd.Obtain<CuteAnimalBandAid>(player);

        await PrepareAndWriteReloadSaveAsync(player, room, watch, mask, doll, movement);
        await VerifyBandAidRewardOrderingAsync(choiceContext, player, room, bandAid);

        NativeSmokeTrace.N6Info(
            "all relic, potion, hook-order, reward, and save preparation checks passed. Relics=11/11, Potions=6/6.");
    }

    private static async Task VerifyBlazingHairbandAsync(
        CombatState combatState,
        Player player,
        CombatRoom room)
    {
        StarterRelicTogawaSakiko starter = player.Relics.OfType<StarterRelicTogawaSakiko>().Single();
        int deckBefore = player.Deck.Cards.Count;
        HashSet<CardModel> cardsBefore = player.Deck.Cards.ToHashSet();

        BlazingHairband blazing = await RelicCmd.Obtain<BlazingHairband>(player);
        Require(!player.Relics.Contains(starter) && starter.HasBeenRemovedFromState,
            "Blazing Hairband did not remove Monochrome Hairband through RelicCmd");
        Require(player.Relics.OfType<BlazingHairband>().Count() == 1,
            "Blazing Hairband acquisition produced the wrong inventory count");

        await Hook.AfterCombatVictory(player.RunState, combatState, room);
        CardModel[] addedCards = player.Deck.Cards.Where(card => !cardsBefore.Contains(card)).ToArray();
        Require(player.Deck.Cards.Count == deckBefore + 1 && addedCards.Length == 1,
            "Blazing Hairband did not add exactly one persistent card through the victory hook");
        Require(BlazingHairband.IsEligibleRandomCard(addedCards[0]) &&
                addedCards[0] is not CarefreeCard and not WeaknessCard,
            "Blazing Hairband selected a disabled compatibility card");
        Require(player.Character.CardPool.AllCards.Any(card => card.Id == addedCards[0].Id),
            "Blazing Hairband generated a card outside the current character's pool");
        Require(addedCards[0].Type is not CardType.Curse and not CardType.Status,
            "Blazing Hairband added a curse or status to the persistent deck");
        Require(addedCards[0].Rarity != CardRarity.Token && addedCards[0].CanBeGeneratedInCombat,
            "Blazing Hairband added a generated-only card to the persistent deck");

        await Hook.AfterCombatVictory(player.RunState, combatState, room);
        Require(player.Deck.Cards.Count == deckBefore + 1,
            "Blazing Hairband was not idempotent for the same combat identity");

        if (addedCards[0].IsRemovable)
        {
            PersistentDeckRemovalResult cleanup = await PersistentDeckMutation.RemoveAsync(
                addedCards[0],
                showPersistentPreview: false,
                skipCombatVisuals: true);
            Require(cleanup.Success, "Blazing Hairband diagnostic card cleanup failed");
        }

        await RelicCmd.Remove(blazing);
        await RelicCmd.Obtain<StarterRelicTogawaSakiko>(player);
        Require(player.Relics.OfType<BlazingHairband>().Count() == 0 &&
                player.Relics.OfType<StarterRelicTogawaSakiko>().Count() == 1,
            "native relic removal/restoration did not restore the boss-swap baseline");

        NativeSmokeTrace.N6Info(
            $"Blazing Hairband boss swap, random persistent add, disabled-card exclusion, and combat idempotence passed. Added={addedCards[0].Id}.");
    }

    private static async Task VerifyColorfulNotebookAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        CombatRoom room)
    {
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        ColorfulNotebook notebook = await RelicCmd.Obtain<ColorfulNotebook>(player);
        await Hook.AfterRoomEntered(player.RunState, room);
        DazzlingPower dazzling = player.Creature.GetPower<DazzlingPower>()
            ?? throw Failure("Colorful Notebook did not apply Dazzling through AfterRoomEntered");
        Require(dazzling.Amount == 1m, "Colorful Notebook applied the wrong Dazzling amount");
        await PowerCmd.Remove(dazzling);
        await RelicCmd.Remove(notebook);

        NativeSmokeTrace.N6Info("Colorful Notebook combat-entry hook applied exactly 1 Dazzling.");
    }

    private static async Task VerifyPotionsAsync(
        CombatState combatState,
        PlayerChoiceContext choiceContext,
        Player player,
        Creature target)
    {
        await PowerCmd.Remove(player.Creature.GetPower<FreeAttackPower>());
        ChocolateMilkJelly chocolate = await ProcurePotionAsync<ChocolateMilkJelly>(player);
        await UsePotionAsync(chocolate, choiceContext, player);
        Require(player.Creature.GetPower<FreeAttackPower>()?.Amount == 1m,
            "Chocolate Milk Jelly did not apply one Free Attack");
        await PowerCmd.Remove(player.Creature.GetPower<FreeAttackPower>());

        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        EarlGreyTea earlGrey = await ProcurePotionAsync<EarlGreyTea>(player);
        await UsePotionAsync(earlGrey, choiceContext, player);
        Require(player.Creature.GetPower<DazzlingPower>()?.Amount == 2m,
            "Earl Grey Tea did not apply two Dazzling");
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());

        decimal playerBlockBefore = player.Creature.Block;
        FreshlySqueezedCucumber cucumber = await ProcurePotionAsync<FreshlySqueezedCucumber>(player);
        await UsePotionAsync(cucumber, choiceContext, player);
        Require(player.Creature.GetPower<FreshlySqueezedCucumberPower>()?.Amount == 20m,
            "Freshly Squeezed Cucumber did not apply its delayed block power");
        await Hook.AfterPlayerTurnStart(combatState, choiceContext, player);
        Require(player.Creature.GetPower<FreshlySqueezedCucumberPower>() is null &&
                player.Creature.Block == playerBlockBefore + 20m,
            "Freshly Squeezed Cucumber did not gain 20 Block and remove itself at the next player turn start");
        await RemoveAddedBlockAsync(choiceContext, player.Creature, playerBlockBefore);

        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingDownPower>());
        HallucinationPotion hallucination = await ProcurePotionAsync<HallucinationPotion>(player);
        await UsePotionAsync(hallucination, choiceContext, player);
        Require(player.Creature.GetPower<DazzlingPower>()?.Amount == 5m &&
                player.Creature.GetPower<DazzlingDownPower>()?.Amount == 5m,
            "Hallucination Potion did not apply paired five-stack powers");
        await Hook.BeforeSideTurnEnd(combatState, CombatSide.Player, [player.Creature]);
        Require(player.Creature.GetPower<DazzlingPower>() is null &&
                player.Creature.GetPower<DazzlingDownPower>() is null,
            "Hallucination Potion did not remove exactly five temporary Dazzling at owner turn end");

        HashSet<CardModel> handBefore = PileType.Hand.GetPile(player).Cards.ToHashSet();
        MatchaParfait matcha = await ProcurePotionAsync<MatchaParfait>(player);
        await UsePotionAsync(matcha, choiceContext, player);
        MelodyCard generatedMelody = PileType.Hand.GetPile(player).Cards
            .OfType<MelodyCard>()
            .Single(card => !handBefore.Contains(card));
        Require(MatchaParfait.IsDamageInRange(generatedMelody.DynamicVars.Damage.BaseValue) &&
                ReferenceEquals(generatedMelody.Owner, player),
            "Matcha Parfait generated an out-of-range or wrong-owner Melody");
        await CardPileCmd.RemoveFromCombat(generatedMelody, skipVisuals: true);

        await PowerCmd.Remove(player.Creature.GetPower<OneTwoPunchPower>());
        OrangeMilkJelly orange = await ProcurePotionAsync<OrangeMilkJelly>(player);
        await UsePotionAsync(orange, choiceContext, player);
        Require(player.Creature.GetPower<OneTwoPunchPower>()?.Amount == 1m,
            "Orange Milk Jelly did not apply native Double Tap");
        decimal targetBlockBefore = target.Block;
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        MelodyCard doubled = await CreateAndAutoPlayAsync<MelodyCard>(
            combatState,
            player,
            choiceContext,
            target);
        Require(targetBlockBefore + 100m - target.Block == 12m &&
                player.Creature.GetPower<OneTwoPunchPower>() is null,
            "Orange Milk Jelly did not double exactly one Attack and consume itself");
        await CardPileCmd.RemoveFromCombat(doubled, skipVisuals: true);
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);

        Require(new PotionModel[] { chocolate, earlGrey, cucumber, hallucination, matcha, orange }
                .All(potion => potion.HasBeenRemovedFromState && !player.PotionSlots.Contains(potion)),
            "one or more N6 potions was not consumed through the native potion wrapper");

        NativeSmokeTrace.N6Info(
            $"all six potions consumed through native hooks. MatchaDamage={generatedMelody.DynamicVars.Damage.BaseValue}, OrangeAttackDamage=12.");
    }

    private static async Task VerifyFountainDrinkAsync(Player player)
    {
        FountainDrink fountain = await RelicCmd.Obtain<FountainDrink>(player);
        List<PotionModel> fillers = [];
        int guard = 0;
        while (player.HasOpenPotionSlots && guard++ < 20)
        {
            fillers.Add(await ProcurePotionAsync<ChocolateMilkJelly>(player));
        }
        Require(!player.HasOpenPotionSlots && fillers.Count > 0,
            "Fountain Drink diagnostic could not fill the potion belt");
        Require(Hook.ShouldForcePotionReward(player.RunState, player, RoomType.Monster),
            "Fountain Drink did not force a potion reward with full slots");

        await PotionCmd.Discard(fillers[0]);
        fillers.RemoveAt(0);
        Require(player.HasOpenPotionSlots &&
                !Hook.ShouldForcePotionReward(player.RunState, player, RoomType.Monster),
            "Fountain Drink forced a potion reward while a slot was open");

        foreach (PotionModel filler in fillers)
        {
            await PotionCmd.Discard(filler);
        }
        await RelicCmd.Remove(fountain);

        NativeSmokeTrace.N6Info("Fountain Drink native reward hook passed full-slot and open-slot branches.");
    }

    private static async Task VerifyCompassAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        Creature target)
    {
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        await PowerCmd.Remove(target.GetPower<DazzlingPower>());
        TheCompass compass = await RelicCmd.Obtain<TheCompass>(player);

        await compass.BeforeSideTurnEnd(choiceContext, CombatSide.Enemy, [target]);
        Require(player.Creature.GetPower<DazzlingPower>() is null && target.GetPower<DazzlingPower>() is null,
            "The Compass responded to the wrong side's participant list");

        await compass.BeforeSideTurnEnd(choiceContext, CombatSide.Player, [player.Creature]);
        Require(player.Creature.GetPower<DazzlingPower>()?.Amount == 2m &&
                target.GetPower<DazzlingPower>()?.Amount == 1m,
            "The Compass did not apply 2 self and 1 enemy Dazzling");

        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        await PowerCmd.Remove(target.GetPower<DazzlingPower>());
        await RelicCmd.Remove(compass);
        NativeSmokeTrace.N6Info("The Compass owner-side participant hook applied 2 self and 1 enemy Dazzling.");
    }

    private static async Task<GoldenPocketWatch> VerifyGoldenPocketWatchAsync(
        CombatState combatState,
        PlayerChoiceContext choiceContext,
        Player player)
    {
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        GoldenPocketWatch watch = await RelicCmd.Obtain<GoldenPocketWatch>(player);
        watch.CardsPlayed = 11;
        decimal blockBefore = player.Creature.Block;
        DefendTogawaSakiko trigger = await CreateAndAutoPlayAsync<DefendTogawaSakiko>(
            combatState,
            player,
            choiceContext,
            null);
        Require(watch.CardsPlayed == 0 && player.Creature.GetPower<StrengthPower>()?.Amount == 2m,
            "Golden Pocket Watch did not carry its counter into a 12-card Strength trigger");
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        await RemoveAddedBlockAsync(choiceContext, player.Creature, blockBefore);
        await CardPileCmd.RemoveFromCombat(trigger, skipVisuals: true);

        NativeSmokeTrace.N6Info("Golden Pocket Watch persisted its count and triggered 2 Strength on card 12.");
        return watch;
    }

    private static async Task<TheDoll> VerifyDollAsync(
        CombatState combatState,
        PlayerChoiceContext choiceContext,
        Player player)
    {
        await PowerCmd.Remove(player.Creature.GetPower<HypePower>());
        TheDoll doll = await RelicCmd.Obtain<TheDoll>(player);
        await Hook.BeforeCombatStart(player.RunState, combatState);
        Require(doll.TurnsElapsed == 0, "The Doll did not reset before combat start");

        await Hook.AfterPlayerTurnStart(combatState, choiceContext, player);
        Require(doll.TurnsElapsed == 1 && player.Creature.GetPower<HypePower>() is null,
            "The Doll triggered before the second player turn");
        await Hook.AfterPlayerTurnStart(combatState, choiceContext, player);
        Require(doll.TurnsElapsed == -1 && player.Creature.GetPower<HypePower>()?.Amount == 2m,
            "The Doll did not apply two Hype on the second player turn");
        await Hook.AfterPlayerTurnStart(combatState, choiceContext, player);
        Require(player.Creature.GetPower<HypePower>()?.Amount == 2m,
            "The Doll triggered more than once in one combat");
        await PowerCmd.Remove(player.Creature.GetPower<HypePower>());

        NativeSmokeTrace.N6Info("The Doll triggered exactly once for 2 Hype on player turn 2.");
        return doll;
    }

    private static async Task<TheThirdMovement> VerifyThirdMovementAsync(
        CombatState combatState,
        PlayerChoiceContext choiceContext,
        Player player,
        Creature target,
        CombatRoom room)
    {
        await PowerCmd.Remove(player.Creature.GetPower<StrengthPower>());
        await PowerCmd.Remove(target.GetPower<VulnerablePower>());
        TheThirdMovement movement = await RelicCmd.Obtain<TheThirdMovement>(player);
        decimal targetBlockBefore = target.Block;
        await CreatureCmd.GainBlock(target, 300m, ValueProp.Unpowered, null, fast: true);
        decimal[] expectedDamage = [45m, 45m, 45m, 15m];

        for (int index = 0; index < expectedDamage.Length; index++)
        {
            decimal blockBeforePlay = target.Block;
            TheMoonlightSonataCard moonlight = await CreateAndAutoPlayAsync<TheMoonlightSonataCard>(
                combatState,
                player,
                choiceContext,
                target);
            Require(blockBeforePlay - target.Block == expectedDamage[index],
                $"The Third Movement play {index + 1} dealt {blockBeforePlay - target.Block} instead of {expectedDamage[index]}");
            Require(movement.RemainingUses == Math.Max(0, 2 - index),
                $"The Third Movement counter was wrong after play {index + 1}");
            await CardPileCmd.RemoveFromCombat(moonlight, skipVisuals: true);
        }

        Require(movement.IsUsedUp && movement.Status == MegaCrit.Sts2.Core.Entities.Relics.RelicStatus.Disabled,
            "The Third Movement did not enter its used-up presentation state");
        Require(room.ExtraRewards.TryGetValue(player, out List<Reward>? rewards) &&
                rewards.Count(reward => reward is CardRemovalReward) >= 4,
            "Moonlight Sonata did not preserve its native purge-reward side effect during The Third Movement test");
        await RemoveAddedBlockAsync(choiceContext, target, targetBlockBefore);

        NativeSmokeTrace.N6Info("The Third Movement dealt 45/45/45/15 damage and consumed exactly three uses.");
        return movement;
    }

    private static async Task VerifyPorcelainCupAsync(Player player, CombatRoom room)
    {
        WarmthInfusedPorcelainCup cup = await RelicCmd.Obtain<WarmthInfusedPorcelainCup>(player);
        StarterRelicTogawaSakiko starter = player.Relics.OfType<StarterRelicTogawaSakiko>().Single();
        starter.SetKingsRewardPending(true);

        List<CardCreationResult> options =
        [
            new(player.RunState.CreateCard(ModelDb.Card<StrikeTogawaSakiko>(), player)),
            new(player.RunState.CreateCard(ModelDb.Card<DefendTogawaSakiko>(), player)),
            new(player.RunState.CreateCard(ModelDb.Card<MelodyCard>(), player))
        ];
        CardCreationOptions creationOptions = CardCreationOptions.ForRoom(player, room.RoomType)
            .WithFlags(CardCreationFlags.IsCardReward | CardCreationFlags.IsFromCombat)
            .WithRngOverride(CreateThresholdPassingRng());
        bool modified = Hook.TryModifyCardRewardOptions(
            player.RunState,
            player,
            options,
            creationOptions,
            out List<AbstractModel> modifiers);

        Require(modified && options.Count == 2,
            "Porcelain Cup changed the Kings-reduced reward count");
        Require(options.Count(option => option.Card is HeartsBarrierCard) == 1 && modifiers.Contains(cup),
            "Porcelain Cup did not force exactly one Heart's Barrier through the native reward hook");
        starter.SetKingsRewardPending(false);
        await RelicCmd.Remove(cup);

        NativeSmokeTrace.N6Info("Porcelain Cup forced one Heart's Barrier while preserving the Kings two-card reward.");
    }

    private static async Task<MasqueradeMask> VerifyMasqueradeMaskAsync(
        CombatState combatState,
        PlayerChoiceContext choiceContext,
        Player player,
        CombatRoom room)
    {
        MasqueradeMask mask = await RelicCmd.Obtain<MasqueradeMask>(player);
        mask.PurgedCards = 2;
        int rewardsBefore = GetCardRemovalRewardCount(room, player);
        DefendTogawaSakiko removable = combatState.CreateCard<DefendTogawaSakiko>(player);
        await CardPileCmd.AddGeneratedCardToCombat(removable, PileType.Hand, player);
        SakikoPurgeResult success = await SakikoPurgeCommand.RemoveAsync(
            removable,
            showPersistentPreview: false,
            skipCombatVisuals: true,
            choiceContext: choiceContext);
        Require(success.Success && success.MasqueradeRewardsAdded == 1 &&
                mask.PurgedCards == 0 &&
                GetCardRemovalRewardCount(room, player) == rewardsBefore + 1,
            "Masquerade Mask did not add one native purge reward on the third successful purge");

        AscendersBane eternal = combatState.CreateCard<AscendersBane>(player);
        await CardPileCmd.AddGeneratedCardToCombat(eternal, PileType.Hand, player);
        int rewardsBeforeFailure = GetCardRemovalRewardCount(room, player);
        SakikoPurgeResult prevented = await SakikoPurgeCommand.RemoveAsync(
            eternal,
            showPersistentPreview: false,
            skipCombatVisuals: true,
            choiceContext: choiceContext);
        Require(!prevented.Success && prevented.Prevented && mask.PurgedCards == 0 &&
                GetCardRemovalRewardCount(room, player) == rewardsBeforeFailure,
            "Masquerade Mask counted a prevented purge");
        await CardPileCmd.RemoveFromCombat(eternal, skipVisuals: true);

        NativeSmokeTrace.N6Info("Masquerade Mask counted only successful purges and added a native reward at 3.");
        return mask;
    }

    private static async Task PrepareAndWriteReloadSaveAsync(
        Player player,
        CombatRoom room,
        GoldenPocketWatch watch,
        MasqueradeMask mask,
        TheDoll doll,
        TheThirdMovement movement)
    {
        watch.CardsPlayed = 7;
        mask.PurgedCards = 2;
        doll.TurnsElapsed = 1;
        movement.RemainingUses = 1;

        if (!player.Deck.Cards.Any(card => card is DesireCard))
        {
            CardPileAddResult desire = await PersistentDeckMutation.AddCanonicalAsync<DesireCard>(
                player,
                skipVisuals: true);
            Require(desire.success, "the N6 reload save could not add its stable Desire probe");
        }

        EarlGreyTea savedPotion = await ProcurePotionAsync<EarlGreyTea>(player);
        Require(!savedPotion.HasBeenRemovedFromState, "the saved potion was removed before serialization");

        await SaveManager.Instance.SaveRun(room, saveProgress: false);
        NativeSmokeTrace.N6Info(
            "native mid-combat save written with Watch=7, Mask=2, Doll=1, ThirdMovement=1, EarlGrey=1, and purge rewards.");
    }

    private static async Task VerifyBandAidRewardOrderingAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        CombatRoom room,
        CuteAnimalBandAid bandAid)
    {
        await CreatureCmd.Heal(player.Creature, player.Creature.MaxHp, playAnim: false);
        await CreatureCmd.Damage(
            choiceContext,
            player.Creature,
            3m,
            ValueProp.Unblockable | ValueProp.Unpowered,
            player.Creature);
        decimal damagedHp = player.Creature.CurrentHp;

        ProbeReward beforeVictory = new(player, selectionSucceeds: true);
        Require(await beforeVictory.SelectUnsynchronized() && beforeVictory.SuccessfullySelected,
            "the pre-victory success probe was not selected");
        Require(player.Creature.CurrentHp == damagedHp,
            "Cute Animal Band-Aid healed before the combat room became pre-finished");

        room.MarkPreFinished();
        ProbeReward failed = new(player, selectionSucceeds: false);
        Require(!await failed.SelectUnsynchronized() && !failed.SuccessfullySelected &&
                player.Creature.CurrentHp == damagedHp,
            "Cute Animal Band-Aid healed for a failed reward selection");

        ProbeReward successful = new(player, selectionSucceeds: true);
        Require(await successful.SelectUnsynchronized() && successful.SuccessfullySelected &&
                player.Creature.CurrentHp == damagedHp + 1m,
            "Cute Animal Band-Aid did not heal exactly once after a successful post-combat reward");
        Require(player.Relics.Contains(bandAid), "Cute Animal Band-Aid was lost during its reward hook");

        NativeSmokeTrace.N6Info(
            "Cute Animal Band-Aid healed exactly 1 only after successful post-combat reward selection.");
    }

    private static async Task<TPotion> ProcurePotionAsync<TPotion>(Player player)
        where TPotion : PotionModel
    {
        PotionProcureResult result = await PotionCmd.TryToProcure<TPotion>(player);
        Require(result.success && result.potion is TPotion,
            $"native potion procurement failed for {typeof(TPotion).Name}: {result.failureReason}");
        return (TPotion)result.potion;
    }

    private static async Task UsePotionAsync(
        PotionModel potion,
        PlayerChoiceContext choiceContext,
        Player player)
    {
        Require(ReferenceEquals(potion.Owner, player) && player.PotionSlots.Contains(potion),
            $"{potion.Id} did not enter its owner's potion belt");
        await potion.OnUseWrapper(choiceContext, player.Creature);
        Require(potion.HasBeenRemovedFromState && !player.PotionSlots.Contains(potion),
            $"{potion.Id} was not consumed before its effect resolved");
    }

    private static async Task<TCard> CreateAndAutoPlayAsync<TCard>(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext,
        Creature? target)
        where TCard : CardModel
    {
        TCard card = combatState.CreateCard<TCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, player, CardPilePosition.Top);
        await CardCmd.AutoPlay(
            choiceContext,
            card,
            target,
            AutoPlayType.Default,
            skipCardPileVisuals: true);
        return card;
    }

    private static int GetCardRemovalRewardCount(CombatRoom room, Player player)
    {
        return room.ExtraRewards.TryGetValue(player, out List<Reward>? rewards)
            ? rewards.Count(reward => reward is CardRemovalReward)
            : 0;
    }

    private static Rng CreateThresholdPassingRng()
    {
        for (ulong seed = 0; seed < 10_000; seed++)
        {
            Rng probe = new(seed);
            if (probe.NextFloat() >= WarmthInfusedPorcelainCup.TriggerThreshold)
            {
                return new Rng(seed);
            }
        }

        throw Failure("could not construct a deterministic threshold-passing reward RNG");
    }

    private static async Task RemoveAddedBlockAsync(
        PlayerChoiceContext choiceContext,
        Creature creature,
        decimal amountBefore)
    {
        decimal addedBlock = creature.Block - amountBefore;
        if (addedBlock > 0m)
        {
            await CreatureCmd.LoseBlock(choiceContext, creature, addedBlock, null);
        }
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
        return new InvalidOperationException("Phase N6 actual-game contract failed: " + message + ".");
    }
}
