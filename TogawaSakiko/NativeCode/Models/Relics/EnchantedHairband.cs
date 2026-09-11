using MegaCrit.Sts2.Core.Runs;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class EnchantedHairband : BlazingHairband
{
    protected override string AssetStem => "enchantedhairband";

    protected override bool UpgradeReward => true;

    // Orobas owns replacement; this relic must not remove another eligible relic.
    public override Task AfterObtained() => Task.CompletedTask;

    public override bool IsAllowed(IRunState runState) => false;
}
