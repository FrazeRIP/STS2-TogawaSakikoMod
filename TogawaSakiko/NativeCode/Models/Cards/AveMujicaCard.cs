using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class AveMujicaCard : CardModel
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override string PortraitPath => NativeAssetPaths.AveMujicaPortrait;

    public AveMujicaCard()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    internal static CardModel[] SelectOptionCanonicals()
    {
        CardModel[] symbols =
        [
            ModelDb.Card<SymbolIFireCard>(),
            ModelDb.Card<SymbolIIAirCard>(),
            ModelDb.Card<SymbolIIIWaterCard>(),
            ModelDb.Card<SymbolIVEarthCard>()
        ];
        return symbols;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Ave Mujica requires an active combat.");
        bool upgradeOptions = IsUpgraded || Owner.GetRelic<MoltenEgg>() is not null;
        List<CardModel> options = SelectOptionCanonicals()
            .Select(canonical => combatState.CreateCard(canonical, Owner))
            .ToList();
        if (upgradeOptions)
        {
            foreach (CardModel option in options)
            {
                CardCmd.Upgrade(option);
            }
        }

        SakikoAudioCmd.TryPlayCardVoice(Owner, "AveMujica");
        CardModel? selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            options,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (selected is null)
        {
            foreach (CardModel option in options)
            {
                combatState.RemoveCard(option);
            }

            return;
        }

        CardPileAddResult persistent = await PersistentDeckMutation.AddStatEquivalentAsync(
            Owner,
            selected);
        CardCmd.PreviewCardPileAdd(persistent);
        // Only the linked hand copy is playable; discard the unchosen preview instances.
        foreach (CardModel option in options)
        {
            combatState.RemoveCard(option);
        }
    }

    protected override void OnUpgrade()
    {
        // The selected element is upgraded when this card is upgraded.
    }
}
