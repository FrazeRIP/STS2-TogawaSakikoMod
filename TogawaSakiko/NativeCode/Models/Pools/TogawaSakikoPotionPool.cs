using Godot;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Models.Pools;

public sealed class TogawaSakikoPotionPool : PotionPoolModel
{
    public override string EnergyColorName => "defect";

    public override Color LabOutlineColor => new("8295A8");

    protected override IEnumerable<PotionModel> GenerateAllPotions()
    {
        return Array.Empty<PotionModel>();
    }
}
