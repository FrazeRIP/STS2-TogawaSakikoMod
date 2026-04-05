using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

/// <summary>
/// In STS1 applies KingsPower (reduced card rewards). Simplified: just deal damage.
/// </summary>
public class KingsCard : TogawaSakikoCard
{
    public KingsCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(14, upgrade: 4);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        // TODO: apply KingsPower debuff (reduce card rewards) — no hook available in STS2
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Kings", "Deal !Damage! damage. (TODO: apply King's Burden debuff)");
}
