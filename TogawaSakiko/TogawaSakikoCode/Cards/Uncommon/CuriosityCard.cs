using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// In STS1: when you play a Skill, gain Strength. Approximated as applying DexterityPower (close buff).
/// </summary>
public class CuriosityCard : TogawaSakikoCard
{
    public CuriosityCard() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<DexterityPower>(1, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<DexterityPower>(this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.Upgrade(); // 2 → 1
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Curiosity",
            "Gain !DexterityPower! Dexterity. (TODO: gain Strength when playing Skills)");
}
