using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static partial class N5BatchDiagnostics
{
    private static async Task VerifyDazzlingPriorStacksAsync(
        CombatState combatState,
        Player player,
        Creature enemy,
        PlayerChoiceContext choiceContext)
    {
        PowerChangeLedger ledger = PowerChangeLedgerService.GetLedger(combatState);
        PowerChangeEvent[] previousPowerEvents = Enumerable.Range(1, combatState.RoundNumber)
            .SelectMany(round => ledger.Snapshot(round)).ToArray();
        Require(previousPowerEvents.Length == ledger.Count,
            "Dazzling diagnostic could not preserve the complete pre-existing power ledger");

        foreach (Creature owner in new[] { player.Creature, enemy })
        {
            Creature[] opponents = combatState.GetOpponentsOf(owner)
                .Where(creature => creature.IsHittable).ToArray();
            Dictionary<Creature, decimal> initialBlock = opponents.Append(owner)
                .Distinct().ToDictionary(creature => creature, creature => (decimal)creature.Block);
            int dazzlingBefore = await RemoveAndRememberPowerAsync<DazzlingPower>(owner);
            int springBefore = await RemoveAndRememberPowerAsync<GirlOfSpringPower>(owner);
            int strengthBefore = await RemoveAndRememberPowerAsync<StrengthPower>(owner);
            foreach (Creature opponent in opponents)
            {
                await CreatureCmd.GainBlock(opponent, 100m, ValueProp.Unpowered, null, fast: true);
            }

            await PowerCmd.Apply<GirlOfSpringPower>(choiceContext, owner, 3m, owner, null);
            decimal ownerBlock = owner.Block;
            decimal opponentBlock = opponents.Sum(creature => creature.Block);
            int targetRngCounter = combatState.RunState.Rng.CombatTargets.ToSerializable().counter;

            await PowerCmd.Apply<DazzlingPower>(choiceContext, owner, 1m, owner, null);
            Require(owner.GetPower<DazzlingPower>()?.Amount == 1,
                "Dazzling first gain did not retain its stack");
            Require(opponents.Sum(creature => creature.Block) == opponentBlock && owner.Block == ownerBlock,
                "Dazzling first gain triggered damage or Girl of Spring Block");
            Require(combatState.RunState.Rng.CombatTargets.ToSerializable().counter == targetRngCounter,
                "Dazzling first gain consumed target RNG without a trigger");

            await PowerCmd.Apply<DazzlingPower>(choiceContext, owner, 1m, owner, null);
            Require(owner.GetPower<DazzlingPower>()?.Amount == 2 &&
                    opponentBlock - opponents.Sum(creature => creature.Block) == 1m &&
                    owner.Block - ownerBlock == 3m,
                "Dazzling 1 + 1 did not trigger exactly the previous one stack");

            await PowerCmd.Apply<DazzlingPower>(choiceContext, owner, 5m, owner, null);
            Require(owner.GetPower<DazzlingPower>()?.Amount == 7 &&
                    opponentBlock - opponents.Sum(creature => creature.Block) == 3m &&
                    owner.Block - ownerBlock == 6m,
                "Dazzling bulk gain included newly gained stacks in damage");

            await PowerCmd.Apply<StrengthPower>(choiceContext, owner, 1m, owner, null);
            Require(opponentBlock - opponents.Sum(creature => creature.Block) == 10m &&
                    owner.Block - ownerBlock == 9m,
                "Dazzling did not use all seven existing stacks for a different Buff gain");

            DazzlingPower dazzling = owner.GetPower<DazzlingPower>()!;
            targetRngCounter = combatState.RunState.Rng.CombatTargets.ToSerializable().counter;
            await PowerCmd.ModifyAmount(choiceContext, dazzling, 0m, owner, null);
            await PowerCmd.ModifyAmount(choiceContext, dazzling, -2m, owner, null);
            Require(dazzling.Amount == 5 &&
                    opponentBlock - opponents.Sum(creature => creature.Block) == 10m &&
                    owner.Block - ownerBlock == 9m &&
                    combatState.RunState.Rng.CombatTargets.ToSerializable().counter == targetRngCounter,
                "Dazzling zero or negative change incorrectly triggered damage, Block, or target RNG");

            await PowerCmd.Remove(dazzling);
            await PowerCmd.Remove(owner.GetPower<GirlOfSpringPower>());
            await PowerCmd.Remove(owner.GetPower<StrengthPower>());
            await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, owner, strengthBefore);
            await RestoreRememberedPowerAsync<GirlOfSpringPower>(choiceContext, owner, springBefore);
            await RestoreRememberedPowerAsync<DazzlingPower>(choiceContext, owner, dazzlingBefore);
            foreach ((Creature creature, decimal blockBefore) in initialBlock)
            {
                await RemoveAddedBlockAsync(choiceContext, creature, blockBefore);
            }
        }

        // These setup/removal commands use real hooks, but their losses must not enter later Kindness fixtures.
        ledger.Clear();
        foreach (PowerChangeEvent powerEvent in previousPowerEvents)
        {
            ledger.Record(powerEvent);
        }

        NativeSmokeTrace.N5Info(
            "Dazzling prior-stack contract passed. Player+Enemy: FirstGain=0, OnePlusOne=1, BulkGain=2, OtherBuff=7, ZeroAndLoss=0, GirlOfSpring=3PerTrigger, InitialTargetRng=unchanged.");
    }
}
