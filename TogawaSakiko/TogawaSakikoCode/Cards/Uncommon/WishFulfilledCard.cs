using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// TODO: WishFulfilledAction not clearly available. Simplified: gain 10 block, Exhaust, Ethereal.
/// </summary>
public class WishFulfilledCard : TogawaSakikoCard
{
    public WishFulfilledCard() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(10, upgrade: 5);
        WithKeywords(CardKeyword.Ethereal, CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Wish Fulfilled", "Gain !Block! Block. Ethereal. Exhaust. (TODO: full WishFulfilled effect)");
}
