using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public interface IMapPathfindingContext
    {
        LevelView Level { get; }
        Location TargetLocation { get; }
    }
}