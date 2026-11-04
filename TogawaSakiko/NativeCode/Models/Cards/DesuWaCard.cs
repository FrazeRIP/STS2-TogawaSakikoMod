using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class DesuWaCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 1m)];

    public override string PortraitPath => NativeAssetPaths.DesuWaPortrait;

    public DesuWaCard()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "DesuWa");
        await CardPileCmd.Draw(choiceContext, DynamicVars["MagicNumber"].BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
