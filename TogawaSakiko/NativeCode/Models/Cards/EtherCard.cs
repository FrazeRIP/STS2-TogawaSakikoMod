using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Presentation;
using TogawaSakiko.NativeCode.Presentation.Godot;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class EtherCard : CardModel
{

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(2m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<OblivionisCard>()];

    public override string PortraitPath => NativeAssetPaths.EtherPortrait;

    public EtherCard()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int removed = 0;
        var candidates = SakikoPurgeCommand.GetCandidates(Owner).ToArray();
        if (candidates.Length > 0)
        {
            using var tips = PurgeCardLocationTips.ShowFor(candidates);
            var selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, 1) { Cancelable = true })).FirstOrDefault();
            if (selected is not null)
            {
                // Stable card identity is the name, independent of upgrades/enchantments/growth.
                var matches = candidates.Where(c => c.Id == selected.Id).ToArray();
                foreach (var card in matches)
                {
                    var result = await SakikoPurgeCommand.RemoveAsync(card);
                    if (result.Success)
                    {
                        removed++;
                    }
                    else if (!result.Prevented)
                    {
                        throw new InvalidOperationException(result.FailureReason);
                    }
                }
            }
        }

        if (removed > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(removed)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(target => SakikoDazzlingImpactVfxNode.Create(target))
                .Execute(choiceContext);
        }

        await CombatRewardCardCommand.AddAsync<OblivionisCard>(Owner, upgraded: false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
