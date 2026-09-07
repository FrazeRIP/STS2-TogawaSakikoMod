using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class SeizeTheFateCard : CardModel
{
    internal const int InitialHype = 6;

    private int _timesPlayed;

    [SavedProperty]
    public int TimesPlayed
    {
        get => _timesPlayed;
        set
        {
            AssertMutable();
            _timesPlayed = Math.Clamp(value, 0, InitialHype);
            RefreshHypeAmount();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MagicNumber", InitialHype)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<HypePower>()];

    public override string PortraitPath => NativeAssetPaths.SeizeTheFatePortrait;

    public SeizeTheFateCard()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int hype = DynamicVars["MagicNumber"].IntValue;
        if (hype > 0)
        {
            await PowerCmd.Apply<HypePower>(
                choiceContext,
                Owner.Creature,
                hype,
                Owner.Creature,
                this);
        }

        if (!IsUpgraded && TimesPlayed < InitialHype)
        {
            SynchronizeTimesPlayed(TimesPlayed + 1);
        }
    }

    protected override void OnUpgrade()
    {
        RefreshHypeAmount();
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        RefreshHypeAmount();
    }

    internal void SynchronizeTimesPlayed(int timesPlayed)
    {
        SeizeTheFateCard? persistentCard = Pile?.Type == PileType.Deck
            ? this
            : DeckVersion as SeizeTheFateCard;
        if (persistentCard is null)
        {
            TimesPlayed = timesPlayed;
            return;
        }

        persistentCard.TimesPlayed = timesPlayed;
        foreach (SeizeTheFateCard linkedCopy in PersistentDeckMutation
                     .SnapshotLinkedCombatCopies(persistentCard)
                     .OfType<SeizeTheFateCard>())
        {
            linkedCopy.TimesPlayed = timesPlayed;
        }
    }

    private void RefreshHypeAmount()
    {
        DynamicVars["MagicNumber"].BaseValue = Math.Max(0, InitialHype - _timesPlayed);
    }
}
