using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// In STS1: X-cost: deal 4 dmg + 4 block per energy spent.
/// TODO: X-cost not supported. Fixed 2-cost skeleton.
/// </summary>
public class CrucifixXCard : TogawaSakikoCard
{
    public CrucifixXCard() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8, upgrade: 4);
        WithBlock(8, upgrade: 4);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.CardAttack(this, play).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Crucifix X",
            "Gain !Block! Block. Deal !Damage! damage. (TODO: X-cost version unavailable in STS2)");
}
