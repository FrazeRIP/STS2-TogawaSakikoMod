using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class HachibouseiDanceCard : TogawaSakikoCard
{
    public HachibouseiDanceCard() : base(4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8, upgrade: 0);
        WithBlock(8, upgrade: 4);
        // Upgrade reduces cost by 1 — handled via upgrade logic
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        await CommonActions.CardBlock(this, play);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.Upgrade(); // 4 → 3
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Hachibousei Dance", "Deal !Damage! damage. Gain !Block! Block.");
}
