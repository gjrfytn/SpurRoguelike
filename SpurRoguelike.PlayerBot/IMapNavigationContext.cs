using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot
{
    public interface IMapNavigationContext
    {
        Location TargetLocation { get; }
        bool ApplyWeights { get; }
    }
}