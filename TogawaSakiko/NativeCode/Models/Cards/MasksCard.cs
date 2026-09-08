using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class MasksCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MagicNumber", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DazzlingPower>(), HoverTipFactory.FromPower<StrengthPower>()];

    public override string PortraitPath => NativeAssetPaths.MasksPortrait;

    public MasksCard()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "Masks");
        DazzlingPower? dazzling = Owner.Creature.GetPower<DazzlingPower>();
        if (dazzling is not null)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                dazzling,
                -DynamicVars["MagicNumber"].BaseValue,
                Owner.Creature,
                this);
        }
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["MagicNumber"].BaseValue,
            Owner.Creature,
            this);

        CardModel? selected = (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                filter: null,
                source: this))
            .FirstOrDefault();
        if (selected is null)
        {
            return;
        }

        var combatState = CombatState
            ?? throw new InvalidOperationException("Masks requires an active combat.");
        await CardPileCmd.RemoveFromCombat(selected);
        CardModel replacement = CardModel.FromSerializable(ToSerializable());
        combatState.AddCard(replacement, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(replacement, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
