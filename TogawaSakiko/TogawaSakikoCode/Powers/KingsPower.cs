using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Debuff (TODO): in STS1, the player receives one fewer card reward.
/// No card reward modification hook available in STS2. Implemented as a plain debuff placeholder.
/// </summary>
public class KingsPower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "King's Burden",
            "Receive !Amount! fewer card reward(s) after combat. (Not yet implemented)",
            "Receive {Amount} fewer card reward(s) after combat. (Not yet implemented)"
        );
}
