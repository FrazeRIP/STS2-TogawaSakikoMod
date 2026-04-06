using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// In STS1: at end of turn gain (6-plays) Hype; upgraded = no decrement.
/// Simplified: apply SeizeTheFatePower which gives Hype at end of turn.
/// </summary>
public class SeizeTheFateCard : TogawaSakikoCard
{
    public SeizeTheFateCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<SeizeTheFatePower>(6, upgrade: 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<SeizeTheFatePower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Seize the Fate",
            "Apply Seize the Fate (!SeizeTheFatePower! stacks): at end of each turn, gain that much Hype.");
}
