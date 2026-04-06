using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class EnduranceCard : TogawaSakikoCard
{
    public EnduranceCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<EndurancePower>(5, upgrade: 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<EndurancePower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Endurance", "Apply !EndurancePower! Endurance: when you take damage, gain equal Block.");
}
