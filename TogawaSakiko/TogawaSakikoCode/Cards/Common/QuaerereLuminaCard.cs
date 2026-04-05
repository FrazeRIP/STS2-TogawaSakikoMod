using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

/// <summary>
/// In STS1 Scry 7 and gain 1 Block per card scryed. No Scry in STS2.
/// Simplified: draw 2 cards + gain 7 block.
/// </summary>
public class QuaerereLuminaCard : TogawaSakikoCard
{
    public QuaerereLuminaCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCards(2, upgrade: 1);
        WithBlock(7, upgrade: 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.Draw(this, ctx);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, BlockProps.card, play);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Quaerere Lumina",
            "Draw !Cards! cards. Gain !Block! Block. (TODO: Scry version unavailable in STS2)");
}
