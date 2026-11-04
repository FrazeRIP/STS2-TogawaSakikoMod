using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class OblivionisCard : CardModel
{
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public override bool HasTurnEndInHandEffect => true;

    public override string PortraitPath => NativeAssetPaths.OblivionisPortrait;

    public OblivionisCard()
        : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
    {
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        if (Pile?.Type != PileType.Hand)
        {
            return;
        }

        var cards = PileType.Hand.GetPile(Owner).Cards.ToArray();
        foreach (var card in cards)
        {
            if (card.Pile?.Type == PileType.Hand)
            {
                await CardCmd.Exhaust(choiceContext, card, false, false);
            }
        }
    }
}
