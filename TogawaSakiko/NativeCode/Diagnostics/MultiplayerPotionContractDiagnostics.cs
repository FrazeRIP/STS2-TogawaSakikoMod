using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Potions;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Diagnostics;

// Runs only inside the recorded, replicated combat diagnostic action.
internal static class MultiplayerPotionContractDiagnostics
{
    internal static async Task RunAsync(PlayerChoiceContext context, CardModel source)
    {
        ICombatState combat = source.Owner.Creature.CombatState
            ?? throw new InvalidOperationException("Potion contracts require active combat.");
        Player[] players = combat.Players.ToArray();
        PotionModel[] canonicalPotions = [ModelDb.Potion<ChocolateMilkJelly>(), ModelDb.Potion<EarlGreyTea>(),
            ModelDb.Potion<FreshlySqueezedCucumber>(), ModelDb.Potion<HallucinationPotion>(),
            ModelDb.Potion<MatchaParfait>(), ModelDb.Potion<OrangeMilkJelly>()];
        Require(players.Length > 1, "a multiplayer party is required");
        Require(players.All(player => player.Creature.IsAlive && player.HasOpenPotionSlots &&
            !player.Creature.Powers.Any(IsPotionPower)),
            "fixture requires living owners, an open potion slot, and no existing potion powers");

        int used = 0;
        // Party slot order survives native replay anonymization of player IDs.
        foreach (Player owner in players)
        {
            foreach (PotionModel canonical in canonicalPotions)
            {
                Dictionary<Player, string> before = players.ToDictionary(player => player, Snapshot);
                Dictionary<Player, int> historyBefore = players.ToDictionary(player => player, PotionUseCount);
                CardModel[] originalCards = owner.PlayerCombatState!.AllCards.ToArray();
                PotionModel?[] originalSlots = owner.PotionSlots.ToArray();
                PotionModel potion = canonical.ToMutable();
                Require((await PotionCmd.TryToProcure(potion, owner)).success,
                    $"{potion.Id} could not enter owner {owner.NetId}'s inventory");
                Require(potion.Owner == owner && potion.TargetType == TargetType.Self &&
                    potion.IsValidTarget(owner.Creature) &&
                    players.Where(player => player != owner).All(player => !potion.IsValidTarget(player.Creature)),
                    $"{potion.Id} did not restrict its native target to its owner");
                int generationCounter = owner.RunState.Rng.CombatCardGeneration.ToSerializable().counter;
                // This is the same wrapper that native UsePotionAction invokes after target validation.
                // It removes the owned slot, runs the branching effect context, hooks, and use history.
                await potion.OnUseWrapper(context, owner.Creature);
                Require(potion.HasBeenRemovedFromState && !owner.Potions.Contains(potion) &&
                    owner.PotionSlots.SequenceEqual(originalSlots),
                    $"{potion.Id} did not consume exactly its owner's inventory slot");
                Require(PotionUseCount(owner) == historyBefore[owner] + 1,
                    $"{potion.Id} did not record exactly one owner potion use");

                PowerModel[] gained = owner.Creature.Powers.Where(IsPotionPower).ToArray();
                CardModel[] generated = owner.PlayerCombatState.AllCards.Except(originalCards).ToArray();
                VerifyEffect(potion, owner, gained, generated);
                Require(owner.RunState.Rng.CombatCardGeneration.ToSerializable().counter ==
                    generationCounter + (potion is MatchaParfait ? 1 : 0),
                    $"{potion.Id} advanced card-generation RNG unexpectedly");
                foreach (Player teammate in players.Where(player => player != owner))
                {
                    Require(Snapshot(teammate) == before[teammate] &&
                        PotionUseCount(teammate) == historyBefore[teammate],
                        $"{potion.Id} crossed from owner {owner.NetId} into teammate {teammate.NetId}");
                }

                // Only immediate potion effects are tested. In particular, Cucumber's later-turn
                // block and Hallucination's expiration are not claimed by this consumption probe.
                foreach (PowerModel power in gained)
                {
                    await PowerCmd.Remove(power);
                }
                if (generated.Length > 0)
                {
                    await CardPileCmd.RemoveFromCombat(generated, skipVisuals: true);
                }
                Require(Snapshot(owner) == before[owner],
                    $"{potion.Id} left a gameplay fixture effect after cleanup");
                used++;
            }
        }
        GD.Print($"Multiplayer potion contracts PASSED: {used} native uses, all six potions for {players.Length} owners; consumption, self-targets, immediate effects, teammate isolation.");
    }

    private static void VerifyEffect(PotionModel potion, Player owner, PowerModel[] gained, CardModel[] generated)
    {
        bool matches = potion switch
        {
            ChocolateMilkJelly => gained is [FreeAttackPower { Amount: 1 }],
            EarlGreyTea => gained is [DazzlingPower { Amount: 2 }],
            FreshlySqueezedCucumber => gained is [FreshlySqueezedCucumberPower { Amount: 20 }],
            HallucinationPotion => gained.Length == 2 && gained.OfType<DazzlingPower>().SingleOrDefault()?.Amount == 5 &&
                gained.OfType<DazzlingDownPower>().SingleOrDefault()?.Amount == 5,
            MatchaParfait => gained.Length == 0 && generated is [MelodyCard melody] &&
                melody.Owner == owner && melody.Pile?.Type is PileType.Hand or PileType.Discard &&
                MatchaParfait.IsDamageInRange(melody.DynamicVars.Damage.BaseValue),
            OrangeMilkJelly => gained is [OneTwoPunchPower { Amount: 1 }],
            _ => false
        };
        Require(matches && gained.All(power => power.Owner == owner.Creature) &&
            (potion is MatchaParfait || generated.Length == 0),
            $"{potion.Id} immediate effect or generated-card owner was incorrect for {owner.NetId}");
    }

    private static bool IsPotionPower(PowerModel power) => power is FreeAttackPower or DazzlingPower or
        FreshlySqueezedCucumberPower or DazzlingDownPower or OneTwoPunchPower;

    private static int PotionUseCount(Player player) =>
        player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).PotionUsed.Count ?? 0;

    private static string Snapshot(Player player) => JsonSerializer.Serialize(new
    {
        player.Creature.CurrentHp,
        player.Creature.Block,
        player.PlayerCombatState!.Energy,
        Powers = player.Creature.Powers.Select(power => new { Id = power.Id.ToString(), power.Amount }).ToArray(),
        Piles = player.PlayerCombatState.AllPiles.Select(pile => new
        {
            pile.Type,
            Cards = pile.Cards.Select(card => card.ToSerializable()).ToArray()
        }).ToArray(),
        Deck = player.Deck.Cards.Select(card => card.ToSerializable()).ToArray(),
        Relics = player.Relics.Select(relic => relic.ToSerializable()).ToArray(),
        Potions = player.PotionSlots.Select(potion => potion?.Id.ToString()).ToArray()
    });

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Multiplayer potion contract failed: " + message);
        }
    }
}
