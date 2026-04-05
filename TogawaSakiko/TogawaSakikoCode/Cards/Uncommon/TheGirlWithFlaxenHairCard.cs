using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class TheGirlWithFlaxenHairCard : TogawaSakikoCard
{
    public TheGirlWithFlaxenHairCard() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(7, upgrade: 3);
        WithPower<DazzlingPower>(3, upgrade: 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        await CommonActions.ApplySelf<DazzlingPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("The Girl With Flaxen Hair", "Deal !Damage! damage. Gain !DazzlingPower! Dazzling.");
}
