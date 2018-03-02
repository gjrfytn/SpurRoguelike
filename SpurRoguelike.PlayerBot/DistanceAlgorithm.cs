using AStarNavigator;
using AStarNavigator.Algorithms;

namespace SpurRoguelike.PlayerBot
{
    class DistanceAlgorithm : IDistanceAlgorithm
    {
        private IDistanceAlgorithmContext _Context;

        public DistanceAlgorithm(IDistanceAlgorithmContext context)
        {
            _Context = context;
        }

        public double Calculate(Tile from, Tile to)
        {
            return _Context.IsLocationHidden(new Core.Primitives.Location((int)to.X, (int)to.Y)) ? 2 : 1;
        }
    }
}
