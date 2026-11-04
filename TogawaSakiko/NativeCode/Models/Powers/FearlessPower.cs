using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Presentation;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class FearlessPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.Player is null || Amount <= 0)
        {
            return;
        }

        CardModel[] candidates = SakikoPurgeCommand.GetCandidates(Owner.Player);
        CardModel? selected = null;
        if (candidates.Length > 0)
        {
            using var locationTips = PurgeCardLocationTips.ShowFor(candidates);
            selected = (await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    candidates,
                    Owner.Player,
                    new CardSelectorPrefs(SelectionScreenPrompt, 0, 1)
                    {
                        Cancelable = true
                    }))
                .FirstOrDefault();
        }

        if (selected is not null)
        {
            SakikoPurgeResult result = await SakikoPurgeCommand.RemoveAsync(selected);
            if (!result.Success && !result.Prevented)
            {
                NativeSmokeTrace.N5Info(
                    $"Fearless could not purge {selected.Id}: {result.FailureReason}");
            }
        }

        await PowerCmd.Decrement(this);
    }
}
