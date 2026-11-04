using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Commands;

internal static class ElementCurseDraw
{
    internal static bool ShouldTrigger(CardModel self, CardModel drawn) =>
        ReferenceEquals(self, drawn) && !self.Owner.Creature.HasPower<WorldviewPower>();
}
