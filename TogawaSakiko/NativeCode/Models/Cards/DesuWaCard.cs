using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class DesuWaCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 1m)];

    public override string PortraitPath => NativeAssetPaths.DesuWaPortrait;

    public DesuWaCard()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    internal static CardModel[] SelectCandidatePile(
        IEnumerable<CardModel> drawPile,
        IEnumerable<CardModel> discardPile,
        CardType type)
    {
        ArgumentNullException.ThrowIfNull(drawPile);
        ArgumentNullException.ThrowIfNull(discardPile);
        CardModel[] drawMatches = drawPile.Where(card => card.Type == type).ToArray();
        return drawMatches.Length > 0
            ? drawMatches
            : discardPile.Where(card => card.Type == type).ToArray();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "DesuWa");
        CardType? previousType = CombatManager.Instance.History.CardPlaysFinished
            .LastOrDefault(entry => entry.CardPlay.Player == Owner)
            ?.CardPlay.Card.Type;
        if (previousType is null)
        {
            return;
        }

        Rng rng = Owner.RunState.Rng.CombatCardSelection;
        for (int index = 0; index < DynamicVars["MagicNumber"].IntValue; index++)
        {
            CardModel? selected = rng.NextItem(SelectCandidatePile(
                PileType.Draw.GetPile(Owner).Cards,
                PileType.Discard.GetPile(Owner).Cards,
                previousType.Value));
            if (selected is null)
            {
                break;
            }

            await CardPileCmd.Add(selected, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
