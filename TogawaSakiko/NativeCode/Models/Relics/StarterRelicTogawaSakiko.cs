using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Presentation;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class StarterRelicTogawaSakiko : RelicModel
{
    private ICombatState? _rewardedCombat;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override string PackedIconPath => NativeAssetPaths.MonochromeHairbandIcon;

    protected override string PackedIconOutlinePath => NativeAssetPaths.MonochromeHairbandOutline;

    protected override string BigIconPath => NativeAssetPaths.MonochromeHairbandBigIcon;

    public override Task BeforeCardRemoved(CardModel card)
    {
        N3ContractDiagnostics.RecordBeforeCardRemoved(card);
        return Task.CompletedTask;
    }

    public override async Task BeforeCombatStart()
    {
        ICombatState? combat = Owner.Creature.CombatState;
        if (Owner.Creature.IsDead || combat is null || ReferenceEquals(_rewardedCombat, combat))
        {
            return;
        }

        // The permanent Desire also gets a linked copy in Hand for this combat.
        _rewardedCombat = combat;
        Flash();
        CardPileAddResult result = await PersistentDeckMutation.AddCanonicalAsync<DesireCard>(Owner);
        HairbandCardPreview.Show(result);
        NativeSmokeTrace.Info($"Monochrome Hairband added Desire to the deck; success={result.success}.");
    }

    internal static bool IsEligibleVictory(string actEntry, string encounterEntry)
    {
        return !IsOutOfScopeRoute(actEntry) && !IsOutOfScopeRoute(encounterEntry);
    }

    private static bool IsOutOfScopeRoute(string entry)
    {
        return entry.StartsWith(NativeStableIds.EntryPrefix, StringComparison.Ordinal);
    }
}
