using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Token;

public class BlackKeysCard : TogawaSakikoCard
{
    public BlackKeysCard() : base(0, CardType.Power, CardRarity.Token, TargetType.Self, autoAdd: false)
    {
        WithPower<StrengthPower>(2, upgrade: 1);
        WithPower<DazzlingPower>("DazzlingEnemy", 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<StrengthPower>(this);
        // Apply DazzlingPower to first enemy
        var enemies = CombatState?.Enemies;
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                await PowerCmd.Apply<DazzlingPower>(enemy, DynamicVars["DazzlingEnemy"].IntValue, Owner.Creature, this);
                break;
            }
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Black Keys", "Gain !StrengthPower! Strength. Apply 1 Dazzling to an enemy.");
}
