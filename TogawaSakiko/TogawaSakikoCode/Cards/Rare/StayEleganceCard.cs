using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

/// <summary>
/// In STS1: obtain a random potion. No potion generation mid-combat in STS2.
/// Simplified: gain 5 Dazzling + 5 Block.
/// </summary>
public class StayEleganceCard : TogawaSakikoCard
{
    public StayEleganceCard() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<DazzlingPower>(5, upgrade: 2);
        WithBlock(5, upgrade: 2);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<DazzlingPower>(this);
        await CommonActions.CardBlock(this, play);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Stay Elegance",
            "Gain !DazzlingPower! Dazzling and !Block! Block. Exhaust. (TODO: obtain a random potion)");
}
