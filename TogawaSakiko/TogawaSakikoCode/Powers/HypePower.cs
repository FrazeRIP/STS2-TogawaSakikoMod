using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Buff (TODO): in STS1 prevents block loss (consumes 1 stack per block-loss event).
/// No block-loss hook available in STS2. Implemented as a plain stackable buff placeholder.
/// </summary>
public class HypePower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Hype",
            "A surge of energy. (!Amount! stack(s))",
            "A surge of energy. ({Amount} stack(s))"
        );
}
