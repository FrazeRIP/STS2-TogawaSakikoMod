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

internal static class BatchSixteenMechanicsTests
{
    public static async Task RunAsync(PlayerChoiceContext context, CardModel source, Action<bool, string> require)
    {
        var owner = source.Owner; var creature = owner.Creature; var combat = creature.CombatState!;
        foreach (var card in new CardModel[] { ModelDb.Card<TwoMoonsCard>(), ModelDb.Card<TheGirlWithFlaxenHairCard>() })
            require(card.ToMutable().HoverTips.Any(t => t.Id == MegaCrit.Sts2.Core.HoverTips.HoverTipFactory.FromPower<DazzlingPower>().Id), "Dazzling card exposes its explanation");
        require(ModelDb.Card<DivineCard>().ToMutable().HoverTips.Any(t => t.Id == MegaCrit.Sts2.Core.HoverTips.HoverTipFactory.FromPower<MonsterDivinityPower>().Id), "Divine explains Melody Lord");
        require(!ModelDb.Card<SymbolIFireCard>().ToMutable().HoverTips.Any(t => t.Id == MegaCrit.Sts2.Core.HoverTips.HoverTipFactory.FromPower<FireEchoPower>().Id), "Fire omits its own buff explanation");
        var blockTip = MegaCrit.Sts2.Core.HoverTips.HoverTipFactory.Static(MegaCrit.Sts2.Core.HoverTips.StaticHoverTip.Block);
        foreach (var card in new CardModel[] { ModelDb.Card<StayEleganceCard>(), ModelDb.Card<AnglesCard>() })
            require(card.ToMutable().HoverTips.Any(t => t.Id == blockTip.Id), "Block abilities show the native Block explanation");
        var stunTip = MegaCrit.Sts2.Core.MonsterMoves.Intents.StunIntent.GetStaticHoverTip();
        require(ModelDb.Card<SymbolIIIWaterCard>().ToMutable().HoverTips.Any(t => t.Id == stunTip.Id), "Water uses the same stun hover as Whistle");
        foreach (var pair in new (CardModel Card, PowerModel Power)[] {
            (ModelDb.Card<StayEleganceCard>(), ModelDb.Power<StayElegancePower>()),
            (ModelDb.Card<AnglesCard>(), ModelDb.Power<AnglesPower>()),
            (ModelDb.Card<CharismaticFormCard>(), ModelDb.Power<EncorePower>()),
            (ModelDb.Card<SuccubusFormCard>(), ModelDb.Power<CharismaticFormPower>()),
            (ModelDb.Card<AllWillsBecomeOneCard>(), ModelDb.Power<AllWillsBecomeOnePower>()),
            (ModelDb.Card<SweetDreamsCard>(), ModelDb.Power<SweetDreamsPower>()),
            (ModelDb.Card<UnrestrainedCard>(), ModelDb.Power<UnrestrainedPower>()),
            (ModelDb.Card<IdealCard>(), ModelDb.Power<FreeAttackPower>()) })
            require(!pair.Card.ToMutable().HoverTips.OfType<MegaCrit.Sts2.Core.HoverTips.HoverTip>().Any(t => t.Title == pair.Power.Title.GetFormattedText()), "Ability card omits its own ongoing buff: " + pair.Card.Id);

        var enemy = combat.GetOpponentsOf(creature).First(c => c.IsHittable);
        async Task Clear()
        {
            foreach (var p in creature.Powers.OrderBy(p => p is KindnessPower ? 0 : p is ForeverDreamPower ? 1 : 2).ToArray()) await PowerCmd.Remove(p);
            foreach (var c in PileType.Hand.GetPile(owner).Cards.Concat(PileType.Draw.GetPile(owner).Cards).ToArray()) await CardPileCmd.Add(c, PileType.Discard);
        }
        async Task<T> Card<T>(bool up = false) where T : CardModel
        {
            var c = combat.CreateCard<T>(owner); if (up) CardCmd.Upgrade(c, CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(c, PileType.Hand, owner); return c;
        }

        await Clear();
        // New table powers: curse-only free plays, draw replacement, and one-shot collection.
        await (await Card<SweetDreamsCard>()).OnPlayWrapper(context, null, false, Zero);
        await (await Card<SweetDreamsCard>(true)).OnPlayWrapper(context, null, false, Zero);
        var fatigue = await Card<TirednessCard>();
        var dream = creature.GetPower<SweetDreamsPower>()!;
        require(dream.Amount == 5 && dream.TryModifyEnergyCostInCombatLate(fatigue, 7, out var freeCost) && freeCost == 0, "Sweet Dreams stacks and overrides fatigue growth");
        await dream.BeforeCardPlayed(new CardPlay { Card = fatigue, Player = owner, Target = null, ResultPile = PileType.Discard, IsAutoPlay = false, Resources = Zero, PlayIndex = 0, PlayCount = 1 });
        require(dream.Amount == 4, "Sweet Dreams consumes one use per fatigue play");
        await Clear();
        await (await Card<UnrestrainedCard>()).OnPlayWrapper(context, null, false, Zero);
        await (await Card<UnrestrainedCard>(true)).OnPlayWrapper(context, null, false, Zero);
        var unrestrained = creature.GetPower<UnrestrainedPower>()!;
        string unrestrainedText = unrestrained.SmartDescription.GetFormattedText();
        require(unrestrained.SmartDescription.Variables["Draws"].ToString() == "2" && unrestrainedText.Contains("2") && unrestrainedText.Contains("3") && !unrestrainedText.Contains("{") && !unrestrainedText.Contains("}"), "Unrestrained combat tooltip formats stacked draws and Ardor without placeholders");
        var drawA = await Card<DefendTogawaSakiko>(); var drawB = await Card<StrikeTogawaSakiko>();
        await CardPileCmd.Add(drawA, PileType.Draw); await CardPileCmd.Add(drawB, PileType.Draw);
        var desire = await Card<DesireCard>();
        await creature.GetPower<UnrestrainedPower>()!.AfterCardDrawn(context, desire, true);
        require(desire.Pile?.Type == PileType.Exhaust && drawA.Pile?.Type == PileType.Hand && drawB.Pile?.Type == PileType.Hand && creature.GetPower<ArdorPower>()?.Amount == 3, "Unrestrained exhausts once and stacks draws plus Ardor");
        await Clear();
        await (await Card<AllWillsBecomeOneCard>()).OnPlayWrapper(context, null, false, new ResourceInfo { EnergyValue = 1, EnergySpent = 1, StarsSpent = 0, StarValue = 0 });
        var upgradedWills = await Card<AllWillsBecomeOneCard>(true);
        require(upgradedWills.EnergyCost.GetWithModifiers(CostModifiers.All) == 0, "All Wills upgrades to zero energy");
        await upgradedWills.OnPlayWrapper(context, null, false, Zero);
        var wills = creature.GetPower<AllWillsBecomeOnePower>()!;
        require(wills.DisplayAmount == 0, "All Wills excludes itself even when played for zero energy");
        CardModel[] kinds = [combat.CreateCard<StrikeTogawaSakiko>(owner), combat.CreateCard<DefendTogawaSakiko>(owner), combat.CreateCard<DesireCard>(owner), combat.CreateCard<TirednessCard>(owner), combat.CreateCard<InnerCryCard>(owner), combat.CreateCard<KingsCard>(owner), combat.CreateCard<DivineCard>(owner), combat.CreateCard<UnnamedFourCard>(owner), combat.CreateCard<UnnamedFiveCard>(owner), combat.CreateCard<CountingStarsCard>(owner), combat.CreateCard<ProtectionCard>(owner), combat.CreateCard<KindnessCard>(owner)];
        foreach (var card in kinds.Take(9)) await wills.AfterCardPlayed(context, new CardPlay { Player = owner, Target = null, ResultPile = PileType.Discard, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1, Card = card, Resources = Zero });
        await wills.AfterCardPlayed(context, new CardPlay { Player = owner, Target = null, ResultPile = PileType.Discard, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1, Card = kinds[0], Resources = Zero });
        string smartWills = wills.SmartDescription.GetFormattedText();
        require(smartWills.Contains("[gold]9[/gold]") && smartWills.Contains(kinds[0].Title) && !smartWills.Contains("{Count}") && !smartWills.Contains("{Cards}"), "All Wills combat smart tooltip formats gold count and card names");
        require(creature.HasPower<AllWillsBecomeOnePower>() && wills.DisplayAmount == 9 && wills.Description.Variables["Cards"].ToString()!.Contains(kinds[0].Title), "All Wills counts distinct names and lists them");
        await wills.AfterCardPlayed(context, new CardPlay { Player = owner, Target = null, ResultPile = PileType.Discard, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1, Card = kinds[9], Resources = new ResourceInfo { EnergyValue = 1, EnergySpent = 1, StarsSpent = 0, StarValue = 0 } });
        require(creature.HasPower<AllWillsBecomeOnePower>(), "All Wills excludes paid cards");
        await wills.AfterCardPlayed(context, new CardPlay { Player = owner, Target = null, ResultPile = PileType.Discard, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1, Card = kinds[9], Resources = Zero });
        var melodies = PileType.Hand.GetPile(owner).Cards.OfType<MelodyCard>().ToArray();
        require(!creature.HasPower<AllWillsBecomeOnePower>() && melodies.Length == 1 && melodies.All(c => !c.IsUpgraded) && melodies.All(c => !c.Keywords.Contains(CardKeyword.Exhaust)) && melodies.Select(c => c.DynamicVars.Damage.IntValue).Order().SequenceEqual(new[] { 80 }), "All Wills is unique and generates one normal reward then removes itself");
        await Clear();
        await CreatureCmd.Heal(creature, 50);
        await CreatureCmd.GainBlock(enemy, 999, ValueProp.Unpowered, null, fast: true);
        foreach (bool up in new[] { false, true })
        {
            await Clear();
            var four = await Card<UnnamedFourCard>(up);
            decimal fourBefore = enemy.Block;
            await four.OnPlayWrapper(context, enemy, false, Zero);
            require(fourBefore - enemy.Block == (up ? 6 : 4) && creature.GetPower<DazzlingPower>()?.Amount == (up ? 4 : 3) && four.EnergyCost.Canonical == 1, "Unnamed4 damage then Dazzling");
            await Clear();
            var five = await Card<UnnamedFiveCard>(up);
            decimal fiveBefore = enemy.Block;
            int fiveBlock = creature.Block;
            await five.OnPlayWrapper(context, enemy, false, Zero);
            require(fiveBefore - enemy.Block == (up ? 6 : 4) && creature.Block - fiveBlock == (up ? 16 : 14), "Unnamed5 damage then Block");
            await Clear();
            var blood = await Card<GodsBloodCard>(up);
            require(blood.HoverTips.OfType<MegaCrit.Sts2.Core.HoverTips.HoverTip>().Count(t => t.Title?.Contains("重放") == true || t.Title?.Contains("Replay") == true) == 1, "Gods Blood has exactly one replay explanation");
            require(blood.GetEnchantedReplayCount() == (up ? 3 : 2), "Gods Blood uses native replay count");
            var loaded = owner.RunState.LoadCard(blood.ToSerializable(), owner);
            require(loaded.GetEnchantedReplayCount() == (up ? 3 : 2), "Gods Blood replay survives serialization");
            decimal before = enemy.Block;
            await blood.OnPlayWrapper(context, enemy, false, Zero);
            require(before - enemy.Block == 16 * (up ? 4 : 3), "Gods Blood executes native replays");
            await Clear();
            var slash = await Card<SlashTheWayCard>(up); before = enemy.Block;
            await slash.OnPlayWrapper(context, enemy, false, Zero);
            require(before - enemy.Block == (up ? 12 : 9) && creature.GetPower<ArdorPower>()?.Amount == 3, "Slash applies Ardor after all hits");
            await Clear();
            var dance = await Card<HachibouseiDanceCard>(up);
            await CreatureCmd.LoseBlock(context, creature, creature.Block, null);
            await CreatureCmd.GainBlock(creature, 30, ValueProp.Unpowered, null, fast: true);
            before = enemy.Block;
            await dance.OnPlayWrapper(context, enemy, false, Zero);
            require(before - enemy.Block == (up ? 20 : 16) && creature.Block == 30, "Dance attack/self-damage/attack/block sequence");
            await Clear();
            var two = await Card<UnnamedTwoCard>(up); before = creature.Block;
            await two.OnPlayWrapper(context, null, false, Zero);
            require(creature.Block - before == 7 && creature.GetPower<ArdorPower>()?.Amount == (up ? 3 : 2), "Unnamed2 block and Ardor");
            var three = await Card<UnnamedThreeCard>(up);
            await three.OnPlayWrapper(context, null, false, Zero);
            require(three.Keywords.Contains(CardKeyword.Retain) && creature.GetPower<ArdorPower>()?.Amount == (up ? 6 : 4), "Unnamed3 Retain and Ardor");
            await Clear();
            var voice = await Card<VoiceCard>(up);
            await voice.OnPlayWrapper(context, null, false, Zero);
            require(creature.GetPower<MelodiaPower>()?.Amount == (up ? 4 : 3), "Voice grants Mantra once at 3/4 stacks");
            await Clear();
            var baddy = await Card<BaddyBaddyCard>(up); int total = 0; int initial = up ? 5 : 4;
            for (int gain = initial; gain >= 1; gain--)
            {
                await baddy.OnPlayWrapper(context, null, false, Zero); total += gain;
                require(creature.GetPower<ArdorPower>()?.Amount == total && baddy.DynamicVars["MagicNumber"].IntValue == gain - 1, "Baddy decreases per instance without negative gain");
                require(baddy.Pile?.Type == (gain > 1 ? PileType.Hand : PileType.Discard), "Baddy stops returning when gain reaches zero");
            }
            var fresh = await Card<BaddyBaddyCard>(up);
            require(fresh.DynamicVars["MagicNumber"].IntValue == initial, "Baddy reduction does not affect another instance");
            await Clear();
            var heaven = await Card<DarkHeavenCard>(up); await heaven.OnPlayWrapper(context, null, false, Zero);
            heaven = await Card<DarkHeavenCard>(up); await heaven.OnPlayWrapper(context, null, false, Zero);
            var power = creature.GetPower<DarkHeavenPower>()!;
            await power.BeforeSideTurnStart(context, creature.Side, [creature], combat);
            require(creature.GetPower<ArdorPower>()?.Amount == (up ? 8 : 6), "Dark Heaven stacks start-of-turn Ardor");
            await Clear();
        }
        foreach (int protection in new[] { 0, 1, 2 })
        {
            bool protect = protection > 0;
            await PowerCmd.Apply<ArdorPower>(context, creature, 7, creature, source);
            if (protection == 1) await PowerCmd.Apply<KindnessPower>(context, creature, 1, creature, source);
            if (protection == 2) { await PowerCmd.Apply<HypePower>(context, creature, 3, creature, source); await PowerCmd.Apply<ForeverDreamPower>(context, creature, 1, creature, source); }
            var soft = await Card<SoftnessCard>(); await soft.OnPlayWrapper(context, null, false, Zero);
            require((creature.GetPower<ArdorPower>()?.Amount ?? 0) == (protect ? 7 : 0), "Softness respects positive-effect protection");
            var pending = creature.GetPower<NextTurnArdorPower>()!;
            await pending.BeforeSideTurnStart(context, creature.Side, [creature], combat);
            require(creature.GetPower<ArdorPower>()?.Amount == (protect ? 14 : 7), "Softness gains snapshot amount even if loss was blocked");
            await pending.BeforeSideTurnStart(context, creature.Side, [creature], combat);
            require(creature.GetPower<ArdorPower>()?.Amount == (protect ? 14 : 7), "Protected delayed buff does not repeat payout");
            if (protection == 1) await PowerCmd.Remove(creature.GetPower<KindnessPower>()!);
            await Clear();
        }
        foreach (bool up in new[] { false, true })
        {
            foreach (var old in PileType.Draw.GetPile(owner).Cards.ToArray()) await CardPileCmd.Add(old, PileType.Discard);
            var originals = new List<CardModel>();
            for (int i = 0; i < 5; i++)
            {
                var original = await Card<StrikeTogawaSakiko>(i % 2 == 0);
                await CardPileCmd.Add(original, PileType.Draw, CardPilePosition.Bottom);
                originals.Add(original);
            }
            var sophie = await Card<SophieCard>(up); int deck = owner.Deck.Cards.Count;
            await sophie.OnPlayWrapper(context, null, false, Zero);
            var copies = PileType.Hand.GetPile(owner).Cards.OfType<StrikeTogawaSakiko>().ToArray();
            require(copies.Length == (up ? 4 : 3) && copies.Select(c => c.IsUpgraded).SequenceEqual(originals.Take(up ? 4 : 3).Select(c => c.IsUpgraded)) && copies.All(c => !originals.Contains(c) && c.DeckVersion is null && !c.Keywords.Contains(CardKeyword.Retain)), "Sophie copies top cards in order without adding Retain or deck links");
            require(PileType.Draw.GetPile(owner).Cards.SequenceEqual(originals) && owner.Deck.Cards.Count == deck && sophie.Pile?.Type == PileType.Exhaust, "Sophie preserves draw pile and permanent deck and exhausts");
            await Clear();
        }
        foreach (bool up in new[] { false, true })
        {
            await Clear();
            var newCard = await Card<UnnamedHypeCard>(up);
            await newCard.OnPlayWrapper(context, null, false, Zero);
            require(creature.GetPower<HypePower>()?.Amount == (up ? 3 : 2) && ReferenceEquals(PileType.Draw.GetPile(owner).Cards.First(), newCard), "Unnamed1 grants Hype and returns to draw top");
            await Clear();
            var kiss = await Card<KillKiSSCard>(up);
            var kissDeckBefore = owner.Deck.Cards.ToHashSet();
            using (CardSelectCmd.PushSelector(new FixedSelector())) await kiss.OnPlayWrapper(context, enemy, false, Zero);
            var added = owner.Deck.Cards.Except(kissDeckBefore).ToArray();
            require(added.Length == 1 && added[0] is DesireCard && added[0].IsUpgraded == up && kiss.Pile?.Type == PileType.Exhaust, "Kill Kiss adds one Desire with matching upgrade and exhausts");
        }
        await Clear();
        foreach (bool worldview in new[] { false, true })
        {
            foreach (var kind in new[] { typeof(TimorisCard), typeof(AmorisCard), typeof(DolorisCard), typeof(MortisCard), typeof(OblivionisCard) })
            {
                await Clear();
                if (worldview) { var w = await Card<WorldviewCard>(); await w.OnPlayWrapper(context, null, false, Zero); }
                await CreatureCmd.LoseBlock(context, creature, creature.Block, null);
                await CreatureCmd.GainBlock(creature, 20, ValueProp.Unpowered, null, fast: true);
                var held = await Card<DefendTogawaSakiko>();
                CardModel curse = kind == typeof(TimorisCard) ? combat.CreateCard<TimorisCard>(owner) : kind == typeof(AmorisCard) ? combat.CreateCard<AmorisCard>(owner) : kind == typeof(DolorisCard) ? combat.CreateCard<DolorisCard>(owner) : kind == typeof(MortisCard) ? combat.CreateCard<MortisCard>(owner) : combat.CreateCard<OblivionisCard>(owner);
                await CardPileCmd.Add(curse, PileType.Draw); int hp = creature.CurrentHp;
                await CardPileCmd.Draw(context, 1, owner);
                if (worldview)
                    require(creature.CurrentHp == hp && creature.Block == 20 && !creature.HasPower<FrailPower>() && !creature.HasPower<WeakPower>() && !held.Keywords.Contains(CardKeyword.Ethereal) && !owner.PlayerCombatState!.AllCards.Contains(curse), "Worldview transforms each element curse before penalty");
                else if (kind == typeof(TimorisCard)) require(creature.GetPower<FrailPower>()?.Amount == 1, "Timoris draw Frail");
                else if (kind == typeof(AmorisCard)) require(creature.GetPower<WeakPower>()?.Amount == 1, "Amoris draw Weak");
                else if (kind == typeof(DolorisCard)) require(creature.CurrentHp == hp-2 && creature.Block == 20, "Doloris draw HP loss bypasses block");
                else if (kind == typeof(MortisCard)) require(creature.Block == 0, "Mortis draw clears block");
                else
                {
                    require(!held.Keywords.Contains(CardKeyword.Ethereal), "Oblivionis no longer adds Ethereal on draw");
                    await curse.OnTurnEndInHandWrapper(context);
                    require(held.Pile?.Type == PileType.Exhaust && curse.Pile?.Type == PileType.Exhaust && PileType.Hand.GetPile(owner).Cards.Count == 0, "Oblivionis exhausts all held cards including itself at turn end");
                }
            }
        }
        await Clear();
        var thirds = new List<CardModel>();
        for (int i=0;i<3;i++) thirds.Add((await PersistentDeckMutation.AddCanonicalAsync<DefendTogawaSakiko>(owner, skipVisuals:true)).cardAdded);
        var third = await Card<TheThirdMovementCard>(true); decimal blockBefore=enemy.Block;
        using (CardSelectCmd.PushSelector(new FixedSelector(thirds.ToArray()))) await third.OnPlayWrapper(context, enemy, false, Zero);
        require(thirds.All(c=>!owner.Deck.Cards.Contains(c)) && blockBefore-enemy.Block==30 && third.EnergyCost.GetWithModifiers(CostModifiers.None)==0, "Third Movement removes three and deals thirty; upgrade zero cost");
        await Clear();
        for (int i = 0; i < 7; i++) {
            var setup = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<DefendTogawaSakiko>(owner, skipPersistentVisuals: true);
            await CardPileCmd.Add(setup.CombatCard!, PileType.Discard);
        }
        var fool=await Card<MementoMoriCard>(); int beforeDeck=owner.Deck.Cards.Count;
        using (CardSelectCmd.PushSelector(new FixedSelector())) await fool.OnPlayWrapper(context,enemy,false,Zero);
        require(owner.Deck.Cards.Count==beforeDeck && !creature.HasPower<MonsterDivinityPower>(), "Memento play only deals damage without purging or entering Divinity");
        foreach (bool up in new[] { false, true })
        {
            await Clear();
            var removable = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<MementoMoriCard>(owner, upgradeLevel: up ? 1 : 0, skipPersistentVisuals: true);
            await CardPileCmd.Add(removable.CombatCard!, PileType.Discard);
            require(removable.PersistentCard!.EnergyCost.GetWithModifiers(CostModifiers.None) == 3 && removable.PersistentCard.DynamicVars.Damage.BaseValue == 30, "Memento both versions cost three and deal thirty");
            for (int drawIndex = 0; drawIndex < (up ? 4 : 2); drawIndex++)
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<DefendTogawaSakiko>(owner), PileType.Draw, owner);
            int handBefore = PileType.Hand.GetPile(owner).Cards.Count;
            var mementoPurge = await SakikoPurgeCommand.RemoveAsync(removable.PersistentCard!, showPersistentPreview: false, skipCombatVisuals: true);
            require(mementoPurge.Success && creature.HasPower<MonsterDivinityPower>() && PileType.Hand.GetPile(owner).Cards.Count == handBefore + (up ? 4 : 2), $"Memento successful permanent removal enters stance and draws matching upgrade: success={mementoPurge.Success}; reason={mementoPurge.FailureReason}; stance={creature.HasPower<MonsterDivinityPower>()}; hand={PileType.Hand.GetPile(owner).Cards.Count}/{handBefore}; upgraded={up}");
        }
        await Clear();
    }
}

