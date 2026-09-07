using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class HypePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldClearBlock(Creature creature)
    {
        return !ReferenceEquals(creature, Owner) || creature.Block <= 0 || Amount <= 0;
    }

    public override async Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
    {
        if (!ReferenceEquals(preventer, this) || !ReferenceEquals(creature, Owner))
        {
            return;
        }

        await ConsumeOneAsync();
    }

    public async Task ConsumeOneAsync()
    {
        if (Amount <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Decrement(this);
    }
}
