using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class MementoMoriCard : CardModel
{
    public const int DrawPileWindowSize = 7;

    internal static bool SkipPurgeVisuals => SaveManager.Instance.PrefsSave.FastMode != FastModeType.Normal;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HoverTipFactory.FromPower<MonsterDivinityPower>()];

    public override string PortraitPath => NativeAssetPaths.MementoMoriPortrait;

    public MementoMoriCard()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    internal static CardModel[] SelectTopRemovableCards(IReadOnlyList<CardModel> drawPile, int windowSize)
    {
        ArgumentNullException.ThrowIfNull(drawPile);
        if (windowSize <= 0)
        {
            return [];
        }

        return drawPile.Take(windowSize).Where(card => card.IsRemovable).ToArray();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        CardModel[] selectedCards = SelectTopRemovableCards(
            PileType.Draw.GetPile(Owner).Cards,
            DrawPileWindowSize);

        if (IsUpgraded)
        {
            await PurgeSelectedCardsAsync(selectedCards);
        }
        else
        {
            foreach (CardModel card in selectedCards)
            {
                await CardCmd.Exhaust(choiceContext, card, skipVisuals: SkipPurgeVisuals);
            }
        }

        await SakikoStanceCmd.EnterDivinityAsync(
            choiceContext,
            Owner.Creature,
            Owner.Creature,
            this);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
    }

    private static async Task PurgeSelectedCardsAsync(IReadOnlyList<CardModel> selectedCards)
    {
        HashSet<CardModel> processedPersistentCards = new(ReferenceEqualityComparer.Instance);
        foreach (CardModel combatCard in selectedCards)
        {
            if (combatCard.HasBeenRemovedFromState || combatCard.Pile is null)
            {
                continue;
            }

            CardModel? persistentCard = combatCard.DeckVersion;
            if (persistentCard is null)
            {
                await CardPileCmd.RemoveFromCombat(combatCard, skipVisuals: SkipPurgeVisuals);
                continue;
            }

            if (!processedPersistentCards.Add(persistentCard))
            {
                continue;
            }

            PersistentDeckRemovalResult result = await PersistentDeckMutation.RemoveAsync(
                persistentCard,
                showPersistentPreview: !SkipPurgeVisuals,
                skipCombatVisuals: SkipPurgeVisuals);
            if (result.Success || result.Prevented)
            {
                continue;
            }

            throw new InvalidOperationException(
                $"Memento Mori could not purge {persistentCard.Id}: {result.Status} ({result.FailureReason}).");
        }
    }
}
