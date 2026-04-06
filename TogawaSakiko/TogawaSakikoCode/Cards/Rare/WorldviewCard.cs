using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

public class WorldviewCard : TogawaSakikoCard
{
    public WorldviewCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<WorldviewPower>(this, 1m);
        // TODO: WithKeywords(CardKeyword.Innate) is a constructor-only fluent call and cannot be applied at runtime.
    }
    // TODO: OnUpgrade not supported in STS2. Upgrade: WithKeywords(CardKeyword.Innate);

    public override List<(string, string)>? Localization =>
        new CardLoc("Worldview",
            "Apply Worldview: when you draw an Unplayable card, exhaust it and draw a replacement. Upgraded: Innate.");
}
