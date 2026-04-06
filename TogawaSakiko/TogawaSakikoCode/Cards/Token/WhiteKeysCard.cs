using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Token;

public class WhiteKeysCard : TogawaSakikoCard
{
    public WhiteKeysCard() : base(0, CardType.Power, CardRarity.Token, TargetType.Self, autoAdd: false)
    {
        WithPower<DazzlingPower>(2, upgrade: 1);
        WithPower<StrengthPower>("StrengthEnemy", 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<DazzlingPower>(this);
        var enemies = CombatState?.Enemies;
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                await PowerCmd.Apply<StrengthPower>(enemy, DynamicVars["StrengthEnemy"].IntValue, Owner.Creature, this);
                break;
            }
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("White Keys", "Gain !DazzlingPower! Dazzling. Apply 1 Strength to an enemy.");
}
