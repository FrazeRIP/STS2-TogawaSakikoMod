using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class GeorgetteMeGeorgetteYouCard : TogawaSakikoCard
{
    public GeorgetteMeGeorgetteYouCard() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, hitCount: 3).Execute(ctx);
        if (play.Target != null)
        {
            await PowerCmd.Apply<StrengthPower>(play.Target, 1, Owner.Creature, this);
            if (IsUpgraded)
            {
                await PowerCmd.Apply<HypePower>(play.Target, 1, Owner.Creature, this);
            }
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Georgette Me, Georgette You",
            "Hit 3 times for !Damage! damage. Apply 1 Strength to enemy. Upgraded: also apply 1 Hype.");
}
