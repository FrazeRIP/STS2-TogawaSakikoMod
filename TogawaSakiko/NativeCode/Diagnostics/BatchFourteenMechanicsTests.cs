using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using static TogawaSakiko.NativeCode.Diagnostics.MechanicsTestSupport;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class BatchFourteenMechanicsTests
{
    public static async Task RunAsync(PlayerChoiceContext context, CardModel source, Action<bool, string> require)
    {
        var owner = source.Owner; var creature = owner.Creature; var combat = creature.CombatState!;
        var enemy = combat.GetOpponentsOf(creature).First(c => c.IsHittable);
        async Task Clear()
        {
            foreach (var p in creature.Powers.ToArray()) await PowerCmd.Remove(p);
            foreach (var c in PileType.Hand.GetPile(owner).Cards.Concat(PileType.Draw.GetPile(owner).Cards).ToArray()) await CardPileCmd.Add(c, PileType.Discard);
        }
        async Task<T> Card<T>(bool up = false) where T : CardModel
        {
            var c = combat.CreateCard<T>(owner); if (up) CardCmd.Upgrade(c, CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(c, PileType.Hand, owner); return c;
        }
        await Clear();
        foreach (var opponent in combat.GetOpponentsOf(creature).Where(c => c.IsHittable))
            await CreatureCmd.GainBlock(opponent, 10000, ValueProp.Unpowered, null, fast: true);
        foreach (bool up in new[] { false, true })
        {
            var world = await Card<WorldviewCard>(up);
            require(world.EnergyCost.GetWithModifiers(CostModifiers.None) == 1 && world.Keywords.Contains(CardKeyword.Innate) == up, "Worldview always costs one and upgrades to Innate");
            var permanent = (await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<TirednessCard>(owner, skipPersistentVisuals: true));
            await CardPileCmd.Add(permanent.CombatCard!, PileType.Draw, CardPilePosition.Top);
            await world.OnPlayWrapper(context, null, false, Zero);
            var handBefore = PileType.Hand.GetPile(owner).Cards.ToHashSet();
            await CardPileCmd.Draw(context, 1, owner);
            var added = PileType.Hand.GetPile(owner).Cards.Except(handBefore).ToArray();
            require(added.Length == 1 && added[0].Type == CardType.Attack && owner.Deck.Cards.Contains(permanent.PersistentCard), $"Worldview transforms playable Curse draw without changing permanent deck: added={string.Join(",", added.Select(c => c.Id+":"+c.Type))}; deck={owner.Deck.Cards.Contains(permanent.PersistentCard)}; cursePile={permanent.CombatCard!.Pile?.Type}; power={creature.HasPower<WorldviewPower>()}");
            handBefore = PileType.Hand.GetPile(owner).Cards.ToHashSet();
            var generatedCurse = combat.CreateCard<TirednessCard>(owner);
            await CardPileCmd.AddGeneratedCardToCombat(generatedCurse, PileType.Hand, owner);
            added = PileType.Hand.GetPile(owner).Cards.Except(handBefore).ToArray();
            require(added.Length == 1 && added[0].Type == CardType.Attack && !owner.PlayerCombatState!.AllCards.Contains(generatedCurse), "Worldview also transforms generated hand curses");
            await Clear();
            var choir = await Card<ChoirSChoirCard>(up);
            require(choir.Keywords.Contains(CardKeyword.Retain) == up && choir.DynamicVars.Damage.BaseValue == 6, "Choir only gains Retain on upgrade");
            await CardPileCmd.Add(choir, PileType.Discard);
            var desire = await Card<DesireCard>();
            await desire.OnPlayWrapper(context, null, false, Zero);
            require(choir.EnergyCost.GetWithModifiers(CostModifiers.Local) == 2, "Choir in discard discounts when Desire played");
            await CardPileCmd.Add(choir, PileType.Hand);
            await choir.OnPlayWrapper(context, enemy, false, Zero);
            require(choir.EnergyCost.GetWithModifiers(CostModifiers.Local) == 3, "Choir discount resets after play");
            await Clear();
            var accomplice = await Card<AccompliceCard>(up); var existing = await Card<DesireCard>();
            int deckCount = owner.Deck.Cards.Count;
            await accomplice.OnPlayWrapper(context, null, false, Zero);
            var desires = PileType.Hand.GetPile(owner).Cards.OfType<DesireCard>().ToArray();
            require(desires.Length == (up ? 4 : 3) && desires.All(c => c.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0) && owner.Deck.Cards.Count == deckCount, $"Accomplice creates temporary Desires and frees all current hand Desires: up={up}, count={desires.Length}, cost={string.Join(",", desires.Select(c => c.EnergyCost.GetWithModifiers(CostModifiers.Local)))}, deck={owner.Deck.Cards.Count}/{deckCount}, hand={PileType.Hand.GetPile(owner).Cards.Count}, powers={string.Join(",", creature.Powers.Select(p=>p.Id))}");
            await Clear();
            var imprisoned = await Card<ImprisonedXIICard>(up);
            var original = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<StrikeTogawaSakiko>(owner, skipPersistentVisuals: true);
            var temp = await Card<DefendTogawaSakiko>();
            deckCount = owner.Deck.Cards.Count;
            using (CardSelectCmd.PushSelector(new FixedSelector(original.CombatCard!, temp))) await imprisoned.OnPlayWrapper(context, null, false, Zero);
            desires = PileType.Hand.GetPile(owner).Cards.OfType<DesireCard>().ToArray();
            require(desires.Length == 2 && desires.All(c => c.Keywords.Contains(CardKeyword.Retain) && c.IsUpgraded == up) && owner.Deck.Cards.Count == deckCount && owner.Deck.Cards.Contains(original.PersistentCard), "Imprisoned transforms chosen hand cards with Retain without changing permanent cards");
            imprisoned = await Card<ImprisonedXIICard>(up);
            using (CardSelectCmd.PushSelector(new FixedSelector())) await imprisoned.OnPlayWrapper(context, null, false, Zero);
            require(PileType.Hand.GetPile(owner).Cards.OfType<DesireCard>().Count() == 2, "Imprisoned allows zero selections");
            await Clear();
        }
        var encore = await Card<CharismaticFormCard>();
        await encore.OnPlayWrapper(context, null, false, Zero);
        var power = creature.GetPower<EncorePower>()!;
        require(power.Description.Variables["LastCard"].ToString() == encore.Title, "Encore tooltip records last played card");
        int energy = owner.PlayerCombatState!.Energy;
        for (int i = 0; i < 4; i++)
        {
            if (i == 2) await power.AfterSideTurnEnd(context, creature.Side, [creature]);
            var defend = await Card<DefendTogawaSakiko>(i == 2);
            await defend.OnPlayWrapper(context, null, false, Zero);
            require(power.Description.Variables["LastCard"].ToString() == defend.Title, "Encore tooltip updates after every card");
            require(owner.PlayerCombatState.Energy == energy + Math.Max(0, i), "Encore AAAA triggers on 2/3/4 including upgraded name and across turns");
        }
        var strike = await Card<StrikeTogawaSakiko>(); await strike.OnPlayWrapper(context, enemy, false, Zero);
        require(owner.PlayerCombatState.Energy == energy + 3, "Different name breaks Encore chain");
        encore = await Card<CharismaticFormCard>(true); await encore.OnPlayWrapper(context, null, false, Zero);
        require(power.Amount == 2, "Encore stacks energy amount");
        var a = await Card<DefendTogawaSakiko>(); await a.OnPlayWrapper(context, null, false, Zero);
        a = await Card<DefendTogawaSakiko>(); await a.OnPlayWrapper(context, null, false, Zero);
        require(owner.PlayerCombatState.Energy == energy + 5, "Stacked Encore gives two energy per matching pair");
        await Clear();
        foreach (var opponent in combat.GetOpponentsOf(creature).Where(c => c.IsHittable))
            await CreatureCmd.GainBlock(opponent, 10000, ValueProp.Unpowered, null, fast: true);
        foreach (bool up in new[] { false, true })
        {
            var resolved = await Card<EncoreCard>(up);
            int expected = owner.Deck.Cards.Count(card => card.Type == CardType.Curse);
            require(resolved.Type == CardType.Power && resolved.Rarity == CardRarity.Uncommon && resolved.EnergyCost.GetWithModifiers(CostModifiers.None) == 1, "Resolved skill rarity and cost");
            await resolved.OnPlayWrapper(context, null, false, Zero);
            require((creature.GetPower<HypePower>()?.Amount ?? 0) == expected * (up ? 2 : 1) && resolved.Pile?.Type != PileType.Exhaust && !creature.HasPower<EncorePower>(), "Resolved counts permanent curses as a Power");
            await Clear();
        }
        await Clear();
    }
}

