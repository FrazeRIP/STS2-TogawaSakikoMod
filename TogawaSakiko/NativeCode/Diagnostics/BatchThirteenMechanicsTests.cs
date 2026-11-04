using MegaCrit.Sts2.Core.Combat;
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

internal static class BatchThirteenMechanicsTests
{
    public static async Task RunAsync(PlayerChoiceContext context, CardModel source, Action<bool, string> require)
    {
        var owner = source.Owner; var creature = owner.Creature; var combat = creature.CombatState!;
        var enemies = combat.GetOpponentsOf(creature).Where(c => c.IsHittable).ToArray();
        var enemy = enemies[0];
        foreach (var e in enemies) await CreatureCmd.GainBlock(e, 10000, ValueProp.Unpowered, null, fast: true);
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
        foreach (bool up in new[] { false, true })
        {
            await Clear();
            var fire = await Card<SymbolIFireCard>(up); int before = enemies.Sum(c => c.Block);
            int curses = owner.Deck.Cards.OfType<TimorisCard>().Count();
            await fire.OnPlayWrapper(context, null, false, Zero);
            require(before - enemies.Sum(c => c.Block) == 20 * enemies.Length && owner.Deck.Cards.OfType<TimorisCard>().Count() == curses + 1, "Fire damage and curse");
            var echo = creature.GetPower<FireEchoPower>()!;
            await echo.AfterSideTurnEnd(context, CombatSide.Player, [creature]);
            require(echo.Amount == (up ? 2 : 1), "Fire Echo persists across turns");
            var skill = await Card<DefendTogawaSakiko>(); await skill.OnPlayWrapper(context, null, false, Zero);
            require(creature.HasPower<FireEchoPower>(), "Skill must not consume Fire Echo");
            var strike = await Card<StrikeTogawaSakiko>(); before = enemy.Block;
            await strike.OnPlayWrapper(context, enemy, false, Zero);
            require(before - enemy.Block == 6 * (up ? 3 : 2) && !creature.HasPower<FireEchoPower>(), "Fire Echo repeats the next Attack N additional times and consumes all charges");
            await Clear();
            for (int i = 0; i < 5; i++) await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<DefendTogawaSakiko>(owner), PileType.Draw, owner);
            var air = await Card<SymbolIIAirCard>(up); before = enemies.Sum(c => c.Block);
            await air.OnPlayWrapper(context, null, false, Zero);
            require(before - enemies.Sum(c => c.Block) == (up ? 20 : 16) && PileType.Hand.GetPile(owner).Cards.Count == (up ? 5 : 4), "Air random damage, draw, and added curse");
            await Clear();
            var water = await Card<SymbolIIIWaterCard>(up); before = enemy.Block;
            await water.OnPlayWrapper(context, enemy, false, Zero);
            require(before - enemy.Block == (up ? 20 : 14) && enemy.IsStunned && !creature.HasPower<AmbergrisPower>(), "Water damage and native stun without extra turn");
            await Clear();
            await PowerCmd.Apply<DexterityPower>(context, creature, 3, creature, source);
            await PowerCmd.Apply<StrengthPower>(context, creature, 2, creature, source);
            var earth = await Card<SymbolIVEarthCard>(up); int block = creature.Block; before = enemy.Block;
            await earth.OnPlayWrapper(context, enemy, false, Zero);
            require(creature.Block - block == (up ? 23 : 18) && before - enemy.Block == creature.Block + 2, "Earth uses post-gain actual Block and applies Strength once");
            await Clear();
            var improv = await Card<ImprovisationCard>(up); before = enemies.Sum(c => c.Block);
            await PowerCmd.Apply<ArdorPower>(context, creature, 1, creature, source);
            int targetBefore = enemy.Block;
            await improv.OnPlayWrapper(context, enemy, false, Zero);
            require(before - enemies.Sum(c => c.Block) == 18 && targetBefore - enemy.Block == 18 && improv.TargetType == TargetType.AnyEnemy, "Improvisation deals six hits only to the selected enemy");
        }
        await Clear();
        var one = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<DesuWaCard>(owner, skipPersistentVisuals: true);
        var two = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<DesuWaCard>(owner, skipPersistentVisuals: true, upgradeLevel: 1);
        var temporary = await Card<DesuWaCard>();
        int total = owner.Deck.Cards.OfType<DesuWaCard>().Count();
        var ether = await Card<EtherCard>(); int oldBlock = enemy.Block;
        using (CardSelectCmd.PushSelector(new FixedSelector(one.PersistentCard))) await ether.OnPlayWrapper(context, enemy, false, Zero);
        require(!owner.Deck.Cards.OfType<DesuWaCard>().Any() && one.CombatCard!.HasBeenRemovedFromState && two.CombatCard!.HasBeenRemovedFromState,
            "Ether removes all same-name permanent cards irrespective of upgrades");
        require(temporary.Pile?.Type == PileType.Hand && oldBlock - enemy.Block == 2 * total, "Ether excludes temporary cards from removal and hit count");
        ether = await Card<EtherCard>(true); int count = owner.Deck.Cards.Count; oldBlock = enemy.Block;
        using (CardSelectCmd.PushSelector(new FixedSelector())) await ether.OnPlayWrapper(context, enemy, false, Zero);
        require(owner.Deck.Cards.Count == count + 1 && enemy.Block == oldBlock, "Skipping Ether still adds curse but deals no damage");
        await Clear();
        var expired = (ImprovisationCard)(await PersistentDeckMutation.AddCanonicalAsync<ImprovisationCard>(owner, skipVisuals: true)).cardAdded;
        await expired.AfterCombatEnd(null!); await expired.AfterCombatEnd(null!);
        var restored = (ImprovisationCard)owner.RunState.LoadCard(expired.ToSerializable(), owner);
        require(restored.CombatsSeen == 2 && restored.DynamicVars["CombatsRemaining"].IntValue == 1, "Improvisation saves countdown");
        await expired.AfterCombatEnd(null!); require(!owner.Deck.Cards.Contains(expired), "Improvisation expires after three combats");
        var permanent = (ImprovisationCard)(await PersistentDeckMutation.AddCanonicalAsync<ImprovisationCard>(owner, skipVisuals: true, upgradeLevel: 1)).cardAdded;
        for (int i = 0; i < 4; i++) await permanent.AfterCombatEnd(null!);
        require(owner.Deck.Cards.Contains(permanent), "Improvisation+ does not expire");
        await Clear();
        foreach (bool temporarySource in new[] { false, true })
        {
            var target = temporarySource ? await Card<StrikeTogawaSakiko>(true) :
                (await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<StrikeTogawaSakiko>(owner, skipPersistentVisuals: true)).CombatCard!;
            if (!temporarySource) CardCmd.Upgrade(target, CardPreviewStyle.None);
            var copy = await Card<AsYourHeartDesiresCard>(); var before = owner.Deck.Cards.ToHashSet();
            using (CardSelectCmd.PushSelector(new FixedSelector(target))) await copy.OnPlayWrapper(context, null, false, Zero);
            var added = owner.Deck.Cards.Except(before).Single();
            require(added.IsUpgraded == temporarySource && PersistentDeckMutation.SnapshotLinkedCombatCopies(added).Count == 1,
                "Heart Desires copies permanent version when available, otherwise temporary hand card");
        }
        await Clear();
    }

    public static async Task RunLethalAsync(PlayerChoiceContext context, CardModel source, string variant, Action<bool,string> require)
    {
        var owner = source.Owner; var combat = owner.Creature.CombatState!;
        var enemies = combat.GetOpponentsOf(owner.Creature).Where(c => c.IsHittable).ToArray(); var last = enemies[0];
        foreach (var e in enemies)
        {
            await CreatureCmd.LoseBlock(context, e, e.Block, null);
            if (e != last) await CreatureCmd.Damage(context, e, e.CurrentHp, ValueProp.Unpowered, owner.Creature);
        }
        if (last.CurrentHp > 1) await CreatureCmd.Damage(context, last, last.CurrentHp - 1, ValueProp.Unpowered, owner.Creature);
        CardModel card = variant switch
        {
            "fire" => combat.CreateCard<SymbolIFireCard>(owner), "air" => combat.CreateCard<SymbolIIAirCard>(owner),
            "water" => combat.CreateCard<SymbolIIIWaterCard>(owner), "earth" => combat.CreateCard<SymbolIVEarthCard>(owner),
            "ether" => combat.CreateCard<EtherCard>(owner), _ => throw new ArgumentException(variant)
        };
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
        var sacrifice = variant == "ether" ? (await PersistentDeckMutation.AddCanonicalAsync<DesireCard>(owner, skipVisuals: true)).cardAdded : null;
        var before = owner.Deck.Cards.ToHashSet();
        using (CardSelectCmd.PushSelector(new FixedSelector(sacrifice))) await card.OnPlayWrapper(context, card.TargetType == TargetType.AnyEnemy ? last : null, false, Zero);
        require(last.IsDead, "Element failed to kill final enemy");
        require(!owner.Deck.Cards.Except(before).Any(), "Element added permanent curse after lethal damage");
    }
}
