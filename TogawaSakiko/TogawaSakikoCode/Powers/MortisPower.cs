using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.TogawaSakikoCode.Cards.Curse;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Debuff: when the owner loses HP, add a Mortis curse card to the draw pile.
/// </summary>
public class MortisPower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Mortis",
            "When you lose HP, add a Mortis curse to your draw pile. (!Amount! stack(s))",
            "When you lose HP, add a Mortis curse to your draw pile. ({Amount} stack(s))"
        );

    public override async Task AfterDamageReceived(
        PlayerChoiceContext ctx, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.DamageDealt > 0 && Owner.Player != null)
        {
            var curse = new MortisCard();
            await CardPileCmd.AddGeneratedCardToCombat(curse, PileType.Draw, addedByPlayer: false);
        }
    }
}
