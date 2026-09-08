using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class QuaerereLuminaCard : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 7m)];

    public override string PortraitPath => NativeAssetPaths.QuaerereLuminaPortrait;

    public QuaerereLuminaCard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    internal static CardModel[] SelectScryWindow(IReadOnlyList<CardModel> drawPile, int amount)
    {
        ArgumentNullException.ThrowIfNull(drawPile);
        return amount <= 0 ? [] : drawPile.Take(amount).ToArray();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel[] scryWindow = SelectScryWindow(
            PileType.Draw.GetPile(Owner).Cards,
            DynamicVars["MagicNumber"].IntValue);
        if (scryWindow.Length == 0)
        {
            return;
        }

        HashSet<CardModel> candidates = new(scryWindow, ReferenceEqualityComparer.Instance);
        List<CardModel> discarded = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Draw.GetPile(Owner),
                Owner,
                new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, scryWindow.Length),
                candidates.Contains))
            .ToList();
        if (discarded.Count == 0)
        {
            return;
        }

        await CardPileCmd.Add(discarded, PileType.Discard);
        await CreatureCmd.GainBlock(
            Owner.Creature,
            discarded.Count,
            ValueProp.Move,
            cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(2m);
    }
}
