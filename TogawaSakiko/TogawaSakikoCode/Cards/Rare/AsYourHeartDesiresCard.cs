using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

/// <summary>
/// In STS1: choose a card from your deck to get a copy of, add permanently.
/// TODO: no "choose from deck and copy" API. Simplified: draw 3 cards.
/// </summary>
public class AsYourHeartDesiresCard : TogawaSakikoCard
{
    public AsYourHeartDesiresCard() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCards(3, upgrade: 0);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.Draw(this, ctx);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.Upgrade(); // 2 → 1
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("As Your Heart Desires",
            "Draw !Cards! cards. Exhaust. (TODO: choose a deck card to duplicate)");
}
