using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.TestSupport;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using static TogawaSakiko.NativeCode.Diagnostics.MechanicsTestSupport;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class BatchTwelveMechanicsTests
{
    private sealed class Select(CardModel[] selected, int maximum, Action<bool, string> require) : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            if (maximum == 0)
            {
                var pool = options.ToArray();
                require(pool.Length == 4 && pool.All(c => c is not EtherCard) && minSelect == 1 && maxSelect == 1, "Ave displays four elements and selects one");
                return Task.FromResult<IEnumerable<CardModel>>([pool[0]]);
            }
            require(minSelect == 0 && maxSelect == maximum && selected.All(options.Contains), "Sora selection cap, optionality and candidates");
            return Task.FromResult<IEnumerable<CardModel>>(selected);
        }
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives)
        {
            require(options.Count == 4 && options.All(o => o.Card is not EtherCard), "Ave displays four elements without Ether");
            return new() { card = options[0].Card };
        }
    }
    public static async Task RunAsync(PlayerChoiceContext context, CardModel source, Action<bool, string> require)
    {
        var owner = source.Owner;
        var creature = owner.Creature;
        var combat = creature.CombatState!;
        async Task Clear()
        {
            foreach (var p in creature.Powers.ToArray()) await PowerCmd.Remove(p);
            foreach (var c in PileType.Hand.GetPile(owner).Cards.Concat(PileType.Draw.GetPile(owner).Cards).ToArray()) await CardPileCmd.Add(c, PileType.Discard);
        }
        async Task<T> Card<T>(bool upgraded = false) where T : CardModel
        {
            var c = combat.CreateCard<T>(owner);
            if (upgraded) CardCmd.Upgrade(c, CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(c, PileType.Hand, owner);
            return c;
        }
        await Clear();
        var growth = new List<(CardModel Run, CardModel Copy, int Expected)>();
        foreach (var pile in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Play })
        {
            bool upgraded = pile == PileType.Exhaust;
            var pair = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<MasqueradeRhapsodyRequestCard>(owner, skipPersistentVisuals: true, upgradeLevel: upgraded ? 1 : 0);
            await CardPileCmd.Add(pair.CombatCard!, pile);
            growth.Add((pair.PersistentCard!, pair.CombatCard!, upgraded ? 3 : 2));
        }
        var absent = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<MasqueradeRhapsodyRequestCard>(owner, skipPersistentVisuals: true);
        await CardPileCmd.RemoveFromCombat(absent.CombatCard!);
        var duplicate = combat.CloneCard(growth[0].Copy);
        await CardPileCmd.AddGeneratedCardToCombat(duplicate, PileType.Hand, owner);
        var fatigue = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<TirednessCard>(owner, skipPersistentVisuals: true);
        await fatigue.CombatCard!.OnPlayWrapper(context, null, false, Zero);
        foreach (var (run, copy, expected) in growth)
            require(run.DynamicVars.Damage.IntValue == expected && copy.DynamicVars.Damage.IntValue == expected, "Masquerade grows once per permanent card across every combat pile");
        require(absent.PersistentCard!.DynamicVars.Damage.IntValue == 2, "Masquerade grows without any combat instance");
        var restored = owner.RunState.LoadCard(growth[3].Run.ToSerializable(), owner);
        require(restored.DynamicVars.Damage.IntValue == 3, "Masquerade growth survives save/load");
        // The fixture left a card in Play; move it back before later plays.
        await CardPileCmd.Add(growth[4].Copy, PileType.Discard);
        foreach (bool upgraded in new[] { false, true })
        {
            await Clear();
            var draw = new List<CardModel>();
            for (int i = 0; i < 8; i++)
            {
                var c = combat.CreateCard<DefendTogawaSakiko>(owner);
                await CardPileCmd.AddGeneratedCardToCombat(c, PileType.Draw, owner);
                draw.Add(c);
            }
            var sora = await Card<SoraNoMusicaCard>(upgraded);
            using (CardSelectCmd.PushSelector(new Select([draw[1], draw[4]], upgraded ? 6 : 4, require)))
                await sora.OnPlayWrapper(context, null, false, Zero);
            require(owner.Creature.GetPower<MegaCrit.Sts2.Core.Models.Powers.EnergyNextTurnPower>()?.Amount == 1, "Sora grants one next-turn energy");
            var drawn = (await CardPileCmd.Draw(context, 2, owner)).ToArray();
            require(drawn.SequenceEqual(new[] { draw[1], draw[4] }) && sora.Pile?.Type == PileType.Exhaust, "Sora preserves chosen draw order with Exhaust");
            sora = await Card<SoraNoMusicaCard>(upgraded);
            var oldOrder = PileType.Draw.GetPile(owner).Cards.ToArray();
            using (CardSelectCmd.PushSelector(new Select([], Math.Min(upgraded ? 6 : 4, oldOrder.Length), require)))
                await sora.OnPlayWrapper(context, null, false, Zero);
            require(PileType.Draw.GetPile(owner).Cards.SequenceEqual(oldOrder), "Sora permits choosing zero");
            await Clear();
            var ave = await Card<AveMujicaCard>(upgraded);
            var previews = TogawaSakiko.NativeCode.Presentation.SakikoCardHoverTips.CreateVariantPreviews(true, upgraded);
            require(previews.Count == 4 && previews.All(c => c is not EtherCard && c.IsUpgraded == upgraded), "Ave hover previews match four options");
            var before = owner.Deck.Cards.ToHashSet();
            using (CardSelectCmd.PushSelector(new Select([], 0, require))) await ave.OnPlayWrapper(context, null, false, Zero);
            var added = owner.Deck.Cards.Except(before).ToArray();
            require(added.Length == 1 && added[0] is not EtherCard && added[0].IsUpgraded == upgraded, "Ave adds one selected permanent element with correct upgrade");
            require(PileType.Hand.GetPile(owner).Cards.Count == 1 && ReferenceEquals(PileType.Hand.GetPile(owner).Cards.Single().DeckVersion, added[0]) && ave.Pile?.Type == PileType.Exhaust,
                "Ave creates exactly one linked hand instance, does not autoplay, and exhausts");
        }
        await Clear();
        var form = await Card<CharismaticFormCard>();
        await form.OnPlayWrapper(context, null, false, Zero);
        require(creature.GetPower<EncorePower>()?.Amount == 1 && !creature.HasPower<HypePower>() && !creature.HasPower<CharismaticFormPower>(), "Form grants Encore without old curse effects");
        form = await Card<CharismaticFormCard>(true);
        require(form.Keywords.Contains(CardKeyword.Innate) && !form.Keywords.Contains(CardKeyword.Retain), "Form upgrade grants Innate");
        await form.OnPlayWrapper(context, null, false, Zero);
        require(creature.GetPower<EncorePower>()?.Amount == 2, "Form stacks matching-card energy");
        await Clear();
    }
}
