using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class TheThirdMovement : SakikoRelicModel
{
    internal const int StartingUses = 3;
    internal const int DamageMultiplier = 3;

    private int _remainingUses = StartingUses;

    protected override string AssetStem => "thethirdmovement";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool IsUsedUp => RemainingUses <= 0;

    public override bool ShowCounter => true;

    public override int DisplayAmount => Math.Max(0, RemainingUses);

    [SavedProperty]
    public int RemainingUses
    {
        get => _remainingUses;
        set
        {
            AssertMutable();
            _remainingUses = Math.Max(0, value);
            Status = _remainingUses > 0 ? RelicStatus.Active : RelicStatus.Disabled;
            InvokeDisplayAmountChanged();
        }
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return ShouldMultiplyDamage(props, dealer, cardSource)
            ? DamageMultiplier
            : 1m;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (RemainingUses > 0 &&
            ReferenceEquals(cardPlay.Card.Owner, Owner) &&
            cardPlay.Card is TheMoonlightSonataCard)
        {
            RemainingUses--;
            Flash();
        }

        return Task.CompletedTask;
    }

    internal bool ShouldMultiplyDamage(ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return IsEligibleDamage(
            RemainingUses > 0,
            props,
            ReferenceEquals(dealer, Owner.Creature) || ReferenceEquals(dealer, Owner.Osty),
            cardSource);
    }

    internal static bool IsEligibleDamage(
        bool hasRemainingUses,
        ValueProp props,
        bool dealerMatchesOwner,
        CardModel? cardSource)
    {
        return hasRemainingUses &&
               props.IsPoweredAttack() &&
               dealerMatchesOwner &&
               cardSource is TheMoonlightSonataCard;
    }
}
