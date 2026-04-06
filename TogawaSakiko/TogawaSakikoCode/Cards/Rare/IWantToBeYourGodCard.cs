using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

/// <summary>
/// In STS1: deal 15 dmg; consume DazzlingPower to enter Divinity Stance.
/// TODO: stance system not in STS2. Simplified: deal 15 dmg, consume DazzlingPower to gain Strength.
/// </summary>
public class IWantToBeYourGodCard : TogawaSakikoCard
{
    public IWantToBeYourGodCard() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(15, upgrade: 5);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        var dazzling = Owner.Creature.Powers.FirstOrDefault(p => p is DazzlingPower) as DazzlingPower;
        if (dazzling != null && dazzling.Amount > 0)
        {
            int stacks = dazzling.Amount;
            await PowerCmd.Remove<DazzlingPower>(Owner.Creature);
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, stacks, Owner.Creature, this);
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("I Want to Be Your God",
            "Deal !Damage! damage. Consume all Dazzling to gain equal Strength. (TODO: Divinity Stance)");
}
