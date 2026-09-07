using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class PhantomOfTakiCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<IdealCard>()];

    public override string PortraitPath => NativeAssetPaths.PhantomOfTakiPortrait;

    public PhantomOfTakiCard()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var combatState = CombatState
            ?? throw new InvalidOperationException("Phantom of Taki requires an active combat.");
        SakikoAudioCmd.TryPlayCardVoice(Owner, "PhantomOfTaki");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
        PersistentDeckAndCombatAddResult addition =
            await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<IdealCard>(
                Owner,
                combatState,
                PileType.Discard,
                upgradeLevel: IsUpgraded ? 1 : 0);
        CardCmd.PreviewCardPileAdd(addition.PersistentResult);
    }
}
