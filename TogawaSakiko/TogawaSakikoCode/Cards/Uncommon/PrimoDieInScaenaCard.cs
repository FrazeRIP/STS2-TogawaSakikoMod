using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// In STS1: apply PrimoDieInScaenaPower (custom power not in the list).
/// Simplified: apply 1 Strength. Upgrade = Innate.
/// </summary>
public class PrimoDieInScaenaCard : TogawaSakikoCard
{
    public PrimoDieInScaenaCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, 1, Owner.Creature, this);
    }
    // TODO: OnUpgrade not supported in STS2. Upgrade: WithKeywords(CardKeyword.Innate);

    public override List<(string, string)>? Localization =>
        new CardLoc("Primo Die in Scaena",
            "Gain 1 Strength. Upgraded: Innate. (TODO: PrimoDieInScaenaPower not available)");
}
