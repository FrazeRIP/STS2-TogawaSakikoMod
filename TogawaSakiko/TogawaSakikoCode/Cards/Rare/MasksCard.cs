using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

/// <summary>
/// In STS1: consume 2 DazzlingPower; gain 2 Strength; next turn play Masks again.
/// Simplified: consume 2 DazzlingPower (if available), gain 2 Strength.
/// </summary>
public class MasksCard : TogawaSakikoCard
{
    public MasksCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<StrengthPower>(2, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var dazzling = Owner.Creature.Powers.FirstOrDefault(p => p is DazzlingPower);
        if (dazzling != null && dazzling.Amount >= 2)
        {
            await PowerCmd.ModifyAmount(dazzling, -2, Owner.Creature, this);
            Flash();
        }
        await CommonActions.ApplySelf<StrengthPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Masks",
            "Consume 2 Dazzling (if available). Gain !StrengthPower! Strength. (TODO: replay Masks next turn)");
}
