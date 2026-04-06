using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Buff: when you deal attack damage, gain Block equal to the damage dealt.
/// </summary>
public class OurSongPower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Our Song",
            "Whenever you deal attack damage, gain equal Block.",
            "Whenever you deal attack damage, gain equal Block."
        );

    public override async Task AfterDamageGiven(
        PlayerChoiceContext ctx, Creature? dealer,
        DamageResult result, ValueProp props,
        Creature target, CardModel? cardSource)
    {
        if (dealer == Owner && result.DamageDealt > 0 && (props & ValueProp.Move) != 0)
        {
            await CreatureCmd.GainBlock(Owner, result.DamageDealt, BlockProps.cardUnpowered, null);
        }
    }
}
