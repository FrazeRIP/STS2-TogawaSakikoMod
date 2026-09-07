using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class PrideCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Innate, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MagicNumber", 1m)];

    public override string PortraitPath => NativeAssetPaths.PridePortrait;

    public PrideCard()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    internal static CardModel[] SelectCandidatePile(
        IEnumerable<CardModel> drawPile,
        IEnumerable<CardModel> discardPile)
    {
        CardModel[] drawAttacks = drawPile.Where(card => card.Type == CardType.Attack).ToArray();
        return drawAttacks.Length > 0
            ? drawAttacks
            : discardPile.Where(card => card.Type == CardType.Attack).ToArray();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "Pride");
        Rng rng = Owner.RunState.Rng.CombatCardSelection;
        for (int index = 0; index < DynamicVars["MagicNumber"].IntValue; index++)
        {
            CardModel? attack = rng.NextItem(SelectCandidatePile(
                PileType.Draw.GetPile(Owner).Cards,
                PileType.Discard.GetPile(Owner).Cards));
            if (attack is null)
            {
                break;
            }
            await CardPileCmd.Add(attack, PileType.Hand);
        }

        PridePower? power = await PowerCmd.Apply<PridePower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
        power?.Register(this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
