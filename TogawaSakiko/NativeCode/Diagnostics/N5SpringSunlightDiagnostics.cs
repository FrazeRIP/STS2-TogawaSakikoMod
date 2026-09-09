using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static partial class N5BatchDiagnostics
{
    private static async Task VerifySpringSunlightStartupAsync(
        CombatState combatState, Player player, Creature target, PlayerChoiceContext choiceContext)
    {
        PlayerCombatState piles = player.PlayerCombatState!;
        CardModel[] originalDeck = player.Deck.Cards.ToArray();
        CombatCardLocation[] moved = await MoveCombatPilesAsideAsync(
            player, PileType.Hand, PileType.Draw, PileType.Discard);
        HashSet<CardModel> originalCombatCards = piles.AllCards.ToHashSet();
        int originalEnergy = piles.Energy;
        decimal originalBlock = target.Block;
        foreach (CardModel card in originalDeck)
        {
            card.RemoveFromCurrentPile();
        }

        try
        {
            await CreatureCmd.GainBlock(target, 1000m, ValueProp.Unpowered, null, fast: true);
            foreach ((int size, int expected) in new[] { (5, 0), (6, 1), (11, 1), (12, 2) })
            {
                for (int index = 0; index < size; index++)
                {
                    CardModel canonical = index < 2
                        ? ModelDb.Card<SpringSunlightCard>()
                        : ModelDb.Card<StrikeTogawaSakiko>();
                    CardModel persistent = player.RunState.CreateCard(canonical, player);
                    if (index == 1)
                    {
                        CardCmd.Upgrade(persistent, CardPreviewStyle.None);
                    }
                    player.Deck.AddInternal(persistent);
                }

                // Use the actual startup path: CloneCard + DrawPile.AddInternal.
                // No generated-card command or direct Spring Sunlight refresh is allowed here.
                player.PopulateCombatState(player.RunState.Rng.Shuffle, combatState);
                SpringSunlightCard[] springs = piles.DrawPile.Cards.OfType<SpringSunlightCard>()
                    .OrderBy(card => card.IsUpgraded).ToArray();
                Require(springs.Length == 2 && springs.All(card => card.DeckVersion is SpringSunlightCard),
                    "Spring Sunlight startup fixture did not clone both persistent cards");
                await Hook.BeforeCombatStart(player.RunState, combatState);
                Require(springs.All(card => card.EnergyCost.GetWithModifiers(CostModifiers.None) == expected),
                    $"Spring Sunlight initial Draw cost was incorrect for deck size {size}");

                await AddDrawWindowAsync(springs);
                foreach (SpringSunlightCard spring in springs)
                {
                    Require(ReferenceEquals(await CardPileCmd.Draw(choiceContext, player), spring),
                        "Spring Sunlight was not drawn through the native draw command");
                    Require(spring.EnergyCost.GetWithModifiers(CostModifiers.All) == expected,
                        $"Spring Sunlight first-hand cost was incorrect for deck size {size}");
                    VerifySpringSunlightCostLabel(spring, expected);
                    piles.Energy = Math.Max(0, expected - 1);
                    if (expected > 0)
                    {
                        Require(!spring.CanPlay(out UnplayableReason reason, out _) &&
                                reason.HasFlag(UnplayableReason.EnergyCostTooHigh),
                            "Spring Sunlight allowed play with insufficient energy");
                    }
                    piles.Energy = expected;
                    Require(spring.CanPlay(), "Spring Sunlight rejected its exact energy cost");
                    (int energy, int stars) = await spring.SpendResources();
                    Require(energy == expected && piles.Energy == 0,
                        "Spring Sunlight spent the wrong energy before OnPlay");
                    await spring.OnPlayWrapper(choiceContext, target, isAutoPlay: false, new ResourceInfo
                    {
                        EnergySpent = energy, EnergyValue = energy, StarsSpent = stars, StarValue = stars
                    });
                    Require(spring.Pile?.Type == PileType.Discard,
                        "Spring Sunlight normal play did not finish in Discard");
                }

                SpringSunlightCard discounted = springs[0];
                discounted.EnergyCost.SetUntilPlayed(0);
                // Change the persistent size without notifying hooks to isolate hand-entry refresh.
                for (int index = 0; index < 6; index++)
                {
                    player.Deck.AddInternal(player.RunState.CreateCard(ModelDb.Card<StrikeTogawaSakiko>(), player));
                }
                await AddDrawWindowAsync([discounted]);
                Require(ReferenceEquals(await CardPileCmd.Draw(choiceContext, player), discounted),
                    "Spring Sunlight redraw did not return the expected card");
                Require(discounted.EnergyCost.GetWithModifiers(CostModifiers.None) == expected + 1 &&
                        discounted.EnergyCost.GetWithModifiers(CostModifiers.All) == 0 &&
                        springs[1].EnergyCost.GetWithModifiers(CostModifiers.None) == expected + 1,
                    "Spring Sunlight hand entry did not refresh all copies while preserving its discount");
                VerifySpringSunlightCostLabel(discounted, 0);

                await RemoveCombatCardsAsync(piles.AllCards.Where(card => !originalCombatCards.Contains(card)).ToArray());
                foreach (CardModel card in player.Deck.Cards.ToArray())
                {
                    card.RemoveFromCurrentPile();
                    card.RemoveFromState();
                }
            }
        }
        finally
        {
            await RemoveCombatCardsAsync(piles.AllCards.Where(card => !originalCombatCards.Contains(card)).ToArray());
            foreach (CardModel card in player.Deck.Cards.ToArray())
            {
                card.RemoveFromCurrentPile();
                card.RemoveFromState();
            }
            foreach (CardModel card in originalDeck)
            {
                player.Deck.AddInternal(card);
            }
            piles.Energy = originalEnergy;
            await RemoveAddedBlockAsync(choiceContext, target, originalBlock);
            await RestoreCombatCardsAsync(moved);
        }
        NativeSmokeTrace.N5Info("Spring Sunlight startup passed. PersistentClone=5:0+6:1+11:1+12:2, BaseAndUpgrade=true, NativeDraw=true, CostLabel=true, ManualEnergy=true, RedrawAndDiscount=true.");
    }

    private static void VerifySpringSunlightCostLabel(SpringSunlightCard card, int expected)
    {
        NCard node = NCard.FindOnTable(card)
            ?? throw new InvalidOperationException("Spring Sunlight hand card has no native UI node.");
        node.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
        Require(node.GetNode<MegaLabel>("%EnergyLabel").Text == expected.ToString(),
            $"Spring Sunlight native energy label did not display {expected}");
    }
}
