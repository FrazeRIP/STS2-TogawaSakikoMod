using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class HeartsBarrierCard : TogawaSakikoCard
{
    public HeartsBarrierCard() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithKeywords(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // Block = deck size
        int deckSize = Owner.Deck.Cards.Count;
        if (IsUpgraded) deckSize += 2;
        await CreatureCmd.GainBlock(Owner.Creature, deckSize, BlockProps.card, play);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Heart's Barrier", "Gain Block equal to your deck size. Retain.");
}
