using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;

namespace TogawaSakiko.NativeCode.Presentation;

internal static class PurgeCardLocationTips
{
    private static readonly Dictionary<CardModel, int> VisibleCards = new();

    internal static IDisposable ShowFor(IEnumerable<CardModel> candidates) => new Scope(candidates);

    internal static bool IsVisibleFor(CardModel card) => VisibleCards.ContainsKey(card);

    internal static PileType[] GetLocations(CardModel card) =>
        PersistentDeckMutation.SnapshotLinkedCombatCopies(card)
            .Select(copy => copy.Pile!.Type)
            .OrderBy(pile => (int)pile)
            .ToArray();

    internal static IEnumerable<IHoverTip> Append(CardModel card, IEnumerable<IHoverTip> current)
    {
        if (!IsVisibleFor(card))
        {
            return current;
        }

        PileType[] locations = GetLocations(card);
        string text = locations.Length == 0
            ? Text("NONE")
            : string.Join("\n", locations.GroupBy(pile => pile).Select(FormatLocation));
        return current.Append(new HoverTip(new LocString("card_keywords", "TOGAWASAKIKO-PURGE_LOCATION.title"), text));
    }

    private static string FormatLocation(IGrouping<PileType, PileType> group) =>
        Text(group.Key.ToString().ToUpperInvariant()) + (group.Count() > 1 ? $" ×{group.Count()}" : "");

    private static string Text(string key) =>
        new LocString("card_keywords", "TOGAWASAKIKO-PURGE_LOCATION." + key).GetFormattedText();

    private sealed class Scope : IDisposable
    {
        private readonly CardModel[] _cards;
        private bool _disposed;

        internal Scope(IEnumerable<CardModel> cards)
        {
            _cards = cards.Distinct().ToArray();
            foreach (CardModel card in _cards)
            {
                VisibleCards[card] = VisibleCards.GetValueOrDefault(card) + 1;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (CardModel card in _cards)
            {
                if (--VisibleCards[card] == 0)
                {
                    VisibleCards.Remove(card);
                }
            }
        }
    }
}
