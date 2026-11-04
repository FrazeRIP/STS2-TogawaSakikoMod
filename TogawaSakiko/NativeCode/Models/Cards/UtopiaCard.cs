using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class UtopiaCard : CardModel
{
    internal const int InitialBlock = 15;

    internal const int BlockLossPerPlay = 3;

    private int _permanentBlockLoss;

    public override bool GainsBlock => true;

    [SavedProperty]
    public int PermanentBlockLoss
    {
        get => _permanentBlockLoss;
        set
        {
            AssertMutable();
            _permanentBlockLoss = Math.Clamp(value, 0, InitialBlock);
            RefreshBlock();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(InitialBlock, ValueProp.Move)];

    public override string PortraitPath => NativeAssetPaths.UtopiaPortrait;

    public UtopiaCard()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (!IsUpgraded)
        {
            var persistent = DeckVersion as UtopiaCard;
            int next = Math.Min(
                InitialBlock,
                (persistent?.PermanentBlockLoss ?? PermanentBlockLoss) + BlockLossPerPlay);
            PermanentBlockLoss = next;
            if (persistent is not null && !persistent.HasBeenRemovedFromState)
            {
                persistent.PermanentBlockLoss = next;
            }
        }
    }

    protected override void OnUpgrade()
    {
        RefreshBlock();
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        RefreshBlock();
    }

    private void RefreshBlock()
    {
        DynamicVars.Block.BaseValue = InitialBlock - _permanentBlockLoss;
    }
}
