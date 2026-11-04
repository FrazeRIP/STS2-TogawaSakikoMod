using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class SophieCard : CardModel
{
    public override string PortraitPath => NativeAssetPaths.CarefreePortrait;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 3m)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public SophieCard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Sophie requires an active combat.");
        var originals = PileType.Draw.GetPile(Owner).Cards
            .Take(DynamicVars["MagicNumber"].IntValue)
            .ToArray();
        foreach (var original in originals)
        {
            var copy = combatState.CloneCard(original);
            copy.DeckVersion = null;
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
