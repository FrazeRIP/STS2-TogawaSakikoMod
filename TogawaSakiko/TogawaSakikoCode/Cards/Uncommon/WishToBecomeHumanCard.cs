using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class WishToBecomeHumanCard : TogawaSakikoCard
{
    public WishToBecomeHumanCard() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(2, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // Hit twice and gain DazzlingPower equal to total damage dealt
        decimal totalDamage = 0;
        var result1 = await CommonActions.CardAttack(this, play, hitCount: 2).Execute(ctx);
        // Approximate: gain Dazzling equal to base damage * 2 hits
        int dazzlingAmount = DynamicVars.Damage.IntValue * 2;
        if (IsUpgraded) dazzlingAmount += 2;
        await PowerCmd.Apply<DazzlingPower>(Owner.Creature, dazzlingAmount, Owner.Creature, this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Wish to Become Human",
            "Hit twice for !Damage! damage. Gain Dazzling equal to total damage dealt.");
}
