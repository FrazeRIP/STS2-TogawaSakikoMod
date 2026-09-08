using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class RaiseTheBetCard : TogawaSakikoCard
{
    public RaiseTheBetCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithPower<StrengthPower>(3, upgrade: 1);
        WithPower<DazzlingPower>(3, upgrade: 1);
        WithKeywords(CardKeyword.Retain, CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target != null)
        {
            await PowerCmd.Apply<StrengthPower>(play.Target, DynamicVars.Power<StrengthPower>().IntValue, Owner.Creature, this);
        }
        await CommonActions.ApplySelf<DazzlingPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Raise the Bet",
            "Apply !StrengthPower! Strength to enemy. Gain !DazzlingPower! Dazzling. Retain. Exhaust.");
}
