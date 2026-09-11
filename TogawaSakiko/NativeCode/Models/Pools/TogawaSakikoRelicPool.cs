using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Models.Pools;

public sealed class TogawaSakikoRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "defect";

    public override Color LabOutlineColor => new("8295A8");

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return Array.Empty<RelicModel>();
    }

    public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState)
    {
        return AllRelics.Where(relic =>
            relic is not StarterRelicTogawaSakiko and not TheThirdMovement and not AnotherMask and not EnchantedHairband);
    }
}
