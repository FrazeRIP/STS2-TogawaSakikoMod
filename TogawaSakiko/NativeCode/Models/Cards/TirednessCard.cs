using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class TirednessCard : CardModel
{
    public override bool CanBeGeneratedInCombat => false;

    public override bool CanBeGeneratedByModifiers => false;

    public override int MaxUpgradeLevel => 0;

    public override string PortraitPath => NativeAssetPaths.TirednessPortrait;

    public TirednessCard()
        : base(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
    {
    }

    public override Task BeforeCombatStart()
    {
        TirednessCostState.Refresh(Owner);
        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card is TirednessCard && card.Owner == Owner)
        {
            TirednessCostState.Refresh(Owner);
        }

        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        var persistent = DeckVersion;
        SakikoAudioCmd.TryPlayCardVoice(player, "Tiredness");
        // Temporary cards are not eligible for permanent removal, per the shared purge rule.
        if (persistent is not null)
        {
            await SakikoPurgeCommand.RemoveAsync(persistent);
        }

        TirednessCostState.Increase(player);
    }
}
