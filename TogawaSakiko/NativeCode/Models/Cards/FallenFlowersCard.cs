using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class FallenFlowersCard : CardModel
{
    private const int CombatsBeforeRemoval = 3;

    private int _combatsSeen;

    [SavedProperty]
    public int CombatsSeen
    {
        get => _combatsSeen;
        set
        {
            AssertMutable();
            _combatsSeen = value;
            DynamicVars["CombatsRemaining"].BaseValue = CombatsBeforeRemoval - value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MagicNumber", 6m), new DynamicVar("CombatsRemaining", CombatsBeforeRemoval)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DazzlingPower>()];

    public override string PortraitPath => NativeAssetPaths.FallenFlowersPortrait;

    public FallenFlowersCard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "FallenFlowers");
        await PowerCmd.Apply<DazzlingPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["MagicNumber"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        // Upgrading removes the expiry without changing Dazzling or resetting saved progress.
    }

    // Same deck-only, end-of-combat lifecycle as the native Guilty card.
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (IsUpgraded || Pile?.Type != PileType.Deck)
        {
            return;
        }

        CombatsSeen++;
        if (CombatsSeen >= CombatsBeforeRemoval)
        {
            await CardPileCmd.RemoveFromDeck(this, true);
        }
    }
}
