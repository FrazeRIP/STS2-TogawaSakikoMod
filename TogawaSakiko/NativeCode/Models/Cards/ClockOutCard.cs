using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class ClockOutCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 15m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<TirednessCard>()];

    public override string PortraitPath => NativeAssetPaths.ClockOutPortrait;

    public ClockOutCard()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Clock Out requires an active combat.");
        SakikoAudioCmd.TryPlayCardVoice(Owner, "ClockOut");
        await PlayerCmd.GainGold(DynamicVars["MagicNumber"].BaseValue, Owner);
        PersistentDeckAndCombatAddResult addition =
            await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<TirednessCard>(
                Owner,
                combatState,
                PileType.Discard);
        CardCmd.PreviewCardPileAdd(addition.PersistentResult);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(5m);
    }
}
