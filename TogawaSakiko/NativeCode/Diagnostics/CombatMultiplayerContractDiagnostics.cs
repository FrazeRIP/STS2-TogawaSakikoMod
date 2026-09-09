using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.GameActions;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Diagnostics;

/// <summary>Opt-in harness probes. Execute through CombatMultiplayerContractGameAction on every peer.</summary>
internal static class CombatMultiplayerContractDiagnostics
{
    internal static async Task RunAsync(PlayerChoiceContext choiceContext, CardModel source)
    {
        ICombatState combat = source.Owner.Creature.CombatState
            ?? throw new InvalidOperationException("Multiplayer combat probes require active combat.");
        Player[] players = combat.Players.ToArray();
        Require(players.Length > 1, "probes require at least two players");

        GD.Print($"Multiplayer shared combat: start, source owner={source.Owner.NetId}, players={players.Length}.");
        await VerifyDeckIdentityAsync(combat, players);
        await VerifyHypeAndCopyAsync(choiceContext, source, players);
        await VerifyNestedLedgerAsync(choiceContext, source, combat);
        await CombatMultiplayerEdgeDiagnostics.RunAsync(choiceContext, source);
        await MultiplayerPotionContractDiagnostics.RunAsync(choiceContext, source);
        NativeSmokeTrace.Info($"Combat multiplayer shared contracts passed for {players.Length} players.");
        GD.Print($"Multiplayer shared combat: PASSED for {players.Length} players.");
    }

    private static async Task VerifyDeckIdentityAsync(ICombatState combat, IReadOnlyList<Player> players)
    {
        foreach (Player owner in players)
        {
            GD.Print($"Multiplayer shared combat: deck identity owner={owner.NetId}.");
            int originalDeckCount = owner.Deck.Cards.Count;
            var first = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<DesireCard>(
                owner, combat, PileType.Draw, skipPersistentVisuals: true);
            var second = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<DesireCard>(
                owner, combat, PileType.Discard, skipPersistentVisuals: true);
            Require(first.Success && second.Success, "duplicate deck fixture could not be added");
            CardModel firstCard = first.PersistentCard!;
            CardModel secondCard = second.PersistentCard!;

            var original = (NetPersistentDeckRemovalAction)new PersistentDeckRemovalGameAction(secondCard, true).ToNetAction();
            Require(original.LinkedCombatCard.HasValue, "queued removal omitted its stable linked combat identity");
            PacketWriter writer = new();
            original.Serialize(writer);
            PacketReader reader = new();
            reader.Reset(writer.Buffer.AsSpan(0, writer.BytePosition).ToArray());
            NetPersistentDeckRemovalAction restored = new();
            restored.Deserialize(reader);
            Require(reader.BitPosition == writer.BitPosition &&
                    restored.PersistentCard.DeckIndex == original.PersistentCard.DeckIndex &&
                    restored.LinkedCombatCard == original.LinkedCombatCard && restored.SkipCombatVisuals,
                "removal packet round trip changed its identity or flags");

            Require((await PersistentDeckMutation.RemoveAsync(firstCard, false, true)).Success,
                "earlier duplicate could not be removed");
            var restoredAction = (PersistentDeckRemovalGameAction)restored.ToGameAction(owner);
            Require(ReferenceEquals(restoredAction.ResolvePersistentCard(), secondCard),
                "deck-index shift changed the queued removal target");
            Player other = players.First(player => player != owner);
            bool rejectedOtherOwner = false;
            try
            {
                ((PersistentDeckRemovalGameAction)restored.ToGameAction(other)).ResolvePersistentCard();
            }
            catch (InvalidOperationException)
            {
                rejectedOtherOwner = true;
            }
            Require(rejectedOtherOwner, "a packet could remove another player's persistent card");
            Require(second.CombatCard!.Pile?.Type == PileType.Discard,
                "removing an identical card changed another linked copy");
            Require((await PersistentDeckMutation.RemoveAsync(secondCard, false, true)).Success,
                "exact queued identity could not be removed");
            Require((await PersistentDeckMutation.RemoveAsync(secondCard, false, true)).Status ==
                    PersistentDeckRemovalStatus.AlreadyRemoved,
                "repeated exact removal was not idempotent");
            Require(owner.Deck.Cards.Count == originalDeckCount &&
                    first.CombatCard!.HasBeenRemovedFromState && second.CombatCard.HasBeenRemovedFromState,
                "persistent/linked fixture cleanup left extra cards");
        }
    }

    private static async Task VerifyHypeAndCopyAsync(
        PlayerChoiceContext choiceContext,
        CardModel source,
        IReadOnlyList<Player> players)
    {
        foreach (Player owner in players)
        {
            GD.Print($"Multiplayer shared combat: Hype/copy owner={owner.NetId}.");
            Creature target = owner.Creature;
            int[] otherHypeBefore = players.Where(player => player != owner)
                .Select(player => player.Creature.GetPower<HypePower>()?.Amount ?? 0).ToArray();
            int hypeBefore = target.GetPower<HypePower>()?.Amount ?? 0;
            await CreatureCmd.GainBlock(target, 5, ValueProp.Unpowered, null);
            await PowerCmd.Apply<HypePower>(choiceContext, target, 1, source.Owner.Creature, source);
            int blockBefore = target.Block;
            await CreatureCmd.LoseBlock(choiceContext, target, 1, source.Owner.Creature);
            Require(target.Block == blockBefore && (target.GetPower<HypePower>()?.Amount ?? 0) == hypeBefore,
                "explicit block loss did not consume exactly one target Hype");
            Require(players.Where(player => player != owner)
                    .Select(player => player.Creature.GetPower<HypePower>()?.Amount ?? 0).SequenceEqual(otherHypeBefore),
                "Hype interception consumed another player's stacks");

            PowerCopyResult copy = await PowerCopyCommand.ApplyCopyAsync(
                choiceContext, ModelDb.Power<StrengthPower>(), target,
                PowerCopyApplierPolicy.None, cardSource: source, amountOverride: 2, silent: true);
            Require(copy.Success && copy.Applier is null && copy.ActualAmountDelta == 2 && copy.AppliedPower?.Owner == target,
                "a null-applier copy failed or acquired another owner");
            await PowerCmd.ModifyAmount(choiceContext, copy.AppliedPower!, -2, null, source, silent: true);
        }
    }

    private static async Task VerifyNestedLedgerAsync(
        PlayerChoiceContext choiceContext,
        CardModel source,
        ICombatState combat)
    {
        Creature owner = source.Owner.Creature;
        GD.Print($"Multiplayer shared combat: nested ledger owner={source.Owner.NetId}.");
        Require(!owner.HasPower<MelodiaPower>() && !owner.HasPower<MonsterDivinityPower>(),
            "nested-ledger fixture requires no active Melodia or Divinity");
        PowerChangeLedger ledger = PowerChangeLedgerService.GetLedger(combat);
        int before = ledger.Snapshot(combat.RoundNumber).Length;
        await PowerCmd.Apply<MelodiaPower>(choiceContext, owner, SakikoStanceCmd.MelodiaThreshold, owner, source);
        PowerChangeEvent[] events = ledger.Snapshot(combat.RoundNumber).Skip(before)
            .Where(change => change.Target == CreatureIdentity.FromCreature(owner) &&
                             change.PowerModelId == ModelDb.GetId<MelodiaPower>()).ToArray();
        Require(events.Length == 2 && events[0].Delta == 10 && events[0].AmountBefore == 0 &&
                events[0].AmountAfter == 10 && events[1].Delta == -10 && events[1].AmountBefore == 10 &&
                events[1].AmountAfter == 0,
            "nested same-power hooks overwrote delta snapshots, reversed events, or double-recorded removal");
        Require(events.All(change => change.Target.PlayerNetId == source.Owner.NetId &&
                                     change.Applier?.PlayerNetId == source.Owner.NetId &&
                                     change.CardSource?.OwnerNetId == source.Owner.NetId),
            "nested ledger events lost their player/applier/card ownership");
        Require(owner.HasPower<MonsterDivinityPower>(), "nested Melodia did not enter Divinity");
        await PowerCmd.Remove<MonsterDivinityPower>(owner);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Combat multiplayer contract failed: " + message);
        }
    }
}
