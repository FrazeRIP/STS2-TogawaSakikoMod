using System.Collections.Immutable;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace TogawaSakiko.NativeCode.Tracking;

public sealed class PowerChangeLedger
{
    private readonly List<PowerChangeEvent> _events = [];

    public int Count => _events.Count;

    public void Record(PowerChangeEvent powerEvent)
    {
        ArgumentNullException.ThrowIfNull(powerEvent);
        if (powerEvent.Round < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(powerEvent), "Combat rounds start at one.");
        }

        _events.Add(powerEvent);
    }

    public ImmutableArray<PowerChangeEvent> Snapshot(
        int round,
        CreatureIdentity? target = null,
        PowerType? powerType = null,
        PowerChangeDirection direction = PowerChangeDirection.Any)
    {
        IEnumerable<PowerChangeEvent> query = _events.Where(powerEvent => powerEvent.Round == round);
        if (target is not null)
        {
            query = query.Where(powerEvent => powerEvent.Target == target);
        }

        if (powerType.HasValue)
        {
            query = query.Where(powerEvent => powerEvent.EffectivePowerType == powerType.Value);
        }

        query = direction switch
        {
            PowerChangeDirection.Any => query,
            PowerChangeDirection.Gain => query.Where(powerEvent => powerEvent.IsGain),
            PowerChangeDirection.Loss => query.Where(powerEvent => powerEvent.IsLoss),
            PowerChangeDirection.Removal => query.Where(powerEvent => powerEvent.IsRemoval),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

        return query.ToImmutableArray();
    }

    public ImmutableArray<PowerChangeEvent> CurrentRound(
        ICombatState combatState,
        Creature? target = null,
        PowerType? powerType = null,
        PowerChangeDirection direction = PowerChangeDirection.Any)
    {
        return Snapshot(
            combatState.RoundNumber,
            target is null ? null : CreatureIdentity.FromCreature(target),
            powerType,
            direction);
    }

    public ImmutableArray<PowerChangeEvent> PreviousRound(
        ICombatState combatState,
        Creature? target = null,
        PowerType? powerType = null,
        PowerChangeDirection direction = PowerChangeDirection.Any)
    {
        int previousRound = combatState.RoundNumber - 1;
        return previousRound < 1
            ? ImmutableArray<PowerChangeEvent>.Empty
            : Snapshot(
                previousRound,
                target is null ? null : CreatureIdentity.FromCreature(target),
                powerType,
                direction);
    }

    public void Clear()
    {
        _events.Clear();
    }
}
