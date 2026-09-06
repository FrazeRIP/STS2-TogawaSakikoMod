using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Characters;
using TogawaSakiko.NativeCode.Models.Pools;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Content;

internal static class NativeStableIds
{
    public const string EntryPrefix = "TOGAWASAKIKO-";

    public static readonly IReadOnlyDictionary<Type, string> Entries = new Dictionary<Type, string>
    {
        [typeof(Models.Characters.TogawaSakiko)] = EntryPrefix + "TOGAWA_SAKIKO",
        [typeof(TogawaSakikoCardPool)] = EntryPrefix + "TOGAWA_SAKIKO_CARD_POOL",
        [typeof(TogawaSakikoRelicPool)] = EntryPrefix + "TOGAWA_SAKIKO_RELIC_POOL",
        [typeof(TogawaSakikoPotionPool)] = EntryPrefix + "TOGAWA_SAKIKO_POTION_POOL",
        [typeof(StrikeTogawaSakiko)] = EntryPrefix + "STRIKE_TOGAWA_SAKIKO",
        [typeof(DefendTogawaSakiko)] = EntryPrefix + "DEFEND_TOGAWA_SAKIKO",
        [typeof(TheMoonlightSonataCard)] = EntryPrefix + "THE_MOONLIGHT_SONATA_CARD",
        [typeof(ASplitMomentCard)] = EntryPrefix + "A_SPLIT_MOMENT_CARD",
        [typeof(DesireCard)] = EntryPrefix + "DESIRE_CARD",
        [typeof(TwoMoonsCard)] = EntryPrefix + "TWO_MOONS_CARD",
        [typeof(SilentFarewellCard)] = EntryPrefix + "SILENT_FAREWELL_CARD",
        [typeof(DazzlingPower)] = EntryPrefix + "DAZZLING_POWER",
        [typeof(StarterRelicTogawaSakiko)] = EntryPrefix + "STARTER_RELIC_TOGAWA_SAKIKO"
    };

    public static bool IsOwnedModel(Type type)
    {
        return type.Assembly == typeof(NativeStableIds).Assembly &&
               type.IsSubclassOf(typeof(AbstractModel));
    }
}
