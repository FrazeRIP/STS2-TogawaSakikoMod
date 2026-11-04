using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class MasqueradeRhapsodyRequestCard : CardModel
{
    private int _permanentDamageIncrease;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new DynamicVar("MagicNumber", 1m)
    ];

    public override string PortraitPath => NativeAssetPaths.MasqueradeRhapsodyRequestPortrait;

    [SavedProperty]
    public int PermanentDamageIncrease
    {
        get => _permanentDamageIncrease;
        private set
        {
            AssertMutable();
            _permanentDamageIncrease = value;
            RefreshDamage();
        }
    }

    public MasqueradeRhapsodyRequestCard()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    internal static int ApplyPurgeGrowth(Player owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (owner.PlayerCombatState is null)
        {
            return 0;
        }

        var permanentCards = owner.Deck.Cards.OfType<MasqueradeRhapsodyRequestCard>().ToArray();
        foreach (var permanent in permanentCards)
        {
            permanent.IncreaseFromPurge(permanent.DynamicVars["MagicNumber"].IntValue);
            foreach (var copy in PersistentDeckMutation.SnapshotLinkedCombatCopies(permanent)
                         .OfType<MasqueradeRhapsodyRequestCard>())
            {
                copy.PermanentDamageIncrease = permanent.PermanentDamageIncrease;
            }
        }

        // Temporary copies can grow during combat, but never mutate an unrelated permanent card.
        foreach (var temporary in owner.PlayerCombatState.AllCards
                     .OfType<MasqueradeRhapsodyRequestCard>()
                     .Where(card => card.DeckVersion is null && card.Pile is not null))
        {
            temporary.IncreaseFromPurge(temporary.DynamicVars["MagicNumber"].IntValue);
        }

        return permanentCards.Length;
    }

    internal void IncreaseFromPurge(int amount)
    {
        if (amount > 0)
        {
            PermanentDamageIncrease += amount;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        RefreshDamage();
    }

    private void RefreshDamage()
    {
        DynamicVars.Damage.BaseValue = 1m + PermanentDamageIncrease;
    }
}
