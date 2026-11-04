using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class SoraNoMusicaCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MagicNumber", 4m)
    ];

    public override string PortraitPath => NativeAssetPaths.SoraNoMusicaPortrait;

    public SoraNoMusicaCard()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EnergyNextTurnPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
        CardPile drawPile = PileType.Draw.GetPile(Owner);
        int maximum = Math.Min(DynamicVars["MagicNumber"].IntValue, drawPile.Cards.Count);
        if (maximum <= 0)
        {
            return;
        }

        CardModel[] selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                drawPile,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, maximum)
                {
                    Cancelable = true,
                    RequireManualConfirmation = true
                }))
            .ToArray();
        foreach (CardModel card in selected.Reverse())
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(2m);
    }
}
