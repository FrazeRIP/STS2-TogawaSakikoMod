using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class AccompliceCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<DesireCard>()];

    public override string PortraitPath => NativeAssetPaths.AccomplicePortrait;

    public AccompliceCard()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    internal static IEnumerable<DesireCard> SelectDesiresInHand(IEnumerable<CardModel> hand)
    {
        ArgumentNullException.ThrowIfNull(hand);
        return hand.OfType<DesireCard>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "Accomplice");
        ICombatState combatState = CombatState
            ?? throw new InvalidOperationException("Accomplice requires a live combat state.");
        for (int index = 0; index < DynamicVars["MagicNumber"].IntValue; index++)
        {
            DesireCard desire = combatState.CreateCard<DesireCard>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(desire, PileType.Hand, Owner);
        }

        foreach (DesireCard desire in SelectDesiresInHand(PileType.Hand.GetPile(Owner).Cards))
        {
            desire.SetToFreeThisTurn();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
