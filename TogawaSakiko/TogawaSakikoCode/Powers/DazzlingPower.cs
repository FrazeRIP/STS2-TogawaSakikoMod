using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Buff: a stackable resource used by many Sakiko cards. Stacks persist across turns.
/// </summary>
public class DazzlingPower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Dazzling",
            "A shimmering resource. Many cards gain power from Dazzling stacks.",
            "A shimmering resource ({Amount} stacks). Many cards gain power from Dazzling stacks."
        );
}
