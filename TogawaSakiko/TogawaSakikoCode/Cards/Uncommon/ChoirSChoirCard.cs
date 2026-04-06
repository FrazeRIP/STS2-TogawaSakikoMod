using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// In STS1: Deal 6 dmg 3 times; cost reduced by 1 each time a Desire is played.
/// TODO: Cost reduction on-trigger not supported. Implemented as 3-hit attack with Retain.
/// </summary>
public class ChoirSChoirCard : TogawaSakikoCard
{
    public ChoirSChoirCard() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(6, upgrade: 2);
        WithKeywords(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, hitCount: 3).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Choir's Choir",
            "Hit 3 times for !Damage! damage. Retain. (TODO: cost reduced per Desire played)");
}
