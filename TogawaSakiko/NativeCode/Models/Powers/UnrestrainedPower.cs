using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class UnrestrainedPower : PowerModel
{
    private sealed class Data
    {
        public int Draws;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Description => PopulateDescription(base.Description);

    protected override object InitInternalData()
    {
        return new Data();
    }

    internal LocString PopulateDescription(LocString text)
    {
        text.Add("Amount", IsMutable ? Amount : 1);
        text.Add("Draws", IsMutable ? GetInternalData<Data>().Draws : 1);
        return text;
    }

    internal static async Task AddAsync(PlayerChoiceContext context, CardModel source)
    {
        UnrestrainedPower? power = await PowerCmd.Apply<UnrestrainedPower>(
            context,
            source.Owner.Creature,
            source.DynamicVars["MagicNumber"].BaseValue,
            source.Owner.Creature,
            source);
        if (power is not null)
        {
            power.GetInternalData<Data>().Draws++;
        }
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card is not DesireCard || card.Owner.Creature != Owner || card.Pile?.Type != PileType.Hand)
        {
            return;
        }

        int draws = GetInternalData<Data>().Draws;
        int ardor = Amount;
        await CardCmd.Exhaust(choiceContext, card, false, false);
        await CardPileCmd.Draw(choiceContext, draws, card.Owner);
        await PowerCmd.Apply<ArdorPower>(choiceContext, Owner, ardor, Owner, null);
    }
}
