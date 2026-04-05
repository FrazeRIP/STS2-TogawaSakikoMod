using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

/// <summary>
/// In STS1: deal 30 dmg; cost = deck size / 6.
/// TODO: dynamic cost not supported. Fixed cost 3, deal 30 dmg.
/// </summary>
public class SpringSunlightCard : TogawaSakikoCard
{
    public SpringSunlightCard() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(30, upgrade: 10);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Spring Sunlight",
            "Deal !Damage! damage. (TODO: cost = deck size / 6)");
}
