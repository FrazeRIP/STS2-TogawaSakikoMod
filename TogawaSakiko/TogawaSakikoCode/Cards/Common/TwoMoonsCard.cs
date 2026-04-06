using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class TwoMoonsCard : TogawaSakikoCard
{
    public TwoMoonsCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(7, upgrade: 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        var dazzling = Owner.Creature.Powers.FirstOrDefault(p => p is DazzlingPower);
        if (dazzling != null && dazzling.Amount > 0)
        {
            await CommonActions.CardAttack(this, play).Execute(ctx);
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Two Moons", "Deal !Damage! damage. If you have Dazzling, deal !Damage! damage again.");
}
