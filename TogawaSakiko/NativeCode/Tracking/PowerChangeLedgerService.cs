using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TogawaSakiko.NativeCode.Diagnostics;

namespace TogawaSakiko.NativeCode.Tracking;

public static class PowerChangeLedgerService
{
    private const string SubscriptionId = Bootstrap.ModEntryPoint.ModId + ".PowerChangeLedger";
    private static readonly ConditionalWeakTable<CombatState, PowerChangeLedgerHookModel> Hooks = new();
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        ModHelper.SubscribeForCombatStateHooks(
            SubscriptionId,
            combatState => [GetOrCreateHook(combatState)]);
        _initialized = true;
    }

    public static PowerChangeLedger GetLedger(ICombatState combatState)
    {
        if (combatState is not CombatState concreteState)
        {
            throw new ArgumentException("The power ledger requires a live CombatState.", nameof(combatState));
        }

        return GetOrCreateHook(concreteState).Ledger;
    }

    internal static PowerChangeLedgerHookModel GetOrCreateHook(CombatState combatState)
    {
        return Hooks.GetValue(combatState, state =>
        {
            PowerChangeLedgerHookModel canonical = ModelDb.GetById<PowerChangeLedgerHookModel>(
                ModelDb.GetId<PowerChangeLedgerHookModel>());
            PowerChangeLedgerHookModel hook = (PowerChangeLedgerHookModel)canonical.MutableClone();
            hook.Attach(state);
            return hook;
        });
    }
}

public sealed class PowerChangeLedgerHookModel : AbstractModel
{
    private readonly record struct PowerStateBeforeChange(int Amount, bool WasPresent);

    private CombatState? _combatState;
    private HashSet<Creature> _subscribedCreatures = [];
    private Dictionary<PowerModel, PowerStateBeforeChange> _stateBeforeChange = [];
    private HashSet<PowerModel> _removeEventAlreadyRecorded = [];
    private int _highestObservedRound = 1;

    public override bool ShouldReceiveCombatHooks => true;

    public PowerChangeLedger Ledger { get; private set; } = new();

    internal void Attach(CombatState combatState)
    {
        if (_combatState is not null)
        {
            throw new InvalidOperationException("A power ledger hook cannot be attached to two combats.");
        }

        _combatState = combatState;
        _highestObservedRound = combatState.RoundNumber;
        combatState.CreaturesChanged += HandleCreaturesChanged;
        SubscribeToCurrentCreatures();
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _combatState = null;
        _subscribedCreatures = [];
        _stateBeforeChange = [];
        _removeEventAlreadyRecorded = [];
        Ledger = new PowerChangeLedger();
    }

    public override Task BeforePowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature target,
        Creature? applier,
        CardModel? cardSource)
    {
        ObserveRound();
        bool wasPresent = target.Powers.Contains(power);
        _stateBeforeChange[power] = new PowerStateBeforeChange(
            wasPresent ? power.Amount : 0,
            wasPresent);
        return Task.CompletedTask;
    }

    // Called at native hook entry, before reactive powers can change/remove this power again.
    internal Task RecordPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        ObserveRound();
        PowerStateBeforeChange stateBefore = _stateBeforeChange.Remove(power, out PowerStateBeforeChange capturedState)
            ? capturedState
            : new PowerStateBeforeChange(power.Amount - (int)amount, power.Amount != (int)amount);
        int actualDelta = power.Amount - stateBefore.Amount;
        bool willBeRemoved = power.ShouldRemoveDueToAmount();
        PowerChangeKind kind = DetermineKind(stateBefore.WasPresent, actualDelta, willBeRemoved);

        Ledger.Record(CreateEvent(
            power,
            actualDelta,
            stateBefore.Amount,
            power.Amount,
            kind,
            applier,
            cardSource));

        if (willBeRemoved)
        {
            _removeEventAlreadyRecorded.Add(power);
        }

        return Task.CompletedTask;
    }

    public override Task AfterCreatureAddedToCombat(Creature creature)
    {
        Subscribe(creature);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        ClearAndDetach();
        return Task.CompletedTask;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await N3ContractDiagnostics.AfterCombatVictoryAsync(Ledger, room);
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        return N3ContractDiagnostics.TryModifyPowerAmountReceived(
            canonicalPower,
            target,
            amount,
            out modifiedAmount);
    }

    private void HandleCreaturesChanged(ICombatState combatState)
    {
        SubscribeToCurrentCreatures();
    }

    private void SubscribeToCurrentCreatures()
    {
        if (_combatState is null)
        {
            return;
        }

        foreach (Creature creature in _combatState.Creatures)
        {
            Subscribe(creature);
        }
    }

    private void Subscribe(Creature creature)
    {
        if (_subscribedCreatures.Add(creature))
        {
            creature.PowerRemoved += HandlePowerRemoved;
        }
    }

    private void HandlePowerRemoved(PowerModel power)
    {
        if (_combatState is null)
        {
            return;
        }

        _stateBeforeChange.Remove(power);
        if (_removeEventAlreadyRecorded.Remove(power))
        {
            return;
        }

        ObserveRound();
        Ledger.Record(CreateEvent(
            power,
            -power.Amount,
            power.Amount,
            0,
            PowerChangeKind.DirectRemoval,
            power.Applier,
            null));
    }

    private PowerChangeEvent CreateEvent(
        PowerModel power,
        decimal delta,
        int amountBefore,
        int amountAfter,
        PowerChangeKind kind,
        Creature? applier,
        CardModel? cardSource)
    {
        if (_combatState is null)
        {
            throw new InvalidOperationException("The power ledger hook is not attached to a combat.");
        }

        PowerType effectiveType = power.GetTypeForAmount(
            kind is PowerChangeKind.FullRemoval or PowerChangeKind.DirectRemoval
                ? amountBefore
                : amountAfter);
        return new PowerChangeEvent(
            _combatState.RoundNumber,
            CreatureIdentity.FromCreature(power.Owner),
            power.Id,
            power.GetType().FullName ?? power.GetType().Name,
            power.Type,
            effectiveType,
            delta,
            amountBefore,
            amountAfter,
            kind,
            applier is null ? null : CreatureIdentity.FromCreature(applier),
            cardSource is null ? null : CardSourceIdentity.FromCard(cardSource));
    }

    private void ObserveRound()
    {
        if (_combatState is null)
        {
            return;
        }

        if (_combatState.RoundNumber < _highestObservedRound)
        {
            Ledger.Clear();
            _stateBeforeChange.Clear();
            _removeEventAlreadyRecorded.Clear();
        }

        _highestObservedRound = _combatState.RoundNumber;
    }

    private void ClearAndDetach()
    {
        if (_combatState is not null)
        {
            _combatState.CreaturesChanged -= HandleCreaturesChanged;
        }

        foreach (Creature creature in _subscribedCreatures)
        {
            creature.PowerRemoved -= HandlePowerRemoved;
        }

        _subscribedCreatures.Clear();
        _stateBeforeChange.Clear();
        _removeEventAlreadyRecorded.Clear();
        Ledger.Clear();
        _combatState = null;
    }

    private static PowerChangeKind DetermineKind(bool wasPresent, decimal actualDelta, bool willBeRemoved)
    {
        if (willBeRemoved)
        {
            return PowerChangeKind.FullRemoval;
        }

        if (!wasPresent)
        {
            return PowerChangeKind.New;
        }

        return actualDelta > 0m ? PowerChangeKind.Increased : PowerChangeKind.Reduced;
    }
}
