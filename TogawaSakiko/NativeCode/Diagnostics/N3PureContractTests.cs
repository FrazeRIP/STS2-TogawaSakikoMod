using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Relics;
using TogawaSakiko.NativeCode.Patches;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class N3PureContractTests
{
    public const int AssertionCount = 31;

    public static void Run()
    {
        TestExactDeckIdentitySelection();
        TestLedgerRoundAndFilterSelection();
        TestPowerCopyPolicy();
        TestHypeEligibility();
        TestHairbandExclusion();
    }

    private static void TestExactDeckIdentitySelection()
    {
        var firstPersistent = ModelDb.Card<DesireCard>().ToMutable();
        var secondPersistent = ModelDb.Card<DesireCard>().ToMutable();
        var firstCopy = ModelDb.Card<DesireCard>().ToMutable();
        var secondCopy = ModelDb.Card<DesireCard>().ToMutable();
        firstCopy.DeckVersion = firstPersistent;
        secondCopy.DeckVersion = secondPersistent;

        Require(PersistentDeckMutation.IsExactLinkedCopy(firstCopy, firstPersistent), "exact DeckVersion link was not selected");
        Require(!PersistentDeckMutation.IsExactLinkedCopy(firstCopy, secondPersistent), "identical model with a different DeckVersion was selected");
        Require(!PersistentDeckMutation.IsExactLinkedCopy(secondCopy, firstPersistent), "unrelated linked copy was selected");
    }

    private static void TestLedgerRoundAndFilterSelection()
    {
        PowerChangeLedger ledger = new();
        CreatureIdentity player = new(0, CombatSide.Player, new ModelId("CHARACTER", "PLAYER"), 1);
        CreatureIdentity secondPlayer = new(1, CombatSide.Player, new ModelId("CHARACTER", "SECOND_PLAYER"), 2);
        CreatureIdentity enemy = new(2, CombatSide.Enemy, new ModelId("MONSTER", "ENEMY"), null);
        ModelId strength = new("POWER", "STRENGTH");
        ModelId weak = new("POWER", "WEAK");

        ledger.Record(Event(1, player, strength, PowerType.Buff, 2m, PowerChangeKind.New));
        ledger.Record(Event(1, player, strength, PowerType.Buff, 1m, PowerChangeKind.Increased));
        ledger.Record(Event(1, secondPlayer, strength, PowerType.Buff, 1m, PowerChangeKind.New));
        ledger.Record(Event(1, enemy, weak, PowerType.Debuff, 2m, PowerChangeKind.New));
        ledger.Record(Event(1, enemy, weak, PowerType.Debuff, -1m, PowerChangeKind.Reduced));
        ledger.Record(Event(2, enemy, weak, PowerType.Debuff, -1m, PowerChangeKind.FullRemoval));

        Require(ledger.Snapshot(1).Length == 5, "same-round events rotated early");
        Require(ledger.Snapshot(2).Length == 1, "next-round event was not isolated");
        Require(ledger.Snapshot(1, player).Length == 2, "player target filter failed");
        Require(ledger.Snapshot(1, secondPlayer).Length == 1, "second-player target filter failed");
        Require(ledger.Snapshot(1, enemy).Length == 2, "enemy target filter failed");
        Require(ledger.Snapshot(1, null, PowerType.Buff).Length == 3, "buff filter failed");
        Require(ledger.Snapshot(1, null, PowerType.Debuff).Length == 2, "debuff filter failed");
        Require(ledger.Snapshot(1, player, null, PowerChangeDirection.Gain).Length == 2, "multiple gains filter failed");
        Require(ledger.Snapshot(1, enemy, null, PowerChangeDirection.Loss).Length == 1, "loss filter failed");
        Require(ledger.Snapshot(2, enemy, null, PowerChangeDirection.Removal).Length == 1, "full-removal filter failed");

        var immutableSnapshot = ledger.Snapshot(1);
        ledger.Clear();
        Require(immutableSnapshot.Length == 5 && ledger.Count == 0, "ledger snapshots were not immutable across reset");
    }

    private static void TestPowerCopyPolicy()
    {
        Require(PowerCopyCommand.GetCompatibility(ModelDb.Power<StrengthPower>()).Supported, "normal stackable power was rejected");
        Require(PowerCopyCommand.GetCompatibility(ModelDb.Power<TheBombPower>()).Supported, "instanced dynamic-var power was rejected");
        Require(PowerCopyCommand.GetCompatibility(ModelDb.Power<StranglePower>()).Supported, "approved reset-safe per-applier power was rejected");
        Require(PowerCopyCommand.GetCompatibility(ModelDb.Power<WeakPower>()).Supported, "duration power was rejected");
        Require(!PowerCopyCommand.GetCompatibility(ModelDb.Power<FlexPotionPower>()).Supported, "paired temporary power was accepted");
        Require(!PowerCopyCommand.GetCompatibility(ModelDb.Power<AutomationPower>()).Supported, "unsupported custom internal state was accepted");
    }

    private static void TestHypeEligibility()
    {
        Require(HypeBlockLossPolicy.CanPreventExplicitLoss(true, false, false, 5, 1m, 1), "eligible Hype loss was rejected");
        Require(!HypeBlockLossPolicy.CanPreventExplicitLoss(false, false, false, 5, 1m, 1), "out-of-combat Hype loss was accepted");
        Require(!HypeBlockLossPolicy.CanPreventExplicitLoss(true, true, false, 5, 1m, 1), "combat teardown Hype loss was accepted");
        Require(!HypeBlockLossPolicy.CanPreventExplicitLoss(true, false, true, 5, 1m, 1), "dead target Hype loss was accepted");
        Require(!HypeBlockLossPolicy.CanPreventExplicitLoss(true, false, false, 0, 1m, 1), "zero-block Hype loss was accepted");
        Require(!HypeBlockLossPolicy.CanPreventExplicitLoss(true, false, false, 5, 0m, 1), "zero loss was accepted");
        Require(!HypeBlockLossPolicy.CanPreventExplicitLoss(true, false, false, 5, -1m, 1), "negative loss was accepted");
        Require(!HypeBlockLossPolicy.CanPreventExplicitLoss(true, false, false, 5, 1m, 0), "zero-Hype loss was accepted");
    }

    private static void TestHairbandExclusion()
    {
        Require(StarterRelicTogawaSakiko.IsEligibleVictory("ACT1", "CULTIST"), "eligible Hairband victory was rejected");
        string excludedRoute = NativeStableIds.EntryPrefix + "OUT_OF_SCOPE_ROUTE";
        Require(!StarterRelicTogawaSakiko.IsEligibleVictory(excludedRoute, "CULTIST"), "mod-owned act route was not excluded");
        Require(!StarterRelicTogawaSakiko.IsEligibleVictory("ACT1", excludedRoute), "mod-owned encounter route was not excluded");
    }

    private static PowerChangeEvent Event(
        int round,
        CreatureIdentity target,
        ModelId powerId,
        PowerType powerType,
        decimal delta,
        PowerChangeKind kind)
    {
        return new PowerChangeEvent(
            round,
            target,
            powerId,
            powerId.Entry,
            powerType,
            powerType,
            delta,
            0,
            (int)delta,
            kind,
            null,
            null);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Phase N3 pure contract failure: " + message);
        }
    }
}
