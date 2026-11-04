using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class DatenCard : CardModel
{
    public const int CandidateCount = 3;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(13m, ValueProp.Move)];

    public override string PortraitPath => NativeAssetPaths.DatenPortrait;

    public DatenCard()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    internal static CardModel[] SelectPurgeCandidates(
        IEnumerable<CardModel> candidates,
        int count,
        Rng rng)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(rng);
        if (count <= 0)
        {
            return [];
        }

        return candidates.TakeRandom(count, rng).ToArray();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        CardModel[] candidates = SelectPurgeCandidates(
            SakikoPurgeCommand.GetCandidates(Owner),
            CandidateCount,
            Owner.RunState.Rng.CombatCardSelection);
        await WishFulfilledCard.ChooseFromCandidatesAsync(
            choiceContext,
            Owner,
            new LocString("cards", "TOGAWASAKIKO-WISH_FULFILLED_CARD.selectionScreenPrompt"),
            candidates);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
