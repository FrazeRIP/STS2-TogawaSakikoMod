using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Diagnostics;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class CharismaticFormPower : PowerModel
{
    private static readonly HashSet<Type> ExcludedEnemyPowerTypes =
    [
        typeof(AdaptablePower),
        typeof(AsleepPower),
        typeof(BackAttackLeftPower),
        typeof(BackAttackRightPower),
        typeof(BattlewornDummyTimeLimitPower),
        typeof(BurrowedPower),
        typeof(CrabRagePower),
        typeof(CurlUpPower),
        typeof(EnragePower),
        typeof(EscapeArtistPower),
        typeof(FlutterPower),
        typeof(GalvanicPower),
        typeof(HardToKillPower),
        typeof(HardenedShellPower),
        typeof(HatchPower),
        typeof(HeistPower),
        typeof(HighVoltagePower),
        typeof(IllusionPower),
        typeof(InfestedPower),
        typeof(MinionPower),
        typeof(NemesisPower),
        typeof(PainfulStabsPower),
        typeof(PaperCutsPower),
        typeof(PersonalHivePower),
        typeof(PossessSpeedPower),
        typeof(PossessStrengthPower),
        typeof(RampartPower),
        typeof(RavenousPower),
        typeof(ReattachPower),
        typeof(SandpitPower),
        typeof(SkittishPower),
        typeof(SlumberPower),
        typeof(SoarPower),
        typeof(SteamEruptionPower),
        typeof(StockPower),
        typeof(SuckPower),
        typeof(SurprisePower),
        typeof(SwipePower),
        typeof(ThieveryPower),
        typeof(VitalSparkPower),
        typeof(WitheringPresencePower)
    ];

    public override PowerType Type => PowerType.Buff;

    internal static int ExcludedEnemyPowerTypeCount => ExcludedEnemyPowerTypes.Count;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        int copiedAmount = (int)amount;
        if (copiedAmount <= 0 ||
            power.Owner.Side == Owner.Side ||
            power.TypeForCurrentAmount != PowerType.Buff)
        {
            return;
        }

        if (IsExcludedEnemyPower(power))
        {
            NativeSmokeTrace.N5Info($"Charismatic Form skipped excluded {power.Id}.");
            return;
        }

        PowerCopyCompatibility compatibility = PowerCopyCommand.GetCompatibility(power);
        if (!compatibility.Supported)
        {
            NativeSmokeTrace.N5Info(
                $"Charismatic Form skipped unsupported {power.Id}: {compatibility.Reason}");
            return;
        }

        Flash();
        for (int index = 0; index < Amount; index++)
        {
            PowerCopyResult result = await PowerCopyCommand.ApplyCopyAsync(
                choiceContext,
                power,
                Owner,
                PowerCopyApplierPolicy.Target,
                cardSource: cardSource,
                amountOverride: copiedAmount);
            if (!result.Success)
            {
                NativeSmokeTrace.N5Info(
                    $"Charismatic Form could not copy {power.Id}: {result.Reason}");
                break;
            }
        }
    }

    internal static bool IsExcludedEnemyPower(PowerModel power)
    {
        ArgumentNullException.ThrowIfNull(power);
        return ExcludedEnemyPowerTypes.Contains(power.GetType());
    }
}
