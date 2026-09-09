using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

/// <summary>Party card; excluded from normal rewards and generation.</summary>
public sealed class NovaHistoriaCard : CardModel
{
    public override string PortraitPath => NativeAssetPaths.NovaHistoriaPortrait;

    protected override string PortraitPngPath => NativeAssetPaths.NovaHistoriaPortrait;

    public override bool CanBeGeneratedInCombat => false;

    public override bool CanBeGeneratedByModifiers => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HypePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<HypePower>(2m)];

    public NovaHistoriaCard()
        : base(1, CardType.Skill, CardRarity.Token, TargetType.AllAllies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        // Include the caster and every living player, with the original synchronized choice
        // context and applier preserved for hooks on powers belonging to other players.
        foreach (Player player in CombatState.Players.Where(player => player.Creature.IsAlive))
        {
            await PowerCmd.Apply<HypePower>(
                choiceContext, player.Creature, DynamicVars["HypePower"].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["HypePower"].UpgradeValueBy(1m);
}
