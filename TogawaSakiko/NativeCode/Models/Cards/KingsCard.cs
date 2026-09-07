using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class KingsCard : CardModel
{
    private bool _kingsRewardPending;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(14m, ValueProp.Move)];

    public override string PortraitPath => NativeAssetPaths.KingsPortrait;

    [SavedProperty]
    public bool KingsRewardPending
    {
        get => _kingsRewardPending;
        private set
        {
            AssertMutable();
            _kingsRewardPending = value;
        }
    }

    public KingsCard()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (!Owner.Creature.HasPower<KingsPower>())
        {
            await PowerCmd.Apply<KingsPower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
    }

    public override Task BeforeCombatStart()
    {
        KingsRewardState.Clear(Owner);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.Creature.HasPower<KingsPower>())
        {
            KingsRewardState.MarkPending(Owner);
        }

        return Task.CompletedTask;
    }

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions)
    {
        return KingsRewardState.TryReduceCardRewardOptions(
            this,
            player,
            cardRewardOptions,
            creationOptions);
    }

    public override Task AfterRewardTaken(Player player, Reward reward)
    {
        if (reward is CardReward && ReferenceEquals(player, Owner))
        {
            KingsRewardState.Clear(player);
        }

        return Task.CompletedTask;
    }

    internal void SetKingsRewardPending(bool pending)
    {
        KingsRewardPending = pending;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
    }
}
