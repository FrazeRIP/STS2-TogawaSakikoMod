using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using static TogawaSakiko.NativeCode.Diagnostics.MechanicsTestSupport;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class BatchTenMechanicsTests
{
    public static async Task RunAsync(PlayerChoiceContext context, CardModel source, Action<bool, string> require)
    {
        var owner = source.Owner;
        var creature = owner.Creature;
        var combat = creature.CombatState!;
        async Task Clear()
        {
            foreach (var p in creature.Powers.ToArray()) await PowerCmd.Remove(p);
            foreach (var c in PileType.Hand.GetPile(owner).Cards.ToArray()) await CardPileCmd.Add(c, PileType.Discard);
        }
        async Task<T> Play<T>(bool upgrade = false) where T : CardModel
        {
            var c = combat.CreateCard<T>(owner);
            if (upgrade) CardCmd.Upgrade(c, CardPreviewStyle.None);
            await CardPileCmd.AddGeneratedCardToCombat(c, PileType.Hand, owner);
            await c.OnPlayWrapper(context, null, false, Zero);
            return c;
        }
        await Clear();
        int block = creature.Block;
        await Play<StayEleganceCard>();
        require(creature.Block == block, "Elegance first application must not trigger itself");
        await Play<StayEleganceCard>(true);
        require(creature.Block == block + 1 && creature.GetPower<StayElegancePower>()?.Amount == 3, "Elegance stacking uses previous amount");
        await PowerCmd.Apply<DexterityPower>(context, creature, 20, creature, source);
        await PowerCmd.Apply<FrailPower>(context, creature, 2, creature, source);
        block = creature.Block;
        await Play<ProtectionCard>();
        require(creature.Block == block + 6 && creature.GetPower<HypePower>()?.Amount == 2, "Follow grants Hype twice; Elegance ignores Dexterity/Frail");
        await Play<ProtectionCard>(true);
        require(creature.Block == block + 12 && creature.GetPower<HypePower>()?.Amount == 4, "Follow+ preserves two separate triggers");
        block = creature.Block;
        await PowerCmd.ModifyAmount(context, creature.GetPower<HypePower>()!, -1, creature, source);
        require(creature.Block == block, "Buff loss must not trigger Elegance");
        foreach (bool upgrade in new[] { false, true })
        {
            await Clear();
            await PowerCmd.Apply<ArdorPower>(context, creature, 4, creature, source);
            for (int i = 0; i < 3; i++)
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<StrikeTogawaSakiko>(owner), PileType.Draw, owner);
            await Play<DivineCard>(upgrade);
            require(creature.GetPower<MelodiaPower>()?.Amount == 4 && creature.GetPower<ArdorPower>()?.Amount == 4, "Divine copies Ardor without consuming it");
            require(PileType.Hand.GetPile(owner).Cards.Count == (upgrade ? 2 : 1), "Divine draw count");
        }
        await Clear();
        await Play<DivineCard>();
        require(!creature.HasPower<MelodiaPower>() && PileType.Hand.GetPile(owner).Cards.Count == 1, "Divine draws even with zero Ardor");
        await Clear();
        var pair = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<FallenFlowersCard>(owner, skipPersistentVisuals: true);
        var run = (FallenFlowersCard)pair.PersistentCard!;
        var copy = (FallenFlowersCard)pair.CombatCard!;
        await copy.OnPlayWrapper(context, null, false, Zero);
        require(creature.GetPower<DazzlingPower>()?.Amount == 6, "Fallen Flowers grants six Dazzling");
        await copy.AfterCombatEnd(null!);
        require(copy.CombatsSeen == 0 && run.CombatsSeen == 0, "Only permanent deck cards count combats");
        await run.AfterCombatEnd(null!);
        await run.AfterCombatEnd(null!);
        require(run.CombatsSeen == 2 && run.DynamicVars["CombatsRemaining"].IntValue == 1 && owner.Deck.Cards.Contains(run), "Fallen Flowers countdown");
        var restored = (FallenFlowersCard)owner.RunState.LoadCard(run.ToSerializable(), owner);
        require(restored.CombatsSeen == 2 && restored.DynamicVars["CombatsRemaining"].IntValue == 1, "Fallen Flowers saved countdown");
        await run.AfterCombatEnd(null!);
        require(!owner.Deck.Cards.Contains(run), "Fallen Flowers automatically removed after third combat");
        var permanent = (FallenFlowersCard)(await PersistentDeckMutation.AddCanonicalAsync<FallenFlowersCard>(owner, skipVisuals: true)).cardAdded;
        permanent.CombatsSeen = 2;
        CardCmd.Upgrade(permanent, CardPreviewStyle.None);
        for (int i = 0; i < 4; i++) await permanent.AfterCombatEnd(null!);
        require(owner.Deck.Cards.Contains(permanent) && permanent.CombatsSeen == 2, "Upgraded Fallen Flowers no longer expires");
        await Clear();
    }
}
