using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Relics;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

// Keep native setup, previews, transformation, enchantments, and saved reward fields.
[HarmonyPatch(typeof(ArchaicTooth), "get_TranscendenceUpgrades")]
internal static class SakikoArchaicToothMappingPatch
{
    private static void Postfix(Dictionary<ModelId, CardModel> __result)
    {
        __result[ModelDb.Card<TheMoonlightSonataCard>().Id] = ModelDb.Card<TheThirdMovementCard>();
    }
}

[HarmonyPatch(typeof(TouchOfOrobas), "get_RefinementUpgrades")]
internal static class SakikoOrobasMappingPatch
{
    private static void Postfix(Dictionary<ModelId, RelicModel> __result)
    {
        RelicModel replacement = ModelDb.Relic<EnchantedHairband>();
        __result[ModelDb.Relic<StarterRelicTogawaSakiko>().Id] = replacement;
        __result[ModelDb.Relic<BlazingHairband>().Id] = replacement;
        __result[ModelDb.Relic<AnotherMask>().Id] = replacement;
    }
}

[HarmonyPatch(typeof(TouchOfOrobas), "GetStarterRelic")]
internal static class SakikoOrobasStarterPatch
{
    internal static bool IsEligible(RelicModel relic) => !relic.IsMelted &&
        (relic is StarterRelicTogawaSakiko or AnotherMask || relic.GetType() == typeof(BlazingHairband));

    private static bool Prefix(Player p, ref RelicModel? __result)
    {
        if (p.Character is not SakikoCharacter)
        {
            return true;
        }

        __result = p.Relics.FirstOrDefault(IsEligible);
        return false;
    }
}
