using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// In STS1: Deal 5 dmg (+4 per turn retained); Retain.
/// TODO: Growing damage per retain turn is custom tracking. Simplified: deal 5 dmg + Retain.
/// </summary>
public class KaoCard : TogawaSakikoCard
{
    public KaoCard() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(5, upgrade: 4);
        WithKeywords(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Kao", "Deal !Damage! damage. Retain. (TODO: damage grows per turn retained)");
}
