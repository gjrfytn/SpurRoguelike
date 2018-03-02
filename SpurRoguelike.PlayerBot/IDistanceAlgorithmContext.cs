using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot
{
    public interface IDistanceAlgorithmContext
    {
        bool IsLocationHidden(Location location);
    }
}