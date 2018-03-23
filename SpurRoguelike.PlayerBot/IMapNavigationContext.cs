using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public interface IMapNavigationContext
    {
        Location TargetLocation { get; }
        bool ApplyWeights { get; }
    }
}