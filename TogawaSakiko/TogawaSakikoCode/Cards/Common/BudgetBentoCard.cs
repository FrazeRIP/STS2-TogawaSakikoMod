using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class BudgetBentoCard : TogawaSakikoCard
{
    public BudgetBentoCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<RegenPower>(4, upgrade: 1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<RegenPower>(this);
        var tired = new TirednessCard();
        await CardPileCmd.AddGeneratedCardToCombat(tired, PileType.Discard, addedByPlayer: false);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Budget Bento", "Gain !RegenPower! Regen. Add Tiredness to discard. Exhaust.");
}
