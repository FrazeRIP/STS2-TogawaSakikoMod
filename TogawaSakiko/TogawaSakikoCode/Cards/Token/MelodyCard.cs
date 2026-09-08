using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Token;

public class MelodyCard : TogawaSakikoCard
{
    public MelodyCard() : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, autoAdd: false)
    {
        WithDamage(6, upgrade: 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Melody", "Deal !Damage! damage.");
}
