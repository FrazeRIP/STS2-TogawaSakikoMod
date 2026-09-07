using MegaCrit.Sts2.Core.Modding;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Pools;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Content;

internal static class NativeModelCatalog
{
    public const int GameplayModelCount = 19;

    private static bool _registered;

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        ModHelper.AddModelToPool<TogawaSakikoCardPool, StrikeTogawaSakiko>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, DefendTogawaSakiko>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, TheMoonlightSonataCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, ASplitMomentCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, DesireCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, TwoMoonsCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, SilentFarewellCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, GreetingsCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, TirednessCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, MelodyCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, IdealCard>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, StarterRelicTogawaSakiko>();

        _registered = true;
    }
}
