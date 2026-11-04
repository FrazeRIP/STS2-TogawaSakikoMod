using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using static TogawaSakiko.NativeCode.Diagnostics.MechanicsTestSupport;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class BatchEightMechanicsTests
{
    public static async Task RunAsync(PlayerChoiceContext context, CardModel source, Action<bool, string> require)
    {
        var owner = source.Owner;
        var creature = owner.Creature;
        var combat = creature.CombatState!;
        var enemies = combat.GetOpponentsOf(creature).Where(c => c.IsHittable).ToArray();
        async Task Clear()
        {
            if (creature.GetPower<KindnessPower>() is { } kindness) await PowerCmd.Remove(kindness);
            if (creature.GetPower<HypePower>() is { } hype) await PowerCmd.Remove(hype);
            if (creature.GetPower<ForeverDreamPower>() is { } dream) await PowerCmd.Remove(dream);
            foreach (var power in creature.Powers.ToArray()) await PowerCmd.Remove(power);
            foreach (var card in PileType.Hand.GetPile(owner).Cards.ToArray()) await CardPileCmd.Add(card, PileType.Discard);
        }
        async Task<T> Card<T>(bool upgraded = false) where T : CardModel
        {
            var card = combat.CreateCard<T>(owner);
            if (upgraded) CardCmd.Upgrade(card, CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
            return card;
        }
        async Task Play(CardModel card, Creature? target = null) => await card.OnPlayWrapper(context, target, false, Zero);
        await Clear();
        foreach (var enemy in enemies)
        {
            foreach (var power in enemy.Powers.ToArray()) await PowerCmd.Remove(power);
            await CreatureCmd.GainBlock(enemy, 10000, ValueProp.Unpowered, null, fast: true);
        }
        foreach (bool upgraded in new[] { false, true })
        {
            var kindness = await Card<KindnessCard>(upgraded);
            require(kindness.EnergyCost.GetWithModifiers(CostModifiers.All) == 0, "Kindness always costs zero energy");
            await Play(kindness);
            var hype = await PowerCmd.Apply<HypePower>(context, creature, 2, creature, source);
            await PowerCmd.Apply<ForeverDreamPower>(context, creature, 1, creature, source);
            var strength = await PowerCmd.Apply<StrengthPower>(context, creature, 8, creature, source);
            await PowerCmd.ModifyAmount(context, strength!, -3, creature, source);
            await PowerCmd.Decrement(hype!);
            require(strength!.Amount == 8 && hype!.Amount == 2, "Kindness failed to protect stacks or Hype");
            await PowerCmd.Remove(strength);
            require(creature.HasPower<StrengthPower>() && hype!.Amount == 2, "Kindness/Dream spent protected Hype");
            var protection = creature.GetPower<KindnessPower>()!;
            var stance = await PowerCmd.Apply<MonsterDivinityPower>(context, creature, 1, creature, source);
            await stance!.AfterSideTurnEnd(context, CombatSide.Player, [creature]);
            require(creature.HasPower<MonsterDivinityPower>(), "Kindness expired before ordinary end-turn buff removal");
            await protection.AfterSideTurnEndLate(context, CombatSide.Player, [creature]);
            require(creature.HasPower<KindnessPower>() == upgraded, "Only upgraded Kindness persists through turn end");
            await protection.BeforeSideTurnStart(context, CombatSide.Enemy, [], combat);
            require(creature.HasPower<KindnessPower>() == upgraded, "Only upgraded Kindness persists through the enemy turn");
            await protection.BeforeSideTurnStart(context, CombatSide.Player, [creature], combat);
            require(!creature.HasPower<KindnessPower>() && hype!.Amount == 2, "Dream protected Kindness expiry");
            await PowerCmd.ModifyAmount(context, strength, -3, creature, source);
            require(strength.Amount == 8 && hype!.Amount == 1, "Dream did not resume after Kindness expired");
            await Clear();
        }
        // Freeze (12 + 8 Strength) * 0.75 Weak = 15 per hit, including protected effects.
        foreach (int mode in new[] { 0, 1, 2 })
        {
            await PowerCmd.Apply<StrengthPower>(context, creature, 8, creature, source);
            await PowerCmd.Apply<WeakPower>(context, creature, 1, creature, source);
            if (mode > 0)
            {
                await PowerCmd.Apply<HypePower>(context, creature, 3, creature, source);
                await PowerCmd.Apply<ForeverDreamPower>(context, creature, 1, creature, source);
            }
            if (mode == 2) await Play(await Card<KindnessCard>());
            int count = BlackBirthdayCard.GetRemovablePowers(creature.Powers).Select(p => p.Id).Distinct().Count();
            var blocks = enemies.Select(c => c.Block).ToArray();
            await Play(await Card<BlackBirthdayCard>());
            require(enemies.Select((enemy, i) => blocks[i] - enemy.Block).All(x => x == 15 * count),
                "Black Birthday lost frozen damage or did not count protected powers");
            require(!creature.HasPower<KindnessPower>(), "Black Birthday left Kindness protected by Dream");
            if (mode == 2) require(creature.GetPower<HypePower>()?.Amount == 3 && creature.GetPower<StrengthPower>()?.Amount == 8,
                "Black Birthday Kindness protection depended on insertion order");
            if (mode == 1) require(creature.GetPower<StrengthPower>()?.Amount == 8 && creature.HasPower<ForeverDreamPower>() && !creature.HasPower<HypePower>(),
                "Black Birthday must attempt Dream removal before removing Hype");
            await Clear();
        }
        foreach (bool upgraded in new[] { false, true })
        {
            await PowerCmd.Apply<DazzlingPower>(context, creature, 2, creature, source);
            await Play(await Card<CountingStarsCard>(upgraded));
            require(creature.GetPower<EnergyNextTurnPower>()?.Amount == 2 && creature.GetPower<DazzlingPower>()?.Amount == (upgraded ? 6 : 4),
                "Counting Stars must count existing Dazzling and new next-turn Energy");
            await Clear();
            await PowerCmd.Apply<DazzlingPower>(context, creature, 2, creature, source);
            await PowerCmd.Apply<DexterityPower>(context, creature, 10, creature, source);
            await PowerCmd.Apply<FrailPower>(context, creature, 1, creature, source);
            var lights = await Card<RhinocerosBeetleCard>(upgraded);
            require(lights.EnergyCost.GetWithModifiers(CostModifiers.All) == (upgraded ? 0 : 1), "Stage Lights upgrade cost");
            int block = creature.Block;
            await Play(lights);
            require(creature.GetPower<DazzlingPower>()?.Amount == 5 && creature.Block - block == 5,
                "Stage Lights failed to use post-gain Dazzling or was modified by Dexterity/Frail");
            require(lights.Pile?.Type == PileType.Exhaust, "Stage Lights did not exhaust");
            await Clear();
        }
        var unnamed = await Card<UnnamedOneCard>();
        int enemyBlock = enemies[0].Block;
        await Play(unnamed, enemies[0]);
        require(enemyBlock - enemies[0].Block == 14 && creature.GetPower<FreeNextCardPower>()?.Amount == 1,
            "Unnamed 1 base damage/free charge");
        var upgradedUnnamed = await Card<UnnamedOneCard>(true);
        require(upgradedUnnamed.EnergyCost.GetWithModifiers(CostModifiers.All) == 0, "Free Card failed to make an Attack free");
        enemyBlock = enemies[0].Block;
        await Play(upgradedUnnamed, enemies[0]);
        require(enemyBlock - enemies[0].Block == 20 && creature.GetPower<FreeNextCardPower>()?.Amount == 1,
            "Unnamed 1 consumed its new charge or upgrade damage incorrect");
        var free = await PowerCmd.Apply<FreeNextCardPower>(context, creature, 2, creature, source);
        var skill = await Card<DefendTogawaSakiko>();
        require(free!.Amount == 3 && skill.EnergyCost.GetWithModifiers(CostModifiers.All) == 0, "Free Cards stacking or Skill cost");
        await Play(skill);
        require(free.Amount == 2, "Free Cards Skill charge consumption");
        var alreadyFree = await Card<DesuWaCard>();
        await Play(alreadyFree);
        require(free.Amount == 1, "A natural zero-cost card must consume one Free Card charge");
        var powerCard = await Card<CuriosityCard>();
        require(powerCard.EnergyCost.GetWithModifiers(CostModifiers.All) == 0, "Free Cards Power cost");
        await Play(powerCard);
        require(!creature.HasPower<FreeNextCardPower>() && free.Amount >= 0, "Free Cards must expire at zero");
        free = await PowerCmd.Apply<FreeNextCardPower>(context, creature, 2, creature, source);
        await PowerCmd.ModifyAmount(context, free!, -100, creature, source);
        require(free!.Amount == 0 && !creature.HasPower<FreeNextCardPower>(), "Free Cards went negative");
        await Clear();
        // Preserve native late-cost modification and the separate native X-cost spending path.
        var x = await Card<CrychicCard>();
        var nativeFree = await PowerCmd.Apply<FreeAttackPower>(context, creature, 1, creature, source);
        bool nativeChanged = nativeFree!.TryModifyEnergyCostInCombatLate(await Card<StrikeTogawaSakiko>(), 3m, out decimal nativeCost);
        await PowerCmd.Remove(nativeFree);
        free = await PowerCmd.Apply<FreeNextCardPower>(context, creature, 1, creature, source);
        require(nativeChanged && free!.TryModifyEnergyCostInCombatLate(x, 3m, out decimal xCost) && xCost == nativeCost,
            "X-card late-cost modifier differs from native FreeAttack");
        owner.PlayerCombatState!.Energy = 3;
        require(x.EnergyCost.GetAmountToSpend() == 3, "Native X-cost spending should bypass the fixed-cost free modifier");
        x.EnergyCost.CapturedXValue = 3;
        await x.SpendResources();
        require(owner.PlayerCombatState.Energy == 0 && x.ResolveEnergyXValue() == 3, "X spending/value diverged from native behavior");
        await Play(x);
        require(!creature.HasPower<FreeNextCardPower>(), "X card did not consume its Free Card charge");
        await Clear();
        // Keep two hits, upgrade each hit from 2 to 3; grant Dazzling after each hit.
        await CreatureCmd.LoseBlock(context, enemies[0], enemies[0].Block, null);
        if (enemies[0].CurrentHp < 20) await CreatureCmd.Heal(enemies[0], 20);
        var human = await Card<WishToBecomeHumanCard>(true);
        await Play(human, enemies[0]);
        require(creature.GetPower<DazzlingPower>()?.Amount == 6, "Human upgrade must grant Dazzling from two 3-damage hits");
        await Clear();
    }
}

