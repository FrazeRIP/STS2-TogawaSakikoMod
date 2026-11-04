using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;

namespace TogawaSakiko.NativeCode.Diagnostics;

/// <summary>
/// Shared fixtures for the STS2 mechanics suite (Sts2MechanicsDiagnostics and the Batch*MechanicsTests files).
/// </summary>
internal static class MechanicsTestSupport
{
    // Plays a card without spending or reporting energy.
    internal static readonly ResourceInfo Zero = new()
    {
        EnergySpent = 0,
        EnergyValue = 0,
        StarsSpent = 0,
        StarValue = 0
    };

    // Selects exactly the given cards (null entries are ignored) and fails if that selection is not allowed.
    internal sealed class FixedSelector(params CardModel?[] chosen) : ICardSelector
    {
        private readonly CardModel[] _chosen = (chosen ?? []).OfType<CardModel>().ToArray();

        public Task<IEnumerable<CardModel>> GetSelectedCards(
            IEnumerable<CardModel> options,
            int minSelect,
            int maxSelect)
        {
            if (_chosen.Length < minSelect || _chosen.Length > maxSelect || _chosen.Any(card => !options.Contains(card)))
            {
                throw new InvalidOperationException("Invalid test selection");
            }

            return Task.FromResult<IEnumerable<CardModel>>(_chosen);
        }

        public CardRewardSelection GetSelectedCardReward(
            IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<CardRewardAlternative> alternatives)
        {
            return new CardRewardSelection { card = options.FirstOrDefault()?.Card };
        }
    }
}
