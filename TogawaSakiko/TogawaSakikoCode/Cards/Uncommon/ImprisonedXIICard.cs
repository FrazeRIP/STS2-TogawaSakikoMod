using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// In STS1: Deal 8 dmg; Retain; gain 1 energy per Desire played this combat.
/// TODO: Energy gain mid-combat not available. Simplified: deal 8 dmg, Retain.
/// </summary>
public class ImprisonedXIICard : TogawaSakikoCard
{
    public ImprisonedXIICard() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8, upgrade: 4);
        WithKeywords(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Imprisoned XII",
            "Deal !Damage! damage. Retain. (TODO: gain energy per Desire played)");
}
