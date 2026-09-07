using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Tracking;

public enum PowerChangeKind
{
    New,
    Increased,
    Reduced,
    FullRemoval,
    DirectRemoval
}

public enum PowerChangeDirection
{
    Any,
    Gain,
    Loss,
    Removal
}

public sealed record CreatureIdentity(
    uint? CombatId,
    CombatSide Side,
    ModelId ModelId,
    ulong? PlayerNetId)
{
    public static CreatureIdentity FromCreature(Creature creature)
    {
        ArgumentNullException.ThrowIfNull(creature);
        return new CreatureIdentity(
            creature.CombatId,
            creature.Side,
            creature.ModelId,
            creature.Player?.NetId);
    }
}

public sealed record CardSourceIdentity(
    ModelId ModelId,
    ulong OwnerNetId,
    PileType? Pile,
    ModelId? PersistentModelId)
{
    public static CardSourceIdentity FromCard(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return new CardSourceIdentity(
            card.Id,
            card.Owner.NetId,
            card.Pile?.Type,
            card.DeckVersion?.Id);
    }
}

public sealed record PowerChangeEvent(
    int Round,
    CreatureIdentity Target,
    ModelId PowerModelId,
    string PowerRuntimeType,
    PowerType DeclaredPowerType,
    PowerType EffectivePowerType,
    decimal Delta,
    int AmountBefore,
    int AmountAfter,
    PowerChangeKind Kind,
    CreatureIdentity? Applier,
    CardSourceIdentity? CardSource)
{
    public bool IsGain => Delta > 0m;

    public bool IsLoss => Delta < 0m;

    public bool IsRemoval => Kind is PowerChangeKind.FullRemoval or PowerChangeKind.DirectRemoval;
}
