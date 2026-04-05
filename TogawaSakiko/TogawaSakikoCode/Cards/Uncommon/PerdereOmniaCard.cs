using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// Unplayable; when drawn, apply 1 PerdereOmniaPower to self.
/// </summary>
public class PerdereOmniaCard : TogawaSakikoCard
{
    public PerdereOmniaCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeywords(CardKeyword.Unplayable);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Task.CompletedTask;
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        if (card == this && Owner.Player != null)
        {
            Flash();
            int amount = IsUpgraded ? 2 : 1;
            await PowerCmd.Apply<PerdereOmniaPower>(Owner.Creature, amount, Owner.Creature, this);
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Perdere Omnia",
            "Unplayable. When drawn, apply 1 Perdere Omnia: whenever you draw a 0-cost card, exhaust it and draw a replacement.");
}
