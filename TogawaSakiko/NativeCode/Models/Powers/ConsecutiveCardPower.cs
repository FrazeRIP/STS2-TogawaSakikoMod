using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Models.Powers;

/// <summary>
/// Tracks the owner's previous card and rewards playing the same card again.
/// Descriptions show it through <c>LastCard</c>, or the power's <c>noPreviousCard</c> text.
/// </summary>
public abstract class ConsecutiveCardPower : PowerModel
{
    private sealed class Data
    {
        public string? PreviousName;

        public string? PreviousTitle;
    }

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            string? title = IsMutable ? GetInternalData<Data>().PreviousTitle : null;
            description.Add(
                "LastCard",
                title ?? new LocString("powers", Id.Entry + ".noPreviousCard").GetFormattedText());
            return description;
        }
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        string name = cardPlay.Card.Id.ToString();
        bool matched = data.PreviousName == name;
        data.PreviousName = name;
        data.PreviousTitle = cardPlay.Card.Title;
        InvokeDisplayAmountChanged();
        if (matched)
        {
            Flash();
            await OnConsecutiveCardAsync(choiceContext, cardPlay);
        }
    }

    protected abstract Task OnConsecutiveCardAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay);
}
