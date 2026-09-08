using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
            PileType.Hand.GetPile(Owner).Cards
                .Concat(PileType.Discard.GetPile(Owner).Cards)
                .Concat(PileType.Draw.GetPile(Owner).Cards),
            CandidateCount,
            Owner.RunState.Rng.CombatCardSelection);
        if (candidates.Length > 0)
        {
            CardModel? chosen = await CardSelectCmd.FromChooseACardScreen(
                choiceContext,
                candidates,
                Owner);
            if (chosen is not null)
            {
                await PurgeChosenCardAsync(choiceContext, chosen);
            }
        }

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

    private static async Task PurgeChosenCardAsync(PlayerChoiceContext choiceContext, CardModel chosen)
    {
        SakikoPurgeResult result = await SakikoPurgeCommand.RemoveAsync(
            chosen,
            choiceContext: choiceContext);
        if (result.Success || result.Prevented)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Daten could not purge {chosen.Id}: {result.FailureReason}.");
    }
}
