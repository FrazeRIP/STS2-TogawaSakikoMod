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

public sealed class PhantomOfMutsumiCard : CardModel
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new BlockVar(6m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<ProtectionCard>()];

    public override string PortraitPath => NativeAssetPaths.PhantomOfMutsumiPortrait;

    public PhantomOfMutsumiCard()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var combatState = CombatState
            ?? throw new InvalidOperationException("Phantom of Mutsumi requires an active combat.");
        SakikoAudioCmd.TryPlayCardVoice(Owner, "PhantomOfMutsumi");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        PersistentDeckAndCombatAddResult addition =
            await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<ProtectionCard>(
                Owner,
                combatState,
                PileType.Discard,
                upgradeLevel: IsUpgraded ? 1 : 0);
        CardCmd.PreviewCardPileAdd(addition.PersistentResult);
    }
}
