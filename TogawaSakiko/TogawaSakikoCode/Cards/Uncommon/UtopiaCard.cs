using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class UtopiaCard : TogawaSakikoCard
{
    public UtopiaCard() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(15, upgrade: 5);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // HP loss: bypasses block (Unblockable)
        if (play.Target != null)
        {
            await CreatureCmd.Damage(ctx, play.Target, DynamicVars.Damage.BaseValue,
                DamageProps.cardHpLoss, this);
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Utopia", "Deal !Damage! HP loss to target (bypasses Block).");
}
