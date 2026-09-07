using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.NativeCode.Commands;

public enum PowerCopyApplierPolicy
{
    PreserveSource,
    Target,
    Explicit,
    None
}

public enum PowerCopyStatus
{
    AppliedNewInstance,
    StackedExistingInstance,
    Prevented,
    Unsupported
}

public sealed record PowerCopyCompatibility(bool Supported, string? Reason)
{
    public static PowerCopyCompatibility Allowed { get; } = new(true, null);
}

public sealed record PowerCopyResult(
    PowerCopyStatus Status,
    PowerModel Source,
    Creature Target,
    PowerModel? AppliedPower,
    int AmountBefore,
    int AmountAfter,
    Creature? Applier,
    string? Reason)
{
    public bool Success => Status is PowerCopyStatus.AppliedNewInstance or PowerCopyStatus.StackedExistingInstance;

    public int ActualAmountDelta => AmountAfter - AmountBefore;
}

public static class PowerCopyCommand
{
    private static readonly HashSet<Type> ResetSafeInternalStatePowers =
    [
        typeof(StranglePower),
        typeof(OblivionPower)
    ];

    public static PowerCopyCompatibility GetCompatibility(PowerModel source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is ITemporaryPower)
        {
            return new PowerCopyCompatibility(
                false,
                $"{source.GetType().Name} is a paired temporary power and must be recreated by its originating effect.");
        }

        if (source.IsMutable && source.Target is not null)
        {
            return new PowerCopyCompatibility(
                false,
                $"{source.GetType().Name} carries a separate Target reference whose ownership semantics are unsupported.");
        }

        MethodInfo? internalDataFactory = source.GetType().GetMethod(
            "InitInternalData",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (internalDataFactory?.DeclaringType != typeof(PowerModel) &&
            !ResetSafeInternalStatePowers.Contains(source.GetType()))
        {
            return new PowerCopyCompatibility(
                false,
                $"{source.GetType().Name} has custom private state without an approved reset-safe copy policy.");
        }

        return PowerCopyCompatibility.Allowed;
    }

    public static async Task<PowerCopyResult> ApplyCopyAsync(
        PlayerChoiceContext choiceContext,
        PowerModel source,
        Creature target,
        PowerCopyApplierPolicy applierPolicy = PowerCopyApplierPolicy.PreserveSource,
        Creature? explicitApplier = null,
        CardModel? cardSource = null,
        int? amountOverride = null,
        bool silent = false)
    {
        ArgumentNullException.ThrowIfNull(choiceContext);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        PowerCopyCompatibility compatibility = GetCompatibility(source);
        if (!compatibility.Supported)
        {
            return new PowerCopyResult(
                PowerCopyStatus.Unsupported,
                source,
                target,
                null,
                0,
                0,
                null,
                compatibility.Reason);
        }

        if (target.CombatState is null)
        {
            return new PowerCopyResult(
                PowerCopyStatus.Prevented,
                source,
                target,
                null,
                0,
                0,
                null,
                "The copy target is not in combat.");
        }

        if (source.IsCanonical && !amountOverride.HasValue)
        {
            return new PowerCopyResult(
                PowerCopyStatus.Unsupported,
                source,
                target,
                null,
                0,
                0,
                null,
                "A canonical power has no live amount; an explicit amount override is required.");
        }

        Creature? applier = ResolveApplier(source, target, applierPolicy, explicitApplier);
        if (applier?.CombatState != target.CombatState)
        {
            return new PowerCopyResult(
                PowerCopyStatus.Unsupported,
                source,
                target,
                null,
                0,
                0,
                applier,
                "The selected applier is not in the target combat.");
        }

        int intendedAmount = amountOverride ?? source.Amount;
        if (intendedAmount == 0)
        {
            return new PowerCopyResult(
                PowerCopyStatus.Prevented,
                source,
                target,
                null,
                0,
                0,
                applier,
                "The intended copied amount is zero.");
        }

        PowerModel preservedClone = (PowerModel)source.ClonePreservingMutability();
        PowerModel clone = preservedClone.IsCanonical
            ? preservedClone.ToMutable()
            : preservedClone;
        clone.Applier = null;
        clone.Target = null;
        clone.AmountOnTurnStart = 0;
        clone.SkipNextDurationTick = false;

        PowerModel? existing = PowerCmd.FindExistingInstanceForStacking(clone, target, applier);
        int amountBefore = existing?.Amount ?? 0;
        await PowerCmd.Apply(choiceContext, clone, target, intendedAmount, applier, cardSource, silent);

        PowerModel? applied = existing;
        if (applied is null && target.Powers.Contains(clone))
        {
            applied = clone;
        }

        if (applied is null)
        {
            return new PowerCopyResult(
                PowerCopyStatus.Prevented,
                source,
                target,
                null,
                amountBefore,
                amountBefore,
                applier,
                "Native power application did not add or stack the copied power.");
        }

        return new PowerCopyResult(
            existing is null ? PowerCopyStatus.AppliedNewInstance : PowerCopyStatus.StackedExistingInstance,
            source,
            target,
            applied,
            amountBefore,
            applied.Amount,
            applier,
            null);
    }

    private static Creature? ResolveApplier(
        PowerModel source,
        Creature target,
        PowerCopyApplierPolicy policy,
        Creature? explicitApplier)
    {
        return policy switch
        {
            PowerCopyApplierPolicy.PreserveSource => source.IsMutable ? source.Applier : null,
            PowerCopyApplierPolicy.Target => target,
            PowerCopyApplierPolicy.Explicit => explicitApplier,
            PowerCopyApplierPolicy.None => null,
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
        };
    }
}
