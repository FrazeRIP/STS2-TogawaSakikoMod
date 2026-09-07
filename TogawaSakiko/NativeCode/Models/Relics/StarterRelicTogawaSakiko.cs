using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class StarterRelicTogawaSakiko : RelicModel
{
    internal const string OblivionEntry = "TOGAWASAKIKO-THE_OBLIVION";

    private MegaCrit.Sts2.Core.Combat.CombatState? _rewardedCombat;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override string PackedIconPath => NativeAssetPaths.MonochromeHairbandIcon;

    protected override string PackedIconOutlinePath => NativeAssetPaths.MonochromeHairbandOutline;

    protected override string BigIconPath => NativeAssetPaths.MonochromeHairbandBigIcon;

    public override Task BeforeCardRemoved(CardModel card)
    {
        N3ContractDiagnostics.RecordBeforeCardRemoved(card);
        return Task.CompletedTask;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner.Creature.IsDead ||
            ReferenceEquals(_rewardedCombat, room.CombatState) ||
            !IsEligibleVictory(room.Act.Id.Entry, room.Encounter.Id.Entry))
        {
            return;
        }

        _rewardedCombat = room.CombatState;
        Flash();
        CardPileAddResult result = await PersistentDeckMutation.AddCanonicalAsync<DesireCard>(Owner);
        CardCmd.PreviewCardPileAdd(result, 2f);
        NativeSmokeTrace.Info($"Monochrome Hairband added Desire to the deck; success={result.success}.");
    }

    internal static bool IsEligibleVictory(string actEntry, string encounterEntry)
    {
        return !string.Equals(actEntry, OblivionEntry, StringComparison.Ordinal) &&
               !string.Equals(encounterEntry, OblivionEntry, StringComparison.Ordinal);
    }
}
