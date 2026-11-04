using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class TimorisCard : CardModel
{
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public override string PortraitPath => NativeAssetPaths.TimorisPortrait;

    public TimorisCard()
        : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
    {
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (!ElementCurseDraw.ShouldTrigger(this, card))
        {
            return;
        }

        await PowerCmd.Apply<FrailPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
