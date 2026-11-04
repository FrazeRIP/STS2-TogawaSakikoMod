using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(
    typeof(RelicCmd),
    nameof(RelicCmd.Obtain),
    [typeof(RelicModel), typeof(Player), typeof(int)])]
internal static class SakikoDarkstonePeriaptExclusionPatch
{
    private static readonly Type[] ReplacementTypes =
    [
        typeof(DreamCatcher),
        typeof(HandDrill),
        typeof(MawBank),
        typeof(TheBoot)
    ];

    internal static IReadOnlyList<RelicModel> GetReplacementCandidates(Player player)
    {
        HashSet<ModelId> ownedIds = player.Relics.Select(relic => relic.Id).ToHashSet();
        RelicModel[] unowned = ReplacementTypes
            .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .Where(relic => relic.Rarity == RelicRarity.Event && relic.IsAllowed(player.RunState) && !ownedIds.Contains(relic.Id))
            .ToArray();

        if (unowned.Length > 0)
        {
            return unowned;
        }

        return ReplacementTypes
            .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .Where(relic => relic.Rarity == RelicRarity.Event && relic.IsAllowed(player.RunState))
            .ToArray();
    }

    internal static RelicModel ReplaceIfForbidden(RelicModel relic, Player player)
    {
        if (player.Character is not SakikoCharacter || relic is not DarkstonePeriapt)
        {
            return relic;
        }

        IReadOnlyList<RelicModel> candidates = GetReplacementCandidates(player);
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException(
                "Darkstone Periapt could not be replaced because no eligible Event relic is available.");
        }

        RelicModel replacement = (player.PlayerRng.Rewards.NextItem(candidates)
            ?? throw new InvalidOperationException("Darkstone Periapt replacement selection returned no relic."))
            .ToMutable();
        replacement.IsWax = relic.IsWax;
        replacement.IsMelted = relic.IsMelted;
        return replacement;
    }

    private static void Prefix(ref RelicModel relic, Player player)
    {
        relic = ReplaceIfForbidden(relic, player);
    }
}
