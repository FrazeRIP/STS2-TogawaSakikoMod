using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class GoldenPocketWatch : SakikoRelicModel
{
    internal const int TriggerAmount = 12;
    internal const int StrengthPerTrigger = 2;

    private int _cardsPlayed;

    protected override string AssetStem => "goldenpocketwatch";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => true;

    public override int DisplayAmount => CardsPlayed;

    [SavedProperty]
    public int CardsPlayed
    {
        get => _cardsPlayed;
        set
        {
            AssertMutable();
            _cardsPlayed = Math.Max(0, value);
            Status = _cardsPlayed == TriggerAmount - 1 ? RelicStatus.Active : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!ReferenceEquals(cardPlay.Card.Owner, Owner) || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        CardsPlayed++;
        while (CardsPlayed >= TriggerAmount)
        {
            CardsPlayed -= TriggerAmount;
            Flash();
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                Owner.Creature,
                StrengthPerTrigger,
                Owner.Creature,
                null);
        }
    }
}
