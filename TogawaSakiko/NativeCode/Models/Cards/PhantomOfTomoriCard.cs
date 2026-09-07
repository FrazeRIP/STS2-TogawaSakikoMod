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

public sealed class PhantomOfTomoriCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(14m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<VoiceCard>()];

    public override string PortraitPath => NativeAssetPaths.PhantomOfTomoriPortrait;

    public PhantomOfTomoriCard()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Phantom of Tomori requires an active combat.");
        SakikoAudioCmd.TryPlayCardVoice(Owner, "PhantomOfTomori");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(combatState)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
        PersistentDeckAndCombatAddResult addition =
            await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<VoiceCard>(
                Owner,
                combatState,
                PileType.Discard,
                upgradeLevel: IsUpgraded ? 1 : 0);
        CardCmd.PreviewCardPileAdd(addition.PersistentResult);
    }
}
