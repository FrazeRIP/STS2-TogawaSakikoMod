using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class AnglesCard : TogawaSakikoCard
{
    public AnglesCard() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(9, upgrade: 3);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        await PowerCmd.Apply<GodsCreationPower>(Owner.Creature, 1, Owner.Creature, this);
        if (play.Target != null)
        {
            await PowerCmd.Apply<GodsCreationPower>(play.Target, 1, Owner.Creature, this);
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Angles", "Deal !Damage! damage. Apply 1 God's Creation to self AND target. Exhaust.");
}
