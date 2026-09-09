using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

/// <summary>Party card; excluded from normal rewards and generation.</summary>
public sealed class MomentMemoryCard : CardModel
{
    public override string PortraitPath => NativeAssetPaths.MomentMemoryPortrait;

    protected override string PortraitPngPath => NativeAssetPaths.MomentMemoryPortrait;

    public override bool CanBeGeneratedInCombat => false;

    public override bool CanBeGeneratedByModifiers => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Rewards", 1m)];

    public MomentMemoryCard()
        : base(5, CardType.Skill, CardRarity.Token, TargetType.AllAllies)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.RunState.CurrentRoom is not CombatRoom room ||
            !ReferenceEquals(room.CombatState, CombatState))
        {
            throw new InvalidOperationException("Party purge rewards require the active combat room.");
        }

        // OnPlay is already replicated by the native action queue. Each peer updates its own
        // room once, including rewards for remote players; the native reward UI selects its owner.
        foreach (Player player in CombatState.Players)
        {
            for (int index = 0; index < (int)DynamicVars["Rewards"].BaseValue; index++)
            {
                room.AddExtraReward(player, new CardRemovalReward(player));
            }
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
