using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class CrychicCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool HasEnergyCostX => true;

    public override string PortraitPath => NativeAssetPaths.CrychicPortrait;

    public CrychicCard()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Crychic requires an active combat.");
        for (int index = 0; index < ResolveEnergyXValue(); index++)
        {
            CardModel? canonical = Owner.RunState.Rng.CombatCardGeneration.NextItem(
                CrychicPower.PhantomCanonicals);
            if (canonical is null)
            {
                break;
            }

            CardModel phantom = combatState.CreateCard(canonical, Owner);
            if (IsUpgraded)
            {
                CardCmd.Upgrade(phantom);
            }
            phantom.EnergyCost.SetUntilPlayed(0);
            await CardPileCmd.AddGeneratedCardToCombat(phantom, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
