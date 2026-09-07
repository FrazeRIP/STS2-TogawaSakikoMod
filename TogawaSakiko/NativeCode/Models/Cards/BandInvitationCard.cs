using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class BandInvitationCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MagicNumber", 4m)];

    protected override bool IsPlayable =>
        Owner is null || PileType.Hand.GetPile(Owner).Cards.Count(card => card.Type == CardType.Skill) <= 1;

    protected override bool ShouldGlowGoldInternal => IsPlayable;

    public override string PortraitPath => NativeAssetPaths.BandInvitationPortrait;

    public BandInvitationCard()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    internal static int CountPositiveEnergy(IEnumerable<CardModel> cards)
    {
        return cards.Sum(card => Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All)));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "BandInvitation");
        int accumulatedCost = 0;
        while (accumulatedCost < DynamicVars["MagicNumber"].IntValue)
        {
            CardModel? drawn = (await CardPileCmd.Draw(choiceContext, 1m, Owner)).FirstOrDefault();
            if (drawn is null)
            {
                break;
            }

            accumulatedCost += Math.Max(0, drawn.EnergyCost.GetWithModifiers(CostModifiers.All));
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
