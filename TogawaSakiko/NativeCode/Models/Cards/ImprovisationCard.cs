using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class ImprovisationCard : CardModel
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
        [new DamageVar(2m, ValueProp.Move), new DynamicVar("CombatsRemaining", CombatsBeforeRemoval)];

    public override string PortraitPath => NativeAssetPaths.AleaIactaEstPortrait;

    public ImprovisationCard()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(6)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // Upgrading removes the expiry without changing damage or resetting saved progress.
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
