using System.Collections.Immutable;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Commands;

public sealed record LostPowerRestorationSelection(ModelId PowerModelId, int Amount);

public enum LostPowerRestorationStatus
{
    Applied,
    Prevented,
    Unsupported,
    MissingModel
}

public sealed record LostPowerRestorationAttempt(
    LostPowerRestorationSelection Selection,
    LostPowerRestorationStatus Status,
    PowerCopyResult? CopyResult,
    string? Reason);

public sealed record LostPowerRestorationResult(ImmutableArray<LostPowerRestorationAttempt> Attempts)
{
    public int RequestedAmount => Attempts.Sum(attempt => attempt.Selection.Amount);

    public int ActualAmountRestored => Attempts.Sum(attempt => attempt.CopyResult?.ActualAmountDelta ?? 0);

    public bool FullyApplied => Attempts.All(attempt => attempt.Status == LostPowerRestorationStatus.Applied);
}

public static class LostPowerRestorationCommand
{
    public static ImmutableArray<LostPowerRestorationSelection> SelectLostBuffs(
        PowerChangeLedger ledger,
        int currentRound,
        CreatureIdentity target,
        bool includePreviousRound)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(target);
        if (currentRound < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(currentRound), "Combat rounds start at one.");
        }

        IEnumerable<PowerChangeEvent> events = ledger.Snapshot(
            currentRound,
            target,
            null,
            PowerChangeDirection.Loss);
        if (includePreviousRound && currentRound > 1)
        {
            events = events.Concat(ledger.Snapshot(
                currentRound - 1,
                target,
                null,
                PowerChangeDirection.Loss));
        }

        Dictionary<ModelId, int> totals = [];
        List<ModelId> order = [];
        foreach (PowerChangeEvent powerEvent in events.Where(powerEvent =>
                     powerEvent.DeclaredPowerType == PowerType.Buff))
        {
            int lostAmount = decimal.ToInt32(decimal.Negate(powerEvent.Delta));
            if (lostAmount <= 0)
            {
                continue;
            }

            if (!totals.TryAdd(powerEvent.PowerModelId, lostAmount))
            {
                totals[powerEvent.PowerModelId] = checked(totals[powerEvent.PowerModelId] + lostAmount);
            }
            else
            {
                order.Add(powerEvent.PowerModelId);
            }
        }

        return order
            .Select(powerId => new LostPowerRestorationSelection(powerId, totals[powerId]))
            .ToImmutableArray();
    }

    public static async Task<LostPowerRestorationResult> RestoreAsync(
        PlayerChoiceContext choiceContext,
        ICombatState combatState,
        Creature target,
        CardModel? cardSource,
        bool includePreviousRound = true)
    {
        ArgumentNullException.ThrowIfNull(choiceContext);
        ArgumentNullException.ThrowIfNull(combatState);
        ArgumentNullException.ThrowIfNull(target);

        ImmutableArray<LostPowerRestorationSelection> selections = SelectLostBuffs(
            PowerChangeLedgerService.GetLedger(combatState),
            combatState.RoundNumber,
            CreatureIdentity.FromCreature(target),
            includePreviousRound);
        ImmutableArray<LostPowerRestorationAttempt>.Builder attempts =
            ImmutableArray.CreateBuilder<LostPowerRestorationAttempt>(selections.Length);

        foreach (LostPowerRestorationSelection selection in selections)
        {
            PowerModel? canonical = ModelDb.GetByIdOrNull<PowerModel>(selection.PowerModelId);
            if (canonical is null)
            {
                attempts.Add(new LostPowerRestorationAttempt(
                    selection,
                    LostPowerRestorationStatus.MissingModel,
                    null,
                    $"No canonical power is registered for {selection.PowerModelId}."));
                continue;
            }

            PowerCopyResult copyResult = await PowerCopyCommand.ApplyCopyAsync(
                choiceContext,
                canonical,
                target,
                PowerCopyApplierPolicy.Target,
                cardSource: cardSource,
                amountOverride: selection.Amount);
            LostPowerRestorationStatus status = copyResult.Status switch
            {
                PowerCopyStatus.AppliedNewInstance or PowerCopyStatus.StackedExistingInstance =>
                    LostPowerRestorationStatus.Applied,
                PowerCopyStatus.Unsupported => LostPowerRestorationStatus.Unsupported,
                _ => LostPowerRestorationStatus.Prevented
            };
            attempts.Add(new LostPowerRestorationAttempt(selection, status, copyResult, copyResult.Reason));
        }

        return new LostPowerRestorationResult(attempts.MoveToImmutable());
    }
}
