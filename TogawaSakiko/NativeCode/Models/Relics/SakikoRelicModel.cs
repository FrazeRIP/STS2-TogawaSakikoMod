using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Relics;

public abstract class SakikoRelicModel : RelicModel
{
    protected abstract string AssetStem { get; }

    public override string PackedIconPath => NativeAssetPaths.RelicIcon(AssetStem);

    protected override string PackedIconOutlinePath => NativeAssetPaths.RelicOutline(AssetStem);

    protected override string BigIconPath => NativeAssetPaths.RelicBigIcon(AssetStem);
}
