using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class ASplitMomentCard : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(1m, ValueProp.Move),
        new PowerVar<DazzlingPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DazzlingPower>()];

    public override string PortraitPath => NativeAssetPaths.SplitMomentPortrait;

    public ASplitMomentCard()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "ASplitMoment");
        await N5BatchDiagnostics.RunInCombatAsync(choiceContext, this);
        await N3ContractDiagnostics.RunInCombatAsync(choiceContext, this);
        await KingsLifecycleDiagnostics.ArmAsync(choiceContext, this);
        await N6BatchDiagnostics.RunInCombatAsync(choiceContext, this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<DazzlingPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(DazzlingPower)].BaseValue,
            Owner.Creature,
            this);
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        return N5BatchDiagnostics.ObserveAfterTakingExtraTurnAsync(player, this);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        return N5BatchDiagnostics.ObserveAfterPlayerTurnStartAsync(player, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
        DynamicVars[nameof(DazzlingPower)].UpgradeValueBy(1m);
    }
}
