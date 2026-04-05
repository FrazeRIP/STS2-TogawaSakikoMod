using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

public class CrueltyCard : TogawaSakikoCard
{
    public CrueltyCard() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<VulnerablePower>(2, upgrade: -1);
        WithPower<CrueltyPower>(2, upgrade: 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<VulnerablePower>(Owner.Creature, DynamicVars.Power<VulnerablePower>().IntValue, Owner.Creature, this);
        await CommonActions.ApplySelf<CrueltyPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Cruelty",
            "Apply !VulnerablePower! Vulnerable to self. Apply !CrueltyPower! Cruelty: draw cards when you play a Desire.");
}
