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

internal static class BatchNineMechanicsTests
{
    public static async Task RunAsync(PlayerChoiceContext context, CardModel source, Action<bool, string> require)
    {
        var owner = source.Owner;
        var combat = owner.Creature.CombatState!;
        foreach (var power in owner.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        var enemies = combat.GetOpponentsOf(owner.Creature).Where(c => c.IsHittable).ToArray();
        foreach (var enemy in enemies) await CreatureCmd.GainBlock(enemy, 1000, ValueProp.Unpowered, null, fast: true);
        async Task CheckPhantom<T, TReward>(bool upgraded, int damage) where T : CardModel where TReward : CardModel
        {
            foreach (var hand in PileType.Hand.GetPile(owner).Cards.ToArray()) await CardPileCmd.Add(hand, PileType.Discard);
            var card = combat.CreateCard<T>(owner);
            if (upgraded) CardCmd.Upgrade(card, CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
            var before = owner.Deck.Cards.ToHashSet();
            int block = enemies.Sum(c => c.Block);
            await card.OnPlayWrapper(context, card.TargetType == TargetType.AnyEnemy ? enemies[0] : null, false, Zero);
            var added = owner.Deck.Cards.Except(before).ToArray();
            require(block - enemies.Sum(c => c.Block) == damage, $"{typeof(T).Name}: damage");
            require(added.Length == 1 && added[0] is TReward && added[0].IsUpgraded == upgraded,
                $"{typeof(T).Name}: permanent reward/upgrade");
            require(PersistentDeckMutation.SnapshotLinkedCombatCopies(added[0]).Any(c => c.Pile?.Type == PileType.Hand && c.IsUpgraded == upgraded),
                $"{typeof(T).Name}: exact linked reward missing from Hand");
            require(card.Pile?.Type == PileType.Exhaust, $"{typeof(T).Name}: Exhaust");
        }
        foreach (bool upgraded in new[] { false, true })
        {
            await CheckPhantom<PhantomOfSakikoCard, RadianceCard>(upgraded, 6);
            await CheckPhantom<PhantomOfTomoriCard, VoiceCard>(upgraded, 6);
            await CheckPhantom<PhantomOfTakiCard, IdealCard>(upgraded, 7);
            int previousBlock = owner.Creature.Block;
            await CheckPhantom<PhantomOfMutsumiCard, ProtectionCard>(upgraded, 3);
            require(owner.Creature.Block == previousBlock + 3, "Mutsumi grants three Block");
            await CheckPhantom<PhantomOfSoyoCard, KindnessCard>(upgraded, 8 * enemies.Length);
            foreach (var hand in PileType.Hand.GetPile(owner).Cards.ToArray()) await CardPileCmd.Add(hand, PileType.Discard);
            int deckCount = owner.Deck.Cards.Count;
            var crychic = combat.CreateCard<CrychicCard>(owner);
            if (upgraded) CardCmd.Upgrade(crychic, CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(crychic, PileType.Hand, owner);
            crychic.EnergyCost.CapturedXValue = 3;
            await crychic.OnPlayWrapper(context, null, false, Zero);
            var phantoms = PileType.Hand.GetPile(owner).Cards.ToArray();
            require(phantoms.Length == 3 && owner.Deck.Cards.Count == deckCount && phantoms.All(c => c.DeckVersion is null),
                "Crychic must generate only three temporary hand cards");
            require(phantoms.All(c => CrychicPower.PhantomCanonicals.Any(p => p.GetType() == c.GetType()) && c.IsUpgraded == upgraded),
                "Crychic random pool or upgrade");
            foreach (var phantom in phantoms) phantom.EnergyCost.EndOfTurnCleanup();
            require(phantoms.All(c => c.EnergyCost.GetWithModifiers(CostModifiers.All) == 0), "Crychic free cost expired at turn end");
            var chosen = phantoms[0];
            await chosen.OnPlayWrapper(context, chosen.TargetType == TargetType.AnyEnemy ? enemies[0] : null, false, Zero);
            require(chosen.EnergyCost.GetWithModifiers(CostModifiers.All) == chosen.EnergyCost.Canonical,
                "Crychic free cost did not expire after first play");
        }
        foreach (var power in owner.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        await CreatureCmd.GainBlock(owner.Creature, 100, ValueProp.Unpowered, null, fast: true);
        var luck = combat.CreateCard<WishYouGoodLuckCard>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(luck, PileType.Hand, owner);
        await luck.OnPlayWrapper(context, null, false, Zero);
        for (int hit = 0; hit < 3; hit++)
            await CreatureCmd.Damage(context, owner.Creature, 1, ValueProp.Move, enemies[0]);
        require(owner.Creature.GetPower<ThornsPower>()?.Amount == 3, "Good Luck grants three Thorns immediately");
        await PowerCmd.Remove(owner.Creature.GetPower<ThornsPower>()!);
        await CreatureCmd.Damage(context, owner.Creature, 0, ValueProp.Move, enemies[0]);
        require(!owner.Creature.HasPower<ThornsPower>(), "Good Luck no longer grants reactive Thorns");
        foreach (bool up in new[] { false, true })
        {
            foreach (var power in owner.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
            await PowerCmd.Apply<DazzlingPower>(context, owner.Creature, 2, owner.Creature, source);
            decimal totalBlock = enemies.Sum(e => e.Block);
            int playerBlock = owner.Creature.Block;
            var skill = combat.CreateCard<WishYouGoodLuckCard>(owner);
            if (up) CardCmd.Upgrade(skill, CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(skill, PileType.Hand, owner);
            await skill.OnPlayWrapper(context, null, false, Zero);
            require(owner.Creature.Block == playerBlock + (up ? 18 : 16) && owner.Creature.GetPower<ThornsPower>()?.Amount == (up ? 5 : 3) && enemies.Sum(e => e.Block) == totalBlock - (up ? 10 : 6), "Good Luck grants Block and separate Thorns applications triggering Dazzling");
        }
        foreach (var power in owner.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
    }

    public static async Task RunLethalAsync(PlayerChoiceContext context, CardModel source, string variant, Action<bool, string> require)
    {
        var owner = source.Owner;
        var combat = owner.Creature.CombatState!;
        var enemies = combat.GetOpponentsOf(owner.Creature).Where(c => c.IsHittable).ToArray();
        var last = enemies[0];
        foreach (var enemy in enemies)
        {
            await CreatureCmd.LoseBlock(context, enemy, enemy.Block, null);
            if (enemy != last) await CreatureCmd.Damage(context, enemy, enemy.CurrentHp, ValueProp.Unpowered, owner.Creature);
        }
        if (last.CurrentHp > 1) await CreatureCmd.Damage(context, last, last.CurrentHp - 1, ValueProp.Unpowered, owner.Creature);
        foreach (var hand in PileType.Hand.GetPile(owner).Cards.ToArray()) await CardPileCmd.Add(hand, PileType.Discard);
        CardModel phantom = variant switch
        {
            "sakiko" => combat.CreateCard<PhantomOfSakikoCard>(owner),
            "tomori" => combat.CreateCard<PhantomOfTomoriCard>(owner),
            "taki" => combat.CreateCard<PhantomOfTakiCard>(owner),
            "mutsumi" => combat.CreateCard<PhantomOfMutsumiCard>(owner),
            "soyo" => combat.CreateCard<PhantomOfSoyoCard>(owner),
            _ => throw new InvalidOperationException("Unknown lethal phantom fixture")
        };
        CardCmd.Upgrade(phantom, CardPreviewStyle.None);
        await CardPileCmd.AddGeneratedCardToCombat(phantom, PileType.Hand, owner);
        var before = owner.Deck.Cards.ToArray();
        await phantom.OnPlayWrapper(context, phantom.TargetType == TargetType.AnyEnemy ? last : null, false, Zero);
        require(last.IsDead, $"{variant} did not kill the final enemy");
        require(before.SequenceEqual(owner.Deck.Cards), $"{variant} added a permanent card after lethal damage");
    }
}
