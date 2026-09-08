using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Commands;

public enum DivinityEntryStatus
{
    Entered,
    AlreadyActive,
    Prevented
}

public sealed record DivinityEntryResult(
    DivinityEntryStatus Status,
    Creature Target,
    MonsterDivinityPower? Power)
{
    public bool Entered => Status == DivinityEntryStatus.Entered;
}

public static class SakikoStanceCmd
{
    public const int MantraThreshold = 10;
    public const int DivinityEnergyGain = 3;
    public const int DivinityDamageMultiplier = 3;

    public static async Task<DivinityEntryResult> EnterDivinityAsync(
        PlayerChoiceContext choiceContext,
        Creature target,
        Creature? applier,
        CardModel? cardSource)
    {
        ArgumentNullException.ThrowIfNull(choiceContext);
        ArgumentNullException.ThrowIfNull(target);

        MonsterDivinityPower? existing = target.GetPower<MonsterDivinityPower>();
        if (existing is not null)
        {
            return new DivinityEntryResult(DivinityEntryStatus.AlreadyActive, target, existing);
        }

        MonsterDivinityPower? applied = await PowerCmd.Apply<MonsterDivinityPower>(
            choiceContext,
            target,
            1m,
            applier,
            cardSource);
        return applied is null
            ? new DivinityEntryResult(DivinityEntryStatus.Prevented, target, null)
            : new DivinityEntryResult(DivinityEntryStatus.Entered, target, applied);
    }
}
