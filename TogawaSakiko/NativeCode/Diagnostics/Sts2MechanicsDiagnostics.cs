using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using static TogawaSakiko.NativeCode.Diagnostics.MechanicsTestSupport;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class Sts2MechanicsDiagnostics
{
    public static bool Enabled => CommandLineHelper.HasArg(NativeSmokeTrace.Sts2MechanicsArgument);
    private static bool _started;
    private static int _assertions;
    private static void Require(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new InvalidOperationException("STS2 mechanics: " + message);
    }

    private sealed class Selector(CardModel? selected) : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            Require(options.All(SakikoPurgeCommand.IsEligible), "selection contains a non-deck card");
            if (selected is null)
            {
                Require(minSelect == 0, "optional selection cannot be skipped");
                return Task.FromResult<IEnumerable<CardModel>>([]);
            }
            Require(options.Contains(selected), "permanent card missing from selection");
            return Task.FromResult<IEnumerable<CardModel>>([selected]);
        }
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<CardRewardAlternative> alternatives) => new() { card = options.FirstOrDefault()?.Card };
    }

    public static async Task RunAsync(PlayerChoiceContext context, CardModel source)
    {
        if (!Enabled || _started) return;
        _started = true;
        var owner = source.Owner;
        var combat = owner.Creature.CombatState!;
        var enemy = combat.GetOpponentsOf(owner.Creature).First(c => c.IsHittable);
        var hairband = owner.Relics.OfType<TogawaSakiko.NativeCode.Models.Relics.StarterRelicTogawaSakiko>().Single();
        var startupDesires = owner.Deck.Cards.OfType<DesireCard>().ToArray();
        Require(startupDesires.Length == 1 &&
                PersistentDeckMutation.SnapshotLinkedCombatCopies(startupDesires[0]).Count == 1,
            "Hairband did not add one permanent Desire and its combat instance at combat start");
        int beforeRepeatedStart = owner.Deck.Cards.Count;
        await hairband.BeforeCombatStart();
        Require(owner.Deck.Cards.Count == beforeRepeatedStart, "Hairband duplicated its combat-start addition");
        foreach (var relic in owner.Relics.ToArray()) await RelicCmd.Remove(relic);
        foreach (var power in owner.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        foreach (var power in enemy.Powers.ToArray()) await PowerCmd.Remove(power);
        // A lethal scenario ("phantom:<member>" or "element:<element>") replaces the regular suite.
        string? lethal = CommandLineHelper.GetValue(NativeSmokeTrace.Sts2MechanicsLethalArgument);
        if (!string.IsNullOrEmpty(lethal))
        {
            string[] parts = lethal.Split(':', 2);
            Task scenario = parts switch
            {
                ["phantom", string member] => BatchNineMechanicsTests.RunLethalAsync(context, source, member, Require),
                ["element", string element] => BatchThirteenMechanicsTests.RunLethalAsync(context, source, element, Require),
                _ => throw new InvalidOperationException("STS2 mechanics: unknown lethal scenario " + lethal)
            };
            await scenario;
            NativeSmokeTrace.Info($"STS2 mechanics: PASS ({_assertions} assertions)");
            return;
        }
        // Empty Hand to exercise normal placement without the native hand-cap fallback.
        foreach (var card in PileType.Hand.GetPile(owner).Cards.ToArray())
            if (card != source) await CardPileCmd.Add(card, PileType.Discard);

        var spring = combat.CreateCard<SpringSunlightCard>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(spring, PileType.Hand, owner);
        spring.EnergyCost.SetUntilPlayed(0);
        SpringSunlightCard.RefreshCombatCosts(owner);
        Require(spring.EnergyCost.GetWithModifiers(CostModifiers.None) == owner.Deck.Cards.Count / 6 &&
                spring.EnergyCost.GetWithModifiers(CostModifiers.All) == 0,
            "Spring Sunlight base refresh overwrote zero-cost modifier");
        var opponents = combat.GetOpponentsOf(owner.Creature).Where(c => c.IsHittable).ToArray();
        foreach (var opponent in opponents)
            await CreatureCmd.GainBlock(opponent, 1000m, ValueProp.Unpowered, null, fast: true);
        var blocks = opponents.Select(c => c.Block).ToArray();
        await spring.OnPlayWrapper(context, null, isAutoPlay: false, Zero);
        Require(opponents.Select((c, i) => blocks[i] - c.Block).All(damage => damage == 30m),
            "Spring Sunlight did not damage every opponent without a selected target");
        var moon = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<TheMoonlightSonataCard>(
            owner, skipPersistentVisuals: true);
        decimal blockBeforeMoon = enemy.Block;
        using (CardSelectCmd.PushSelector(new Selector(moon.PersistentCard!)))
            await moon.CombatCard!.OnPlayWrapper(context, enemy, isAutoPlay: false, Zero);
        Require(!owner.Deck.Cards.Contains(moon.PersistentCard!) && moon.CombatCard!.HasBeenRemovedFromState &&
                blockBeforeMoon - enemy.Block == 15m,
            "Moonlight Sonata could not remove itself and finish its attack");
        foreach (var opponent in opponents) await CreatureCmd.LoseBlock(context, opponent, opponent.Block, null);

        int deckSize = owner.Deck.Cards.Count;
        var first = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<DesireCard>(
            owner, skipPersistentVisuals: true, upgradeLevel: 1);
        Require(first.Success && owner.Deck.Cards.Count == deckSize + 1, "permanent addition failed");
        Require(first.CombatCard!.Pile?.Type == PileType.Hand, "added card did not enter Hand");
        Require(first.CombatCard.IsUpgraded && ReferenceEquals(first.CombatCard.DeckVersion, first.PersistentCard),
            "upgrade or permanent identity lost");
        Require(PersistentDeckMutation.SnapshotLinkedCombatCopies(first.PersistentCard!).Count == 1,
            "addition produced duplicate combat copies");

        var second = await PersistentDeckMutation.AddStatEquivalentAsync(owner, first.PersistentCard!, skipVisuals: true);
        var secondCopy = PersistentDeckMutation.SnapshotLinkedCombatCopies(second.cardAdded).Single();
        Require(secondCopy.Pile?.Type == PileType.Hand && secondCopy.IsUpgraded, "copy addition did not enter Hand");

        var temporary = combat.CreateCard<DesireCard>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(temporary, PileType.Hand, owner);
        Require(owner.Deck.Cards.Count == deckSize + 2, "temporary generation changed permanent deck");
        var rejected = await SakikoPurgeCommand.RemoveAsync(temporary);
        Require(!rejected.Success && rejected.Prevented && temporary.Pile?.Type == PileType.Hand,
            "temporary card was purged");
        Require(!SakikoPurgeCommand.GetCandidates(owner).Contains(temporary), "temporary card is selectable");

        // Verify exact identity, including copies in every combat pile and an identical independent card.
        foreach (var pile in new[] { PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Play })
        {
            var copy = combat.CloneCard(first.PersistentCard!);
            copy.DeckVersion = first.PersistentCard;
            await CardPileCmd.AddGeneratedCardToCombat(copy, pile, owner);
        }
        var linked = PersistentDeckMutation.SnapshotLinkedCombatCopies(first.PersistentCard!);
        Require(linked.Count == 5, "five-pile fixture incomplete");
        var locations = TogawaSakiko.NativeCode.Presentation.PurgeCardLocationTips.GetLocations(first.PersistentCard!);
        Require(locations.Length == 5 && new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Play }
                .All(locations.Contains), "purge location tips omitted a linked pile");
        using (TogawaSakiko.NativeCode.Presentation.PurgeCardLocationTips.ShowFor([first.PersistentCard!]))
        {
            Require(TogawaSakiko.NativeCode.Presentation.PurgeCardLocationTips.IsVisibleFor(first.PersistentCard!) &&
                    !TogawaSakiko.NativeCode.Presentation.PurgeCardLocationTips.IsVisibleFor(second.cardAdded),
                "location tip scope confused identical permanent cards");
        }
        Require(!TogawaSakiko.NativeCode.Presentation.PurgeCardLocationTips.IsVisibleFor(first.PersistentCard!),
            "location tip leaked after selection scope ended");
        using (CardSelectCmd.PushSelector(new Selector(first.PersistentCard!)))
            await WishFulfilledCard.ChooseAndPurgeAsync(context, owner,
                new MegaCrit.Sts2.Core.Localization.LocString("cards", "TOGAWASAKIKO-WISH_FULFILLED_CARD.selectionScreenPrompt"));
        Require(!owner.Deck.Cards.Contains(first.PersistentCard!) && linked.All(c => c.HasBeenRemovedFromState),
            "purge failed to remove permanent card and all linked instances");
        Require(TogawaSakiko.NativeCode.Presentation.PurgeCardLocationTips.GetLocations(first.PersistentCard!).Length == 0,
            "location tips retained removed combat instances");
        Require(owner.Deck.Cards.Contains(second.cardAdded) && secondCopy.Pile?.Type == PileType.Hand &&
            temporary.Pile?.Type == PileType.Hand, "purge removed unrelated same-name cards");

        second.cardAdded.AddKeyword(CardKeyword.Eternal);
        Require(!SakikoPurgeCommand.GetCandidates(owner).Contains(second.cardAdded), "protected card is selectable");
        Require((await SakikoPurgeCommand.RemoveAsync(secondCopy)).Prevented,
            "protected permanent card was purged via combat copy");
        second.cardAdded.RemoveKeyword(CardKeyword.Eternal);

        var ardor = (await PowerCmd.Apply<ArdorPower>(context, owner.Creature, 2, owner.Creature, source))!;
        await PowerCmd.Apply<ArdorPower>(context, owner.Creature, 1, owner.Creature, source);
        Require(ardor.Amount == 3, "Ardor did not stack");
        await CreatureCmd.GainBlock(enemy, 10000, ValueProp.Unpowered, null, fast: true);
        await CreatureCmd.GainBlock(owner.Creature, 10000, ValueProp.Unpowered, null, fast: true);
        for (int hit = 0; hit < 3; hit++)
        {
            int before = enemy.Block;
            await CreatureCmd.Damage(context, enemy, 5, ValueProp.Move, owner.Creature);
            Require(enemy.Block == before - 8, "outgoing attack did not gain 3 per hit");
        }
        int block = owner.Creature.Block;
        await CreatureCmd.Damage(context, owner.Creature, 5, ValueProp.Move, enemy);
        Require(owner.Creature.Block == block - 8, "incoming attack did not gain 3");
        block = enemy.Block;
        await CreatureCmd.Damage(context, enemy, 5, ValueProp.Unpowered, owner.Creature);
        Require(enemy.Block == block - 5, "Ardor changed non-attack damage");
        var enemyArdor = (await PowerCmd.Apply<ArdorPower>(context, enemy, 2, enemy, null))!;
        block = enemy.Block;
        await CreatureCmd.Damage(context, enemy, 5, ValueProp.Move, owner.Creature);
        Require(enemy.Block == block - 10, "both participants' Ardor did not combine");
        await ardor.AfterSideTurnEnd(context, CombatSide.Player, [owner.Creature]);
        Require(ardor.Amount == 3 && owner.Creature.HasPower<ArdorPower>(), "Ardor expired or was consumed");
        await PowerCmd.Remove(ardor);
        await PowerCmd.Remove(enemyArdor);
        await VerifyBatchTwoAsync(context, source);
        await VerifyBatchThreeAsync(context, source);
        await VerifyBatchFiveAsync(context, source);
        await VerifyBatchSixAsync(context, source);
        await VerifyBatchSevenAsync(context, source);
        await BatchEightMechanicsTests.RunAsync(context, source, Require);
        await BatchNineMechanicsTests.RunAsync(context, source, Require);
        await BatchTenMechanicsTests.RunAsync(context, source, Require);
        await BatchElevenMechanicsTests.RunAsync(context, source, Require);
        await BatchTwelveMechanicsTests.RunAsync(context, source, Require);
        await BatchThirteenMechanicsTests.RunAsync(context, source, Require);
        await BatchFourteenMechanicsTests.RunAsync(context, source, Require);
        await BatchFifteenMechanicsTests.RunAsync(context, source, Require);
        await BatchSixteenMechanicsTests.RunAsync(context, source, Require);
        await KindnessInteractionTests.RunAsync(context, source, Require);
        await SuccubusRestorationTests.RunAsync(context, source, Require);
        await VerifyBatchFourAsync(context, source);
        NativeSmokeTrace.Info($"STS2 mechanics: PASS ({_assertions} assertions)");
    }

    private static async Task VerifyBatchSevenAsync(PlayerChoiceContext context, CardModel source)
    {
        var owner = source.Owner;
        var creature = owner.Creature;
        var combat = creature.CombatState!;
        foreach (var power in creature.Powers.ToArray()) await PowerCmd.Remove(power);
        foreach (var card in PileType.Hand.GetPile(owner).Cards.ToArray()) await CardPileCmd.Add(card, PileType.Discard);
        var zero = Zero;
        var opponents = combat.GetOpponentsOf(creature).Where(c => c.IsHittable).ToArray();
        foreach (var enemy in opponents)
            await CreatureCmd.GainBlock(enemy, 1000, ValueProp.Unpowered, null, fast: true);
        foreach (bool upgraded in new[] { false, true })
        {
            int added = upgraded ? 5 : 4;
            var girl = combat.CreateCard<TheGirlWithFlaxenHairCard>(owner);
            if (upgraded) CardCmd.Upgrade(girl, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(girl, PileType.Hand, owner);
            var blocks = opponents.Select(c => c.Block).ToArray();
            await girl.OnPlayWrapper(context, null, false, zero);
            Require(opponents.Select((enemy, index) => blocks[index] - enemy.Block).All(x => x == (upgraded ? 10 : 8)),
                "Flaxen Hair AoE damage incorrect or new Dazzling triggered itself");
            Require(creature.GetPower<DazzlingPower>()?.Amount == added && creature.GetPower<DazzlingDownPower>()?.Amount == added,
                "Flaxen Hair temporary Dazzling incorrect");
            await PowerCmd.Apply<DazzlingPower>(context, creature, 2, creature, source);
            await creature.GetPower<DazzlingDownPower>()!.BeforeSideTurnEnd(context, CombatSide.Player, [creature]);
            Require(creature.GetPower<DazzlingPower>()?.Amount == 2 && !creature.HasPower<DazzlingDownPower>(),
                "Temporary Dazzling removed permanent stacks or failed to clear expiry");
            await PowerCmd.Remove(creature.GetPower<DazzlingPower>()!);
        }
        var first = combat.CreateCard<TheGirlWithFlaxenHairCard>(owner);
        var second = combat.CreateCard<TheGirlWithFlaxenHairCard>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(first, PileType.Hand, owner);
        await CardPileCmd.AddGeneratedCardToCombat(second, PileType.Hand, owner);
        await first.OnPlayWrapper(context, null, false, zero);
        await second.OnPlayWrapper(context, null, false, zero);
        Require(creature.GetPower<DazzlingPower>()?.Amount == 8 && creature.GetPower<DazzlingDownPower>()?.Amount == 8,
            "Temporary Dazzling did not accumulate");
        await creature.GetPower<DazzlingDownPower>()!.BeforeSideTurnEnd(context, CombatSide.Player, [creature]);
        Require(!creature.HasPower<DazzlingPower>() && !creature.HasPower<DazzlingDownPower>(), "Accumulated Dazzling did not expire together");

        foreach (bool upgraded in new[] { false, true })
        {
            var dream = combat.CreateCard<ForeverDreamCard>(owner);
            if (upgraded) CardCmd.Upgrade(dream, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(dream, PileType.Hand, owner);
            await dream.OnPlayWrapper(context, null, false, zero);
            Require(creature.GetPower<HypePower>()?.Amount == (upgraded ? 5 : 4) && creature.HasPower<ForeverDreamPower>(),
                "Forever Dream did not grant Hype and protection");
            await PowerCmd.Remove(creature.GetPower<HypePower>()!);
            await PowerCmd.Remove(creature.GetPower<ForeverDreamPower>()!);
        }
        var protection = await PowerCmd.Apply<ForeverDreamPower>(context, creature, 1, creature, source);
        var hype = await PowerCmd.Apply<HypePower>(context, creature, 4, creature, source);
        var dazzling = await PowerCmd.Apply<DazzlingPower>(context, creature, 5, creature, source);
        var strength = await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>(context, creature, 3, creature, source);
        int result = await PowerCmd.ModifyAmount(context, dazzling!, -4, creature, source);
        Require(dazzling!.Amount == 5 && hype!.Amount == 3 && result == 5, "Dream partial reduction or ModifyAmount return contract incorrect");
        await PowerCmd.Remove(strength!);
        Require(creature.GetPower<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>() == strength && strength!.Amount == 3 && hype!.Amount == 2,
            "Dream direct removal or per-type payment incorrect");
        await PowerCmd.Decrement(dazzling);
        Require(dazzling.Amount == 5 && hype!.Amount == 1, "Dream did not protect decrement with one payment");
        var down = await PowerCmd.Apply<DazzlingDownPower>(context, creature, 5, creature, source);
        await down!.BeforeSideTurnEnd(context, CombatSide.Player, [creature]);
        Require(dazzling.Amount == 5 && !creature.HasPower<HypePower>() && !creature.HasPower<DazzlingDownPower>(),
            "Dream failed to retain temporary Dazzling or protected its Debuff expiry marker");
        await PowerCmd.ModifyAmount(context, dazzling, -2, creature, source);
        Require(dazzling.Amount == 3, "Dream prevented loss without Hype");
        await PowerCmd.Remove(strength!);
        Require(!creature.HasPower<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>(), "Dream prevented direct removal without Hype");
        await PowerCmd.Remove(dazzling);
        // Removing a Debuff must not consume Hype.
        hype = await PowerCmd.Apply<HypePower>(context, creature, 2, creature, source);
        var weak = await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.WeakPower>(context, creature, 2, creature, source);
        await PowerCmd.Remove(weak!);
        Require(hype!.Amount == 2 && !creature.HasPower<MegaCrit.Sts2.Core.Models.Powers.WeakPower>(), "Dream protected a Debuff");
        // Mantra payment is a real stack reduction. Prevent it without undoing the stance entry.
        await PowerCmd.Apply<MelodiaPower>(context, creature, 10, creature, source);
        Require(creature.GetPower<MelodiaPower>()?.Amount == 10 && creature.HasPower<MonsterDivinityPower>() && hype.Amount == 1,
            "Dream failed to replace Mantra consumption or prevented Divinity entry");
        await PowerCmd.Decrement(hype);
        Require(!creature.HasPower<HypePower>(), "Dream tried to protect Hype itself");
        await PowerCmd.Remove(protection!);
        foreach (var power in creature.Powers.ToArray()) await PowerCmd.Remove(power);

        // Existing Curiosity behavior: first copy grants no Strength; second uses the old stack count.
        for (int play = 0; play < 2; play++)
        {
            var curiosity = combat.CreateCard<CuriosityCard>(owner);
            await CardPileCmd.AddGeneratedCardToCombat(curiosity, PileType.Hand, owner);
            await curiosity.OnPlayWrapper(context, null, false, zero);
            Require((creature.GetPower<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>()?.Amount ?? 0) == play,
                "Curiosity counted its newly applied stacks on the same card");
        }
        var later = combat.CreateCard<ForeverDreamCard>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(later, PileType.Hand, owner);
        await later.OnPlayWrapper(context, null, false, zero);
        Require(creature.GetPower<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>()?.Amount == 3,
            "Stacked Curiosity failed to grant 2 Strength for a later Power card");
        await PowerCmd.Remove(creature.GetPower<HypePower>()!);
        await PowerCmd.Remove(creature.GetPower<ForeverDreamPower>()!);
        foreach (var power in creature.Powers.ToArray()) await PowerCmd.Remove(power);
    }

    private sealed class BatchSelector(CardModel[] selected, int expectedMax, bool cursesOnly = false) : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            var pool = options.ToArray();
            Require(minSelect == 0 && maxSelect == expectedMax, "batch 6 optional selection bounds");
            Require(pool.All(SakikoPurgeCommand.IsEligible), "batch 6 selection contains temporary cards");
            Require(!cursesOnly || pool.All(c => c.Type == CardType.Curse), "Perdere offered a non-Curse");
            Require(selected.All(pool.Contains), "batch 6 selected permanent card missing");
            return Task.FromResult<IEnumerable<CardModel>>(selected);
        }
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<CardRewardAlternative> alternatives) => new() { card = options.FirstOrDefault()?.Card };
    }

    private static async Task VerifyBatchSixAsync(PlayerChoiceContext context, CardModel source)
    {
        var owner = source.Owner;
        var combat = owner.Creature.CombatState!;
        foreach (var power in owner.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        foreach (var card in PileType.Hand.GetPile(owner).Cards.ToArray()) await CardPileCmd.Add(card, PileType.Discard);
        var zero = Zero;
        foreach (bool upgraded in new[] { false, true })
        {
            int count = upgraded ? 3 : 2;
            var curses = new List<CardModel>();
            var copies = new List<CardModel>();
            for (int i = 0; i < count; i++)
            {
                var pair = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<AmorisCard>(owner, skipPersistentVisuals: true);
                curses.Add(pair.PersistentCard!); copies.Add(pair.CombatCard!);
            }
            var perdere = combat.CreateCard<PerdereOmniaCard>(owner);
            if (upgraded) CardCmd.Upgrade(perdere, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(perdere, PileType.Hand, owner);
            using (CardSelectCmd.PushSelector(new BatchSelector(curses.ToArray(), count, true)))
                await perdere.OnPlayWrapper(context, null, false, zero);
            Require(curses.All(c => c.HasBeenRemovedFromState) && copies.All(c => c.HasBeenRemovedFromState),
                "Perdere failed to remove selected permanent Curses and combat copies");

            var wishPair = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<WishFulfilledCard>(owner, skipPersistentVisuals: true);
            var wish = wishPair.CombatCard!;
            if (upgraded) CardCmd.Upgrade(wish, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
            var targets = new List<CardModel> { wishPair.PersistentCard! };
            for (int i = 1; i < count; i++) targets.Add((await PersistentDeckMutation.AddCanonicalAsync<DefendTogawaSakiko>(owner, skipVisuals: true)).cardAdded);
            using (CardSelectCmd.PushSelector(new BatchSelector(targets.ToArray(), count)))
                await wish.OnPlayWrapper(context, null, false, zero);
            Require(targets.All(c => c.HasBeenRemovedFromState), "Wish multi-purge or self-purge failed");
        }
        var kiss = combat.CreateCard<KillKiSSCard>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(kiss, PileType.Hand, owner);
        int desiresBefore = owner.Deck.Cards.OfType<DesireCard>().Count();
        using (CardSelectCmd.PushSelector(new Selector(null))) await kiss.OnPlayWrapper(context, combat.GetOpponentsOf(owner.Creature).First(c => c.IsHittable), false, zero);
        var addedDesire = owner.Deck.Cards.OfType<DesireCard>().Last();
        Require(owner.Deck.Cards.OfType<DesireCard>().Count() == desiresBefore + 1 &&
            PersistentDeckMutation.SnapshotLinkedCombatCopies(addedDesire).Any(c => c.Pile?.Type == PileType.Hand),
            "Kill KiSS skip did not add Desire to permanent deck and hand");
        foreach (var desire in owner.Deck.Cards.OfType<DesireCard>().ToArray())
            await SakikoPurgeCommand.RemoveAsync(desire);
        var temporary = combat.CreateCard<DesireCard>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(temporary, PileType.Hand, owner);
        var kao = combat.CreateCard<KaoCard>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(kao, PileType.Hand, owner);
        kao.EnergyCost.SetUntilPlayed(0);
        Require(kao.CanPlay(), "Kao must remain playable without a permanent Desire");
        var food = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<DesireCard>(owner, skipPersistentVisuals: true);
        Require(kao.CanPlay(), "Kao cannot play with removable permanent Desire");
        var opponents = combat.GetOpponentsOf(owner.Creature).Where(c => c.IsHittable).ToArray();
        foreach (var enemy in opponents)
        {
            foreach (var power in enemy.Powers.ToArray()) await PowerCmd.Remove(power);
            await CreatureCmd.GainBlock(enemy, 1000m, ValueProp.Unpowered, null, fast: true);
        }
        var blocks = opponents.Select(c => c.Block).ToArray();
        // A skip selector must never be invoked: Kao is mandatory and random.
        using (CardSelectCmd.PushSelector(new BatchSelector([], -1))) await kao.OnPlayWrapper(context, null, false, zero);
        Require(food.PersistentCard!.HasBeenRemovedFromState && food.CombatCard!.HasBeenRemovedFromState && !temporary.HasBeenRemovedFromState,
            "Kao did not purge the exact persistent Desire and its combat copy");
        Require(opponents.Select((enemy, i) => blocks[i] - enemy.Block).All(x => x == 10), "Kao AoE damage incorrect");
        blocks = opponents.Select(c => c.Block).ToArray();
        using (CardSelectCmd.PushSelector(new BatchSelector([], -1))) await kao.OnPlayWrapper(context, null, false, zero);
        Require(opponents.Select((enemy, i) => blocks[i] - enemy.Block).All(x => x == 10), "Kao still damages enemies with no removable Desire");
        var moon = combat.CreateCard<TheMoonlightSonataCard>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(moon, PileType.Hand, owner);
        int deckCount = owner.Deck.Cards.Count;
        int blockBefore = opponents[0].Block;
        using (CardSelectCmd.PushSelector(new Selector(null))) await moon.OnPlayWrapper(context, opponents[0], false, zero);
        Require(owner.Deck.Cards.Count == deckCount && blockBefore - opponents[0].Block == 15, "Moonlight skip prevented damage");
        var daten = combat.CreateCard<DatenCard>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(daten, PileType.Hand, owner);
        blockBefore = opponents[0].Block;
        using (CardSelectCmd.PushSelector(new Selector(null))) await daten.OnPlayWrapper(context, opponents[0], false, zero);
        Require(owner.Deck.Cards.Count == deckCount && blockBefore - opponents[0].Block == 13, "Daten skip prevented damage");
    }

    private static async Task VerifyBatchFiveAsync(PlayerChoiceContext context, CardModel source)
    {
        var owner = source.Owner;
        var combat = owner.Creature.CombatState!;
        foreach (var power in owner.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        foreach (var card in PileType.Hand.GetPile(owner).Cards.ToArray()) await CardPileCmd.Add(card, PileType.Discard);
        var zero = Zero;
        var u = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<UtopiaCard>(owner, skipPersistentVisuals: true);
        var uOther = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<UtopiaCard>(owner, skipPersistentVisuals: true);
        var persistent = (UtopiaCard)u.PersistentCard!;
        var copy = (UtopiaCard)u.CombatCard!;
        int block = owner.Creature.Block;
        await copy.OnPlayWrapper(context, null, false, zero);
        Require(owner.Creature.Block - block == 15 && copy.DynamicVars.Block.BaseValue == 12 &&
                persistent.DynamicVars.Block.BaseValue == 12 && uOther.PersistentCard!.DynamicVars.Block.BaseValue == 15,
            "Utopia did not grant 15 then decay only its own permanent card by 3");
        CardCmd.Upgrade(copy, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
        CardCmd.Upgrade(persistent, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
        await CardPileCmd.Add(copy, PileType.Hand);
        block = owner.Creature.Block;
        await copy.OnPlayWrapper(context, null, false, zero);
        Require(owner.Creature.Block - block == 12 && persistent.PermanentBlockLoss == 3 && copy.PermanentBlockLoss == 3,
            "Utopia upgrade restored or continued reducing block");
        var restored = (UtopiaCard)owner.RunState.LoadCard(persistent.ToSerializable(), owner);
        Require(restored.IsUpgraded && restored.PermanentBlockLoss == 3 && restored.DynamicVars.Block.BaseValue == 12,
            "Utopia decay lost on save/load");
        restored.RemoveFromState();
        var bottom = (UtopiaCard)uOther.CombatCard!;
        bottom.PermanentBlockLoss = 15;
        ((UtopiaCard)uOther.PersistentCard!).PermanentBlockLoss = 15;
        block = owner.Creature.Block;
        await bottom.OnPlayWrapper(context, null, false, zero);
        Require(owner.Creature.Block == block && bottom.DynamicVars.Block.BaseValue == 0 && bottom.PermanentBlockLoss == 15,
            "Utopia block fell below zero");

        var fate = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<SeizeTheFateCard>(owner, skipPersistentVisuals: true);
        var fateOther = await PersistentDeckMutation.AddCanonicalAsync<SeizeTheFateCard>(owner, skipVisuals: true);
        var fateRun = (SeizeTheFateCard)fate.PersistentCard!;
        await fate.CombatCard!.OnPlayWrapper(context, null, false, zero);
        Require(owner.Creature.GetPower<HypePower>()!.Amount == 6 && fateRun.DynamicVars["MagicNumber"].BaseValue == 5 &&
                fateOther.cardAdded.DynamicVars["MagicNumber"].BaseValue == 6,
            "Seize the Fate initial grant or isolated decay incorrect");
        CardCmd.Upgrade(fateRun, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
        var fateCopy = combat.CloneCard(fateRun);
        fateCopy.DeckVersion = fateRun;
        await CardPileCmd.AddGeneratedCardToCombat(fateCopy, PileType.Hand, owner);
        await fateCopy.OnPlayWrapper(context, null, false, zero);
        Require(owner.Creature.GetPower<HypePower>()!.Amount == 11 && fateRun.TimesPlayed == 1 && fateRun.DynamicVars["MagicNumber"].BaseValue == 5,
            "Seize the Fate upgrade restored or continued reducing Hype");
        var fateLoaded = (SeizeTheFateCard)owner.RunState.LoadCard(fateRun.ToSerializable(), owner);
        Require(fateLoaded.IsUpgraded && fateLoaded.TimesPlayed == 1 && fateLoaded.DynamicVars["MagicNumber"].BaseValue == 5,
            "Seize the Fate decay lost on save/load");
        fateLoaded.TimesPlayed = 100;
        Require(fateLoaded.DynamicVars["MagicNumber"].BaseValue == 0, "Seize the Fate Hype fell below zero");
        fateLoaded.RemoveFromState();

        foreach (bool upgraded in new[] { false, true })
        {
            var fearless = combat.CreateCard<FearlessCard>(owner);
            if (upgraded) CardCmd.Upgrade(fearless, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(fearless, PileType.Hand, owner);
            Require(fearless.EnergyCost.GetWithModifiers(CostModifiers.All) == 2, "Fearless cost must be two in both versions");
            await fearless.OnPlayWrapper(context, null, false, zero);
            var power = owner.Creature.GetPower<FearlessPower>()!;
            Require(power.Amount == (upgraded ? 2 : 1), "Fearless wrong duration");
            int count = owner.Deck.Cards.Count;
            using (CardSelectCmd.PushSelector(new Selector(null)))
                await power.BeforeSideTurnEnd(context, CombatSide.Player, [owner.Creature]);
            Require(owner.Deck.Cards.Count == count && (upgraded ? power.Amount == 1 : !owner.Creature.HasPower<FearlessPower>()),
                "Fearless skip did not consume one opportunity");
            if (upgraded)
            {
                using (CardSelectCmd.PushSelector(new Selector(fateOther.cardAdded)))
                    await power.BeforeSideTurnEnd(context, CombatSide.Player, [owner.Creature]);
                Require(fateOther.cardAdded.HasBeenRemovedFromState && !owner.Creature.HasPower<FearlessPower>(),
                    "Fearless upgraded second optional purge failed to expire");
            }
        }
    }

    private static async Task VerifyBatchFourAsync(PlayerChoiceContext context, CardModel source)
    {
        var owner = source.Owner;
        var combat = owner.Creature.CombatState!;
        var enemy = combat.GetOpponentsOf(owner.Creature).First(c => c.IsHittable);
        foreach (var power in owner.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        foreach (var power in enemy.Powers.ToArray()) await PowerCmd.Remove(power);
        foreach (var card in PileType.Hand.GetPile(owner).Cards.ToArray()) await CardPileCmd.Add(card, PileType.Discard);
        var primo = (await PowerCmd.Apply<PrimoDieInScaenaPower>(context, owner.Creature, 1, owner.Creature, source))!;
        await PowerCmd.Apply<PrimoDieInScaenaPower>(context, owner.Creature, 1, owner.Creature, source);
        int energy = owner.PlayerCombatState!.Energy;
        await primo.AfterPlayerTurnStart(context, owner);
        Require(primo.Amount == 2 && owner.PlayerCombatState.Energy == energy + 2, "Primo energy did not stack");
        await primo.AfterShuffle(context, owner);
        Require(!owner.Creature.HasPower<PrimoDieInScaenaPower>(), "Primo did not remove all stacks on shuffle");

        var first = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<GeorgetteMeGeorgetteYouCard>(
            owner, skipPersistentVisuals: true);
        var other = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<GeorgetteMeGeorgetteYouCard>(
            owner, skipPersistentVisuals: true);
        var runCard = (GeorgetteMeGeorgetteYouCard)first.PersistentCard!;
        var played = (GeorgetteMeGeorgetteYouCard)first.CombatCard!;
        await CreatureCmd.GainBlock(enemy, 1000, ValueProp.Unpowered, null, fast: true);
        for (int play = 0; play < 2; play++)
        {
            var sacrifice = await PersistentDeckMutation.AddCanonicalAsync<DesireCard>(owner, skipVisuals: true);
            int block = enemy.Block;
            await CardPileCmd.Add(played, PileType.Hand);
            using (CardSelectCmd.PushSelector(new Selector(sacrifice.cardAdded)))
                await played.OnPlayWrapper(context, enemy, false,
                    Zero);
            Require(sacrifice.cardAdded.HasBeenRemovedFromState && enemy.Block == block && played.Pile?.Type == PileType.Exhaust,
                "Georgette must purge and exhaust without dealing damage");
            Require(runCard.PermanentCostIncrease == 1 && played.EnergyCost.GetWithModifiers(CostModifiers.All) == 1,
                "Georgette upgrade lost accumulated cost or continued increasing it");
            Require(((GeorgetteMeGeorgetteYouCard)other.PersistentCard!).PermanentCostIncrease == 0 &&
                    other.CombatCard!.EnergyCost.GetWithModifiers(CostModifiers.All) == 0,
                "Georgette changed another same-name card");
            if (play == 0)
            {
                var loaded = (GeorgetteMeGeorgetteYouCard)owner.RunState.LoadCard(runCard.ToSerializable(), owner);
                Require(loaded.PermanentCostIncrease == 1 && loaded.EnergyCost.GetWithModifiers(CostModifiers.All) == 1,
                    "Georgette cost was lost after save/load");
                loaded.RemoveFromState();
                CardCmd.Upgrade(runCard, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
                CardCmd.Upgrade(played, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
            }
        }
        Require(DatenCard.SelectPurgeCandidates([runCard, other.PersistentCard!], 3, owner.RunState.Rng.CombatCardSelection).Length == 2,
            "Daten did not retain both candidates when fewer than three exist");

        // End this diagnostic combat with the unupgraded copy to exercise victory cleanup.
        foreach (var opponent in combat.GetOpponentsOf(owner.Creature).Where(c => c.IsHittable).ToArray())
        {
            await CreatureCmd.LoseBlock(context, opponent, opponent.Block, null);
            if (opponent != enemy) await CreatureCmd.Damage(context, opponent, opponent.CurrentHp, ValueProp.Unpowered, owner.Creature);
        }
        if (enemy.CurrentHp > 1) await CreatureCmd.Damage(context, enemy, enemy.CurrentHp - 1, ValueProp.Unpowered, owner.Creature);
        var finalSacrifice = await PersistentDeckMutation.AddCanonicalAsync<DesireCard>(owner, skipVisuals: true);
        using (CardSelectCmd.PushSelector(new Selector(finalSacrifice.cardAdded)))
            await other.CombatCard!.OnPlayWrapper(context, enemy, false,
                Zero);
        await CreatureCmd.Damage(context, enemy, enemy.CurrentHp, ValueProp.Unpowered, owner.Creature);
        Require(enemy.IsDead && ((GeorgetteMeGeorgetteYouCard)other.PersistentCard!).PermanentCostIncrease == 1 &&
                other.PersistentCard.EnergyCost.GetWithModifiers(CostModifiers.All) == 1,
            "Georgette cost growth did not survive later victory");
    }

    private static async Task VerifyBatchThreeAsync(PlayerChoiceContext context, CardModel source)
    {
        var owner = source.Owner;
        var combat = owner.Creature.CombatState!;
        foreach (var card in PileType.Hand.GetPile(owner).Cards.ToArray())
            await CardPileCmd.Add(card, PileType.Discard);
        foreach (bool upgraded in new[] { false, true })
        {
            var first = combat.CreateCard<DefendTogawaSakiko>(owner);
            var second = combat.CreateCard<StrikeTogawaSakiko>(owner);
            await CardPileCmd.AddGeneratedCardToCombat(second, PileType.Draw, owner, CardPilePosition.Top);
            await CardPileCmd.AddGeneratedCardToCombat(first, PileType.Draw, owner, CardPilePosition.Top);
            var desuwa = combat.CreateCard<DesuWaCard>(owner);
            if (upgraded) CardCmd.Upgrade(desuwa, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(desuwa, PileType.Hand, owner);
            int handBefore = PileType.Hand.GetPile(owner).Cards.Count;
            await desuwa.OnPlayWrapper(context, null, false,
                Zero);
            Require(first.Pile?.Type == PileType.Hand && second.Pile?.Type == PileType.Draw &&
                    PileType.Hand.GetPile(owner).Cards.Count == handBefore,
                "Desuwa must draw exactly one top card without type filtering");
            Require(desuwa.Pile?.Type == (upgraded ? PileType.Discard : PileType.Exhaust),
                "Desuwa did not exhaust only when unupgraded");
            await CardPileCmd.RemoveFromCombat([first, second, desuwa], true);

            var cry = combat.CreateCard<InnerCryCard>(owner);
            if (upgraded) CardCmd.Upgrade(cry, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(cry, PileType.Hand, owner);
            int blockBefore = owner.Creature.Block;
            int mantraBefore = owner.Creature.GetPower<MelodiaPower>()?.Amount ?? 0;
            await cry.OnPlayWrapper(context, null, false,
                Zero);
            Require(owner.Creature.Block - blockBefore == (upgraded ? 11 : 9) &&
                    owner.Creature.GetPower<MelodiaPower>()!.Amount - mantraBefore == (upgraded ? 4 : 3),
                "Inner Cry block/mantra values incorrect");
        }
    }

    private static async Task VerifyBatchTwoAsync(PlayerChoiceContext context, CardModel source)
    {
        var owner = source.Owner;
        var combat = owner.Creature.CombatState!;
        var enemy = combat.GetOpponentsOf(owner.Creature).First(c => c.IsHittable);
        foreach (var card in PileType.Hand.GetPile(owner).Cards.ToArray())
            await CardPileCmd.Add(card, PileType.Discard);
        var drawCards = Enumerable.Range(0, 4).Select(_ => combat.CreateCard<DefendTogawaSakiko>(owner)).ToArray();
        foreach (var card in drawCards.Reverse())
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, owner, CardPilePosition.Top);
        int handBefore = PileType.Hand.GetPile(owner).Cards.Count;
        var shared = (await PowerCmd.Apply<SharedDestinyPower>(context, owner.Creature, 2, owner.Creature, source))!;
        Require(PileType.Hand.GetPile(owner).Cards.Count == handBefore, "Shared Destiny drew on its own application");
        var strength = (await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>(
            context, owner.Creature, 1, owner.Creature, source))!;
        Require(drawCards.Take(2).All(c => c.Pile?.Type == PileType.Hand), "new buff did not draw two cards");
        await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>(context, owner.Creature, 1, owner.Creature, source);
        Require(drawCards.Skip(2).All(c => c.Pile?.Type == PileType.Draw), "buff stacking triggered Shared Destiny");
        await PowerCmd.Remove(strength);
        strength = (await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>(context, owner.Creature, 1, owner.Creature, source))!;
        Require(drawCards.All(c => c.Pile?.Type == PileType.Hand), "reacquired buff did not trigger Shared Destiny");
        await PowerCmd.Remove(shared);
        await PowerCmd.Remove(strength);

        var song = (await PowerCmd.Apply<OurSongPower>(context, owner.Creature, 2, owner.Creature, source))!;
        await song.AfterSideTurnEnd(context, CombatSide.Player, [owner.Creature]);
        Require(owner.Creature.HasPower<OurSongPower>() && song.Amount == 2, "Our Song did not persist across turns");
        await CreatureCmd.LoseBlock(context, enemy, enemy.Block, null);
        await CreatureCmd.Heal(enemy, enemy.MaxHp, playAnim: false);
        await CreatureCmd.GainBlock(enemy, 6, ValueProp.Unpowered, null, fast: true);
        var strike = combat.CreateCard<StrikeTogawaSakiko>(owner);
        strike.DynamicVars.Damage.BaseValue = 10;
        await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, owner);
        int blockBefore = owner.Creature.Block;
        await strike.OnPlayWrapper(context, enemy, false, Zero);
        Require(owner.Creature.Block - blockBefore == 4 && song.Amount == 1,
            "Our Song did not convert 10 damage against 6 block into 4 block and consume one charge");
        var multi = combat.CreateCard<WishToBecomeHumanCard>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(multi, PileType.Hand, owner);
        blockBefore = owner.Creature.Block;
        await multi.OnPlayWrapper(context, enemy, false, Zero);
        Require(owner.Creature.Block - blockBefore == 4 && !owner.Creature.HasPower<OurSongPower>(),
            "Our Song did not count both hits of its second attack exactly once");
        if (owner.Creature.GetPower<DazzlingPower>() is { } dazzling) await PowerCmd.Remove(dazzling);

        foreach (bool upgraded in new[] { false, true })
        {
            foreach (var card in PileType.Hand.GetPile(owner).Cards.ToArray()) await CardPileCmd.Add(card, PileType.Discard);
            var invitation = combat.CreateCard<BandInvitationCard>(owner);
            if (upgraded) CardCmd.Upgrade(invitation, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
            var otherSkill = combat.CreateCard<DefendTogawaSakiko>(owner);
            await CardPileCmd.AddGeneratedCardToCombat(otherSkill, PileType.Hand, owner);
            await CardPileCmd.AddGeneratedCardToCombat(invitation, PileType.Hand, owner);
            owner.PlayerCombatState!.Energy = 9;
            Require(invitation.CanPlay() && invitation.EnergyCost.GetWithModifiers(CostModifiers.All) == 1,
                "Band Invitation still restricts other skills or has the wrong cost");
            var x = combat.CreateCard<CrychicCard>(owner);
            x.EnergyCost.CapturedXValue = 9;
            var zero = combat.CreateCard<StrikeTogawaSakiko>(owner);
            zero.EnergyCost.SetUntilPlayed(0);
            var twoA = combat.CreateCard<StrikeTogawaSakiko>(owner);
            twoA.EnergyCost.SetUntilPlayed(2);
            var twoB = combat.CreateCard<StrikeTogawaSakiko>(owner);
            twoB.EnergyCost.SetUntilPlayed(2);
            var one = combat.CreateCard<StrikeTogawaSakiko>(owner);
            CardModel[] window = [x, zero, twoA, twoB, one];
            foreach (var card in window.Reverse())
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, owner, CardPilePosition.Top);
            Require(BandInvitationCard.CountPositiveEnergy([x, zero]) == 0, "X/discounted cards contributed to draw threshold");
            await invitation.OnPlayWrapper(context, null, false, Zero);
            Require(window.Take(4).All(c => c.Pile?.Type == PileType.Hand) &&
                    one.Pile?.Type == (upgraded ? PileType.Hand : PileType.Draw),
                "Band Invitation did not stop at the actual 4/5 energy threshold");
        }
    }
}
