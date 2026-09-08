using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

/// <summary>
/// In STS1: play all 4 Symbol cards + Ether in sequence.
/// TODO: Complex sequence action. Simplified: deal 7 damage.
/// </summary>
public class AveMujicaCard : TogawaSakikoCard
{
    public AveMujicaCard() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(7, upgrade: 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Ave Mujica",
            "Deal !Damage! damage. (TODO: play all Symbol cards and Ether in sequence)");
}
