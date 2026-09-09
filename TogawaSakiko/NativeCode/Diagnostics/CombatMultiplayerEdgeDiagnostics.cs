using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Diagnostics;

/// <summary>Bounded real-command probes inside the recorded, replicated diagnostic action.</summary>
internal static class CombatMultiplayerEdgeDiagnostics
{
    internal static async Task RunAsync(PlayerChoiceContext context, CardModel source)
    {
        CombatState combat = source.Owner.Creature.CombatState as CombatState
            ?? throw new InvalidOperationException("Multiplayer edge probes require concrete combat state.");
        Player[] players = combat.Players.ToArray();
        Creature[] enemies = combat.HittableEnemies.ToArray();
        Require(players.Length > 1 && enemies.Length > 0, "fixture party or opponents missing");
        // Fixture protection/cleanup is deterministic and intentionally bypasses Hype interception.
        // Native history and RNG advances remain intact for peer and replay checksum comparison.
        Dictionary<Creature, int> blocks = players.Select(player => player.Creature).Concat(enemies)
            .ToDictionary(creature => creature, creature => creature.Block);
        foreach (Creature enemy in enemies)
        {
            enemy.GainBlockInternal(10000);
        }
        await VerifyOwnerHistoryAsync(context, combat, players, enemies[0]);
        await VerifyDazzlingOwnerRngAsync(context, source, combat, players, enemies);
        await VerifyInstancedCopiesAsync(context, source, players, enemies[0]);
        foreach ((Creature creature, int block) in blocks)
        {
            creature.LoseBlockInternal(creature.Block);
            creature.GainBlockInternal(block);
        }
        GD.Print($"Multiplayer combat edges PASSED: {players.Length} owner histories, Dazzling RNG, instanced/per-applier copies.");
    }

    private static async Task VerifyOwnerHistoryAsync(
        PlayerChoiceContext context, CombatState combat, Player[] players, Creature enemy)
    {
        List<CardModel> fixture = [];
        Dictionary<Player, ImprisonedXIICard> attacks = [];
        for (int slot = 0; slot < players.Length; slot++)
        {
            Player owner = players[slot];
            int before = ImprisonedXIICard.CountDesiresPlayed(CombatManager.Instance.History.CardPlaysFinished, owner);
            for (int copy = 0; copy <= slot; copy++)
            {
                fixture.Add(await PlayAsync<DesireCard>(context, combat, owner, null));
            }
            int desires = ImprisonedXIICard.CountDesiresPlayed(CombatManager.Instance.History.CardPlaysFinished, owner);
            Require(desires == before + slot + 1, "owner Desire history included a teammate or lost a duplicate");
            int[] energies = players.Select(player => player.PlayerCombatState!.Energy).ToArray();
            ImprisonedXIICard attack = await PlayAsync<ImprisonedXIICard>(context, combat, owner, enemy);
            fixture.Add(attack);
            attacks.Add(owner, attack);
            Require(owner.PlayerCombatState!.Energy == energies[slot] + desires,
                "Imprisoned XII paid energy from another player's Desire history");
            Require(players.Where(player => player != owner)
                    .All(player => player.PlayerCombatState!.Energy == energies[Array.IndexOf(players, player)]),
                "Imprisoned XII changed another player's energy");
            await PlayerCmd.SetEnergy(energies[slot], owner);
        }
        // All owners played the same model; the latest global attack belongs to the last slot.
        foreach (Player owner in players)
        {
            Require(ReferenceEquals(SymbolIFireCard.SelectPreviousAttack(
                    CombatManager.Instance.History.CardPlaysStarted, owner), attacks[owner]),
                "Symbol I: Fire selected another owner's identical attack from native history");
        }
        await CardPileCmd.RemoveFromCombat(fixture, skipVisuals: true);
    }

    private static async Task<T> PlayAsync<T>(
        PlayerChoiceContext context, CombatState combat, Player owner, Creature? target) where T : CardModel
    {
        T card = combat.CreateCard<T>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, owner, CardPilePosition.Top);
        await CardCmd.AutoPlay(context, card, target, skipCardPileVisuals: true);
        return card;
    }

    private static async Task VerifyDazzlingOwnerRngAsync(
        PlayerChoiceContext context, CardModel source, CombatState combat, Player[] players, Creature[] enemies)
    {
        Require(players.All(player => !player.Creature.HasPower<DazzlingPower>()),
            "Dazzling fixture needs an initially clear party");
        foreach (Player owner in players)
        {
            int counter = combat.RunState.Rng.CombatTargets.ToSerializable().counter;
            int block = enemies.Sum(enemy => enemy.Block);
            DazzlingPower dazzling = await PowerCmd.Apply<DazzlingPower>(
                context, owner.Creature, 2, owner.Creature, source)
                ?? throw new InvalidOperationException("Dazzling fixture was prevented.");
            Require(combat.RunState.Rng.CombatTargets.ToSerializable().counter == counter,
                "Dazzling initial gain consumed target RNG");
            Player other = players.First(player => player != owner);
            await PowerCmd.Apply<HypePower>(context, other.Creature, 1, owner.Creature, source);
            Require(combat.RunState.Rng.CombatTargets.ToSerializable().counter == counter &&
                    enemies.Sum(enemy => enemy.Block) == block,
                "Dazzling reacted to another owner's buff or consumed target RNG");
            await PowerCmd.ModifyAmount(context, other.Creature.GetPower<HypePower>()!, -1, owner.Creature, source);
            await PowerCmd.Apply<HypePower>(context, owner.Creature, 1, owner.Creature, source);
            Require(combat.RunState.Rng.CombatTargets.ToSerializable().counter == counter + 1 &&
                    block - enemies.Sum(enemy => enemy.Block) == 2,
                "Dazzling owner buff did not consume exactly one target draw and deal two damage");
            await PowerCmd.ModifyAmount(context, owner.Creature.GetPower<HypePower>()!, -1, owner.Creature, source);
            await PowerCmd.Remove(dazzling);
        }
    }

    private static async Task VerifyInstancedCopiesAsync(
        PlayerChoiceContext context, CardModel source, Player[] players, Creature enemy)
    {
        TheBombPower bomb = await PowerCmd.Apply<TheBombPower>(
            context, players[0].Creature, 2, players[0].Creature, source, silent: true)
            ?? throw new InvalidOperationException("Bomb fixture was prevented.");
        bomb.SetDamage(77);
        int removed = 0;
        bomb.Removed += () => removed++;
        List<PowerModel> copies = [];
        foreach (Player owner in players)
        {
            PowerCopyResult result = await PowerCopyCommand.ApplyCopyAsync(
                context, bomb, owner.Creature, PowerCopyApplierPolicy.Target, cardSource: source, silent: true);
            Require(result.Status == PowerCopyStatus.AppliedNewInstance && result.AppliedPower is TheBombPower &&
                    !ReferenceEquals(result.AppliedPower, bomb) && result.AppliedPower.Owner == owner.Creature &&
                    result.Applier == owner.Creature && result.AppliedPower.DynamicVars.Damage.BaseValue == 77,
                "instanced copy lost owner/applier/variables or merged with another instance");
            copies.Add(result.AppliedPower!);
        }
        foreach (PowerModel copy in copies)
        {
            await PowerCmd.Remove(copy);
        }
        Require(removed == 0 && players[0].Creature.Powers.Contains(bomb),
            "copied Bomb removal invoked its source subscription or removed its source");
        await PowerCmd.Remove(bomb);
        Require(removed == 1, "source Bomb event subscription did not remain independent");

        Require(!enemy.Powers.OfType<StranglePower>().Any(), "per-applier fixture needs clear target");
        List<PowerModel> perApplier = [];
        foreach (Player owner in players)
        {
            PowerCopyResult first = await PowerCopyCommand.ApplyCopyAsync(
                context, ModelDb.Power<StranglePower>(), enemy, PowerCopyApplierPolicy.Explicit,
                owner.Creature, source, amountOverride: 2, silent: true);
            PowerCopyResult second = await PowerCopyCommand.ApplyCopyAsync(
                context, ModelDb.Power<StranglePower>(), enemy, PowerCopyApplierPolicy.Explicit,
                owner.Creature, source, amountOverride: 2, silent: true);
            Require(first.Status == PowerCopyStatus.AppliedNewInstance &&
                    second.Status == PowerCopyStatus.StackedExistingInstance &&
                    ReferenceEquals(first.AppliedPower, second.AppliedPower) && second.AmountAfter == 4 &&
                    second.AppliedPower?.Applier == owner.Creature,
                "per-applier copy failed to separate players or stack the same player");
            perApplier.Add(first.AppliedPower!);
        }
        Require(perApplier.Distinct().Count() == players.Length &&
                enemy.Powers.OfType<StranglePower>().Count() == players.Length,
            "per-applier instances were merged across owners");
        foreach (PowerModel power in perApplier)
        {
            await PowerCmd.Remove(power);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Multiplayer combat edge failed: " + message);
        }
    }
}
