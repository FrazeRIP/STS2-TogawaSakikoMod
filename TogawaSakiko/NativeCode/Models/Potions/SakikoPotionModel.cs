using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Potions;

public abstract class SakikoPotionModel : PotionModel
{
    internal abstract string AssetFolder { get; }

    internal string NativeContainerPath => NativeAssetPaths.PotionContainer(AssetFolder);

    internal string NativeOutlinePath => NativeAssetPaths.PotionOutline(AssetFolder);
}
