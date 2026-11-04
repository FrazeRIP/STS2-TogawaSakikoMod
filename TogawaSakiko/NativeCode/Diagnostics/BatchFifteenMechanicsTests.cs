using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using static TogawaSakiko.NativeCode.Diagnostics.MechanicsTestSupport;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class BatchFifteenMechanicsTests
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
        await CreatureCmd.GainBlock(enemy, 999, ValueProp.Unpowered, null, fast: true);
        foreach (bool up in new[] { false, true })
        {
            foreach (var old in owner.PlayerCombatState!.AllCards.OfType<InYourBlueEyesCard>().ToArray()) await CardPileCmd.RemoveFromCombat(old);
            var eyes = await Card<InYourBlueEyesCard>(up); decimal before = enemy.Block;
            await eyes.OnPlayWrapper(context, enemy, false, Zero);
            require(before - enemy.Block == (up ? 9 : 7), "Blue Eyes single hit without matching discard");
            var matching = await Card<InYourBlueEyesCard>(!up); await CardPileCmd.Add(matching, PileType.Discard);
            var extra = await Card<InYourBlueEyesCard>(); await CardPileCmd.Add(extra, PileType.Discard);
            eyes = await Card<InYourBlueEyesCard>(up); before = enemy.Block;
            await eyes.OnPlayWrapper(context, enemy, false, Zero);
            require(before - enemy.Block == (up ? 27 : 21), $"Blue Eyes exactly three hits: up={up}, delta={before-enemy.Block}, before={before}, after={enemy.Block}, discard={PileType.Discard.GetPile(owner).Cards.OfType<InYourBlueEyesCard>().Count()}, enemyPowers={string.Join(",", enemy.Powers.Select(p => p.Id.Entry))}");
            await Clear();
            await PowerCmd.Apply<DexterityPower>(context, creature, 2, creature, source);
            var song = await Card<ASongOfRomanceCard>(up); before = creature.Block;
            int deckCount = owner.Deck.Cards.Count;
            await song.OnPlayWrapper(context, null, false, Zero);
            require(creature.Block - before == (up ? 9 : 6), "Romance gains normal modified block");
            require(PileType.Hand.GetPile(owner).Cards.OfType<DesireCard>().Count() == 1 && owner.Deck.Cards.Count == deckCount, "Romance adds one temporary Desire only");
            await Clear();
            foreach (var old in owner.PlayerCombatState!.AllCards.OfType<DesireCard>().ToArray()) await CardPileCmd.RemoveFromCombat(old);
            var generated = new List<CardModel>();
            foreach (var pile in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Play })
            {
                var d = await Card<DesireCard>(); await CardPileCmd.Add(d, pile); generated.Add(d);
            }
            var world = await Card<TheWholeBlueWorldCard>(up); before = enemy.Block;
            await world.OnPlayWrapper(context, enemy, false, Zero);
            require(before - enemy.Block == 6 + 5 * (up ? 3 : 2), "Blue World counts Desire across all five combat piles including temporary cards");
            await CardPileCmd.RemoveFromCombat(generated[0]);
            world = await Card<TheWholeBlueWorldCard>(up); before = enemy.Block;
            await world.OnPlayWrapper(context, enemy, false, Zero);
            require(before - enemy.Block == 6 + 4 * (up ? 3 : 2), "Blue World excludes removed combat cards");
            await Clear();
        }
        var angles = await Card<AnglesCard>(); await angles.OnPlayWrapper(context, null, false, Zero);
        var power = creature.GetPower<AnglesPower>()!;
        await PowerCmd.Apply<DexterityPower>(context, creature, 20, creature, source);
        await PowerCmd.Apply<FrailPower>(context, creature, 2, creature, source);
        decimal block = creature.Block;
        for (int i = 0; i < 4; i++)
        {
            if (i == 2) await power.AfterSideTurnEnd(context, creature.Side, [creature]);
            var attack = await Card<StrikeTogawaSakiko>(i == 2);
            await attack.OnPlayWrapper(context, enemy, false, Zero);
            require(creature.Block == block + 3 * Math.Max(0, i), "Angles AAAA triggers on 2/3/4, ignores Dex/Frail, survives turn end");
            require(power.Description.Variables["LastCard"].ToString() == attack.Title, "Angles tooltip shows previous card");
        }
        angles = await Card<AnglesCard>(true); await angles.OnPlayWrapper(context, null, false, Zero);
        require(power.Amount == 7 && angles.EnergyCost.GetWithModifiers(CostModifiers.None) == 1, "Angles stacks and upgrades block at one energy");
        var strike = await Card<StrikeTogawaSakiko>(); await strike.OnPlayWrapper(context, enemy, false, Zero);
        strike = await Card<StrikeTogawaSakiko>(); await strike.OnPlayWrapper(context, enemy, false, Zero);
        require(creature.Block == block + 16, "Stacked Angles grants seven block per pair");
        await Clear();
        await CreatureCmd.LoseBlock(context, creature, creature.Block, null);
        await CreatureCmd.GainBlock(creature, 20, ValueProp.Unpowered, null, fast: true);
        await PowerCmd.Apply<StrengthPower>(context, creature, 30, creature, source);
        await PowerCmd.Apply<ArdorPower>(context, creature, 20, creature, source);
        int hp = creature.CurrentHp;
        var cruelty = await Card<CrueltyCard>(); await cruelty.OnPlayWrapper(context, null, false, Zero);
        require(creature.Block == 12 && creature.CurrentHp == hp, "Cruelty damage is blockable and unaffected by Strength/Ardor");
        await CreatureCmd.LoseBlock(context, creature, creature.Block, null);
        cruelty = await Card<CrueltyCard>(true); await cruelty.OnPlayWrapper(context, null, false, Zero);
        require(creature.CurrentHp == hp - 8 && cruelty.EnergyCost.GetWithModifiers(CostModifiers.None) == 0, "Cruelty unblocked damage is exactly eight, upgrade only cost");
        require(creature.GetPower<TogawaSakiko.NativeCode.Models.Powers.CrueltyPower>()!.Amount == 4, "Cruelty stacks draw count");
        for (int i = 0; i < 4; i++) { var d = await Card<DefendTogawaSakiko>(); await CardPileCmd.Add(d, PileType.Draw); }
        var desire = await Card<DesireCard>();
        var beforeHand = PileType.Hand.GetPile(owner).Cards.ToHashSet();
        await desire.OnPlayWrapper(context, null, false, Zero);
        require(PileType.Hand.GetPile(owner).Cards.Except(beforeHand).Count() == 4, "Cruelty draws stacked four cards on Desire play");
        await Clear();
    }
}

