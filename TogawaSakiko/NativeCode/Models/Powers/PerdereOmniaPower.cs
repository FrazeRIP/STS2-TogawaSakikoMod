using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Diagnostics;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class PerdereOmniaPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Unplayable)];

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        if (Amount <= 0 ||
            card.Owner.Creature != Owner ||
            !card.Keywords.Contains(CardKeyword.Unplayable) ||
            !card.IsRemovable)
        {
            return;
        }

        SakikoPurgeResult result = await SakikoPurgeCommand.RemoveAsync(
            card,
            choiceContext: choiceContext);
        if (!result.Success)
        {
            if (!result.Prevented)
            {
                NativeSmokeTrace.N5Info(
                    $"Perdere Omnia could not purge {card.Id}: {result.FailureReason}");
            }

            return;
        }

        Flash();
        await PowerCmd.Decrement(this);
        if (Owner.Player is not null)
        {
            await CardPileCmd.Draw(choiceContext, 1, Owner.Player);
        }
    }
}
