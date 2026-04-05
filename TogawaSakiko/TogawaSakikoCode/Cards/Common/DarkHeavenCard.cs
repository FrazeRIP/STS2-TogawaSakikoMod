using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class DarkHeavenCard : TogawaSakikoCard
{
    public DarkHeavenCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(9, upgrade: 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        // Steal 1 Strength: -1 on enemy, +1 on self
        if (play.Target != null)
        {
            await PowerCmd.Apply<StrengthPower>(play.Target, -1, Owner.Creature, this);
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, 1, Owner.Creature, this);
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Dark Heaven", "Deal !Damage! damage. Steal 1 Strength from the enemy.");
}
