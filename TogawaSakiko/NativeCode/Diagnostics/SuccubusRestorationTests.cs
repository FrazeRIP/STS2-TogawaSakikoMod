using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Models.Pools;
using static TogawaSakiko.NativeCode.Diagnostics.MechanicsTestSupport;

namespace TogawaSakiko.NativeCode.Diagnostics;
internal static class SuccubusRestorationTests
{
    internal static async Task RunAsync(PlayerChoiceContext context, CardModel source, Action<bool, string> require)
    {
        var player = source.Owner;
        var owner = player.Creature;
        var combat = owner.CombatState!;
        var enemies = combat.GetOpponentsOf(owner).Where(e => e.CanReceivePowers).ToArray();
        async Task Clear()
        {
            if (owner.GetPower<KindnessPower>() is { } kindness) await PowerCmd.Remove(kindness);
            if (owner.GetPower<ForeverDreamPower>() is { } dream) await PowerCmd.Remove(dream);
            foreach (var creature in enemies.Prepend(owner))
                foreach (var power in creature.Powers.ToArray()) await PowerCmd.Remove(power);
            foreach (var card in PileType.Hand.GetPile(player).Cards.ToArray()) await CardPileCmd.Add(card, PileType.Discard);
        }
        var tome = (MegaCrit.Sts2.Core.Models.Relics.DustyTome)ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.DustyTome>().ToMutable();
        tome.SetupForPlayer(player);
        require(tome.AncientCard == ModelDb.Card<SuccubusFormCard>().Id, "Dusty Tome selects restored Succubus instead of Fateweaver");
        var savedTome = (MegaCrit.Sts2.Core.Models.Relics.DustyTome)RelicModel.FromSerializable(tome.ToSerializable());
        require(savedTome.AncientCard == tome.AncientCard, "Dusty Tome preserves Succubus selection through save round-trip");
        require(ModelDb.Card<CharismaticFormCard>().Id != ModelDb.Card<SuccubusFormCard>().Id && ModelDb.Card<CharismaticFormCard>().Rarity == CardRarity.Rare, "Fateweaver retains its distinct identity and Rare rarity");
        foreach (bool upgraded in new[] { false, true })
        {
            await Clear();
            var card = combat.CreateCard<SuccubusFormCard>(player);
            if (upgraded) CardCmd.Upgrade(card, CardPreviewStyle.None);
            require(card.Rarity == CardRarity.Ancient && card.Keywords.Contains(CardKeyword.Innate) && card.EnergyCost.GetWithModifiers(CostModifiers.None) == 3 && !card.CanBeGeneratedInCombat && !card.CanBeGeneratedByModifiers && TogawaSakikoCardPool.IsVisibleInLibrary(card), "Succubus is a visible three-cost Innate Ancient with no generic generation");
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            await card.OnPlayWrapper(context, null, false, Zero);
            var restored = CardModel.FromSerializable(card.ToSerializable());
            require(restored is SuccubusFormCard && restored.IsUpgraded == upgraded && restored.Keywords.Contains(CardKeyword.Innate), "Succubus card saves preserve identity, upgrade, and Innate");
            int hype = upgraded ? 3 : 2;
            require(enemies.All(e => e.GetPower<HypePower>()?.Amount == hype) && owner.GetPower<HypePower>()?.Amount == hype * enemies.Length, "Succubus copies each enemy Hype grant, with upgrade increasing it to three");
            var enemy = enemies[0];
            await PowerCmd.Apply<StrengthPower>(context, enemy, 3, enemy, null);
            await PowerCmd.Apply<StrengthPower>(context, enemy, 2, enemy, null);
            require(owner.GetPower<StrengthPower>()?.Amount == 5, "Succubus copies only gained stacks rather than the enemy total");
            await PowerCmd.Apply<VigorPower>(context, enemy, 4, enemy, null);
            require(owner.GetPower<VigorPower>()?.Amount == 4 && owner.GetPower<VigorPower>()!.Owner == owner, "Succubus preserves reset-safe Vigor copy ownership");
            await PowerCmd.Apply<BackAttackLeftPower>(context, enemy, 1, enemy, null);
            await PowerCmd.Apply<AnglesPower>(context, enemy, 1, enemy, null);
            await PowerCmd.Apply<WeakPower>(context, enemy, 1, enemy, null);
            require(!owner.HasPower<BackAttackLeftPower>() && !owner.HasPower<AnglesPower>() && !owner.HasPower<WeakPower>(), "Succubus retains excluded-enemy, unsupported-private-state, and debuff filters");
            await PowerCmd.Apply<CharismaticFormPower>(context, owner, 1, owner, source);
            await PowerCmd.Apply<DexterityPower>(context, enemy, 1, enemy, null);
            require(owner.GetPower<DexterityPower>()?.Amount == 2, "Repeated Succubus doubles copied gains without recursive self-copy");
        }
        await Clear();
    }
}
