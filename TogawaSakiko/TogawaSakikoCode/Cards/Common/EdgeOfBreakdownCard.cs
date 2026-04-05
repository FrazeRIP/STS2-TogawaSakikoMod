using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class EdgeOfBreakdownCard : TogawaSakikoCard
{
    public EdgeOfBreakdownCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<VulnerablePower>(2, upgrade: -1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // Select a card from discard pile to exhaust
        var selected = await CommonActions.SelectSingleCard(this,
            new MegaCrit.Sts2.Core.Localization.LocString("cards", "edgeofbreakdown.prompt"),
            ctx, PileType.Discard);
        if (selected != null)
        {
            await CardPileCmd.Add(selected, PileType.Exhaust);
        }
        await CommonActions.ApplySelf<VulnerablePower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Edge of Breakdown",
            "Exhaust a card from your discard. Gain !VulnerablePower! Vulnerable. Exhaust.",
            ("prompt", "Choose a card to exhaust."));
}
