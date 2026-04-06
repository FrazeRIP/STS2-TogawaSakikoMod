using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Curse;

public class MortisCard : TogawaSakikoCard
{
    public MortisCard() : base(0, CardType.Curse, CardRarity.Curse, TargetType.Self, autoAdd: false)
    {
        WithKeywords(CardKeyword.Unplayable);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Task.CompletedTask;
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        if (card == this && Owner.Player != null)
        {
            await PowerCmd.Apply<MortisPower>(Owner.Creature, 1, Owner.Creature, this);
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Mortis", "Unplayable. When drawn, apply 1 Mortis to yourself.");
}
