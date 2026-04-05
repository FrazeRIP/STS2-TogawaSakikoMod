using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

/// <summary>
/// In STS1, damage grows each turn this stays in hand. Simplified: fixed damage.
/// </summary>
public class MasqueradeRhapsodyRequestCard : TogawaSakikoCard
{
    public MasqueradeRhapsodyRequestCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8, upgrade: 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Masquerade Rhapsody Request",
            "Deal !Damage! damage. (TODO: damage grows each turn it remains in hand)");
}
