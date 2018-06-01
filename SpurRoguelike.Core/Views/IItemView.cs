using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.Core.Views
{
    public interface IItemView : IView
    {
        int AttackBonus { get; }

        int DefenceBonus { get; }

        Location Location { get; }
    }
}