using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

/// <summary>
/// In STS1: X-cost, apply CrychicPower (X amount). Fixed 2-cost → apply 3 CrychicPower.
/// </summary>
public class CrychicCard : TogawaSakikoCard
{
    public CrychicCard() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<CrychicPower>(3, upgrade: 1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<CrychicPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Crychic",
            "Apply !CrychicPower! Crychic: at the start of each turn, add a Melody to hand. Exhaust.");
}
