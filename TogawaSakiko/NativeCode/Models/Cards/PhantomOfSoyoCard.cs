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

public sealed class PhantomOfSoyoCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<KindnessCard>()];

    public override string PortraitPath => NativeAssetPaths.PhantomOfSoyoPortrait;

    public PhantomOfSoyoCard()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Phantom of Soyo requires an active combat.");
        SakikoAudioCmd.TryPlayCardVoice(Owner, "PhantomOfSoyo");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
        PersistentDeckAndCombatAddResult addition =
            await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<KindnessCard>(
                Owner,
                combatState,
                PileType.Discard,
                upgradeLevel: IsUpgraded ? 1 : 0);
        CardCmd.PreviewCardPileAdd(addition.PersistentResult);
    }
}
