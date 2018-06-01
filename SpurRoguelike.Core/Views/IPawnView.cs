using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.Core.Views
{
    public interface IPawnView:IView
    {
        string Name { get; }

        int Attack  { get; }

        int Defence  { get; }

        int TotalAttack  { get; }

        int TotalDefence  { get; }

        int Health { get; }

        bool TryGetEquippedItem(out IItemView item);

        bool IsDestroyed { get; }

        Location Location { get; }
    }
}