using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot
{
    public interface IPathfindingContext
    {
        Location TargetLocation { get; }
    }
}