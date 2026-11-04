using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using static TogawaSakiko.NativeCode.Diagnostics.MechanicsTestSupport;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class BatchElevenMechanicsTests
{
    public static async Task RunAsync(PlayerChoiceContext context, CardModel source, Action<bool, string> require)
    {
        var owner = source.Owner;
        var creature = owner.Creature;
        var combat = creature.CombatState!;
        var enemy = combat.GetOpponentsOf(creature).First(c => c.IsHittable);
        await CreatureCmd.GainBlock(enemy, 10000, ValueProp.Unpowered, null, fast: true);
        async Task Clear()
        {
            foreach (var p in creature.Powers.ToArray()) await PowerCmd.Remove(p);
            foreach (var c in PileType.Hand.GetPile(owner).Cards.Concat(PileType.Draw.GetPile(owner).Cards).ToArray())
                await CardPileCmd.Add(c, PileType.Discard);
        }
        async Task<T> Card<T>(bool upgraded = false) where T : CardModel
        {
            var c = combat.CreateCard<T>(owner);
            if (upgraded) CardCmd.Upgrade(c, CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(c, PileType.Hand, owner);
            return c;
        }
        await Clear();
        var kings = await Card<KingsCard>();
        int deck = owner.Deck.Cards.Count;
        require(!PileType.Hand.GetPile(owner).Cards.OfType<TirednessCard>().Any(), "Generating Kings must not trigger fatigue");
        for (int i = 1; i <= 2; i++)
        {
            await CardPileCmd.Add(kings, PileType.Draw, CardPilePosition.Top);
            await CardPileCmd.Draw(context, 1, owner);
            require(owner.Deck.Cards.Count == deck + i && PileType.Hand.GetPile(owner).Cards.OfType<TirednessCard>().Count() == i,
                "Every Kings draw adds one persistent fatigue and one hand copy");
        }
        int before = enemy.Block;
        await kings.OnPlayWrapper(context, enemy, false, Zero);
        require(before - enemy.Block == 12, "Kings damage");
        var fatigues = PileType.Hand.GetPile(owner).Cards.OfType<TirednessCard>().ToArray();
        var growth = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<MasqueradeRhapsodyRequestCard>(owner, skipPersistentVisuals: true);
        var removed = fatigues[0].DeckVersion!;
        await fatigues[0].OnPlayWrapper(context, null, false, Zero);
        require(growth.PersistentCard!.DynamicVars.Damage.IntValue == 2, "Fatigue self-removal uses shared purge triggers");
        require(!owner.Deck.Cards.Contains(removed) && !PersistentDeckMutation.SnapshotLinkedCombatCopies(removed).Any(), "Fatigue removes permanent and linked combat instance");
        require(fatigues[1].EnergyCost.GetWithModifiers(CostModifiers.All) == 1 && fatigues[1].DeckVersion!.EnergyCost.GetWithModifiers(CostModifiers.None) == 0,
            "Fatigue raises combat costs only");
        var late = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<TirednessCard>(owner, skipPersistentVisuals: true);
        require(late.CombatCard!.EnergyCost.GetWithModifiers(CostModifiers.All) == 1, "New fatigue inherits combat cost increase");
        await fatigues[1].OnPlayWrapper(context, null, false, Zero);
        require(late.CombatCard.EnergyCost.GetWithModifiers(CostModifiers.All) == 2, "Fatigue cost increases accumulate");
        late.CombatCard.SetToFreeThisTurn();
        require(late.CombatCard.EnergyCost.GetWithModifiers(CostModifiers.All) == 0, "Free effect overrides fatigue base cost");
        foreach (bool upgraded in new[] { false, true })
        {
            await Clear();
            var melody = await Card<UnceasingMelodyCard>(upgraded);
            deck = owner.Deck.Cards.Count;
            await melody.OnPlayWrapper(context, null, false, Zero);
            require(creature.GetPower<HypePower>()?.Amount == (upgraded ? 5 : 4) && owner.Deck.Cards.Count == deck + 1,
                "Unceasing Melody Hype and fatigue");
            await Clear();
            for (int i = 0; i < 5; i++)
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<DefendTogawaSakiko>(owner), PileType.Draw, owner);
            var affairs = await Card<BandAffairsCard>(upgraded);
            int block = creature.Block;
            deck = owner.Deck.Cards.Count;
            await affairs.OnPlayWrapper(context, null, false, Zero);
            require(creature.Block - block == (upgraded ? 12 : 9) && PileType.Hand.GetPile(owner).Cards.Count == (upgraded ? 5 : 4)
                && owner.Deck.Cards.Count == deck + 1, "Band Affairs block/draw/fatigue");
            await Clear();
            var cross = await Card<CrucifixXCard>(upgraded);
            cross.EnergyCost.CapturedXValue = 3;
            block = creature.Block; before = enemy.Block;
            await cross.OnPlayWrapper(context, enemy, false, Zero);
            require(creature.Block - block == (upgraded ? 21 : 15) && before - enemy.Block == (upgraded ? 21 : 15), "Crucifix repeats damage and Block X times");
            await Clear();
            var attack = await Card<KingsCard>();
            await CardPileCmd.Add(attack, PileType.Draw);
            var pride = await Card<PrideCard>(upgraded);
            deck = owner.Deck.Cards.Count;
            using (CardSelectCmd.PushSelector(new FixedSelector(attack))) await pride.OnPlayWrapper(context, null, false, Zero);
            require(attack.Pile?.Type == PileType.Hand && pride.Pile?.Type == PileType.Draw && PileType.Draw.GetPile(owner).Cards.Last() == pride,
                "Pride selects attack then returns itself to top");
            require(owner.Deck.Cards.Count == deck, "Pride retrieval does not change the permanent deck");
            await Clear();
            var masks = await Card<MasksCard>(upgraded);
            var original = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<DefendTogawaSakiko>(owner, skipPersistentVisuals: true);
            using (CardSelectCmd.PushSelector(new FixedSelector(original.CombatCard!))) await masks.OnPlayWrapper(context, null, false, Zero);
            require(creature.GetPower<StrengthPower>()?.Amount == 2 && !creature.HasPower<DazzlingPower>(), "Masks works without Dazzling");
            require(owner.Deck.Cards.Contains(original.PersistentCard!) && PileType.Hand.GetPile(owner).Cards.OfType<MasksCard>().Any(c => c.IsUpgraded == upgraded && c.DeckVersion is null),
                "Masks transforms combat card only, preserving source upgrade");
        }
        await Clear();
        var emptyPride = await Card<PrideCard>();
        await emptyPride.OnPlayWrapper(context, null, false, Zero);
        require(emptyPride.Pile?.Type == PileType.Draw, "Pride returns even with no attack available");
        await Clear();
    }
}
