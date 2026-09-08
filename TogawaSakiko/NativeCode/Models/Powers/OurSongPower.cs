using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class OurSongPower : PowerModel
{
    private sealed class Data
    {
        public readonly Dictionary<CardModel, int> AmountsForAttackCards = new();
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Block)];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner && cardPlay.Card.Type == CardType.Attack)
        {
            GetInternalData<Data>().AmountsForAttackCards[cardPlay.Card] = Amount;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer == Owner &&
            cardSource is not null &&
            props.IsPoweredAttack() &&
            result.UnblockedDamage > 0 &&
            GetInternalData<Data>().AmountsForAttackCards.TryGetValue(cardSource, out int amount) &&
            amount > 0)
        {
            Flash();
            await CreatureCmd.GainBlock(
                Owner,
                result.UnblockedDamage,
                ValueProp.Unpowered,
                null,
                fast: true);
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner &&
            GetInternalData<Data>().AmountsForAttackCards.Remove(cardPlay.Card, out int amount) &&
            amount > 0)
        {
            await PowerCmd.Decrement(this);
        }
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
