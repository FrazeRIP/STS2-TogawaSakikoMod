using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Models.Powers;
using static TogawaSakiko.NativeCode.Diagnostics.MechanicsTestSupport;

namespace TogawaSakiko.NativeCode.Diagnostics;
internal static class KindnessInteractionTests
{
    internal static async Task RunAsync(PlayerChoiceContext context, CardModel source, Action<bool, string> require)
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
        }
        await Clear();
        foreach (var enemy in enemies) await CreatureCmd.GainBlock(enemy, 10000, ValueProp.Unpowered, null, fast: true);
        await PowerCmd.Apply<KindnessPower>(context, creature, 1, creature, source);
        await PowerCmd.Apply<MelodiaPower>(context, creature, 10, creature, source);
        require(creature.GetPower<MelodiaPower>()?.Amount == 10 && creature.HasPower<MonsterDivinityPower>(), "Kindness preserves ten Mantra while entering divinity");
        var stance = creature.GetPower<MonsterDivinityPower>()!;
        await PowerCmd.Apply<MelodiaPower>(context, creature, 1, creature, source);
        require(creature.GetPower<MelodiaPower>()?.Amount == 11 && ReferenceEquals(stance, creature.GetPower<MonsterDivinityPower>()), "Protected Mantra gain does not recursively consume stacks or duplicate stance");
        await creature.GetPower<KindnessPower>()!.BeforeSideTurnStart(context, CombatSide.Player, [creature], combat);
        await PowerCmd.Apply<MelodiaPower>(context, creature, 1, creature, source);
        require(creature.GetPower<MelodiaPower>()?.Amount == 2, "Mantra consumes ten normally after Kindness expires");
        await Clear();
        foreach (int stacks in new[] { 9, 10, 11, 25 })
        {
            await PowerCmd.Apply<KindnessPower>(context, creature, 1, creature, source);
            await PowerCmd.Apply<MelodiaPower>(context, creature, stacks, creature, source);
            await creature.GetPower<KindnessPower>()!.BeforeSideTurnStart(context, CombatSide.Player, [creature], combat);
            if (creature.GetPower<MonsterDivinityPower>() is { } activeStance) await PowerCmd.Remove(activeStance);
            var mantra = creature.GetPower<MelodiaPower>()!;
            await mantra.AfterPlayerTurnStart(context, owner);
            require(mantra.Amount == (stacks >= 10 ? stacks - 10 : stacks) && creature.HasPower<MonsterDivinityPower>() == (stacks >= 10),
                "Own-turn Mantra check requires at least ten and triggers only once: " + stacks);
            await Clear();
        }
        var play = new CardPlay { Player = owner, Target = null, ResultPile = PileType.Discard, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1, Card = source, Resources = Zero };
        foreach (bool protectedByKindness in new[] { false, true })
        {
            if (protectedByKindness) await PowerCmd.Apply<KindnessPower>(context, creature, 1, creature, source);
            await PowerCmd.Apply<DazzlingPower>(context, creature, 3, creature, source);
            var tender = (await PowerCmd.Apply<TenderPower>(context, creature, 1, creature, source))!;
            decimal before = enemies.Sum(e => e.Block);
            await tender.AfterCardPlayed(context, play);
            require(creature.GetPower<StrengthPower>()?.Amount == -1 && creature.GetPower<DexterityPower>()?.Amount == -1, "Tender can apply negative stats from zero with or without Kindness");
            await tender.AfterSideTurnEnd(context, CombatSide.Player, [creature]);
            require(!creature.HasPower<StrengthPower>() && !creature.HasPower<DexterityPower>(), "Tender restored zero stats must be removed even with Kindness");
            require(enemies.Sum(e => e.Block) == before, "Restoring negative stats to zero does not grant a positive effect for Dazzling");
            require(creature.HasPower<TenderPower>(), "Tender debuff remains active after its per-turn stats recover");
            await Clear();
        }
        await PowerCmd.Apply<StrengthPower>(context, creature, 2, creature, source);
        await PowerCmd.Apply<DexterityPower>(context, creature, 2, creature, source);
        await PowerCmd.Apply<KindnessPower>(context, creature, 1, creature, source);
        await PowerCmd.Apply<DazzlingPower>(context, creature, 3, creature, source);
        var protectedTender = (await PowerCmd.Apply<TenderPower>(context, creature, 1, creature, source))!;
        decimal blockBefore = enemies.Sum(e => e.Block);
        await protectedTender.AfterCardPlayed(context, play);
        require(creature.GetPower<StrengthPower>()?.Amount == 2 && creature.GetPower<DexterityPower>()?.Amount == 2, "Kindness protects positive stats from Tender");
        await protectedTender.AfterSideTurnEnd(context, CombatSide.Player, [creature]);
        require(creature.GetPower<StrengthPower>()?.Amount == 3 && creature.GetPower<DexterityPower>()?.Amount == 3, "Tender still grants its native end-turn restoration after prevented losses");
        require(enemies.Sum(e => e.Block) == blockBefore - 6, "Tender positive Strength and Dexterity gains trigger Dazzling separately");
        await Clear();
    }
}
