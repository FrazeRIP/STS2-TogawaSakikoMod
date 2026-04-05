using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Curse;

public class DolorisCard : TogawaSakikoCard
{
    public DolorisCard() : base(0, CardType.Curse, CardRarity.Curse, TargetType.Self, autoAdd: false)
    {
        WithKeywords(CardKeyword.Unplayable);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Task.CompletedTask;
    }

    public override bool HasTurnEndInHandEffect => true;

    public override async Task OnTurnEndInHand(PlayerChoiceContext ctx)
    {
        Flash();
        await CreatureCmd.Damage(ctx, Owner.Creature, 2m, DamageProps.nonCardUnpowered, null);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Doloris", "Unplayable. At the end of your turn, take 2 damage.");
}
