using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class NeverGiveYouUpCard : TogawaSakikoCard
{
    public NeverGiveYouUpCard() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var selected = await CommonActions.SelectSingleCard(this,
            new MegaCrit.Sts2.Core.Localization.LocString("cards", "nevergiveyouup.prompt"),
            ctx, PileType.Discard);
        if (selected != null)
        {
            await CardPileCmd.Add(selected, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.Upgrade(); // 2 → 1
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Never Give You Up",
            "Return a card from your discard to your hand. Exhaust.",
            ("prompt", "Choose a card to return to hand."));
}
