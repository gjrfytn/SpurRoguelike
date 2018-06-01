using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;
using SpurRoguelike.PlayerBot.Extensions;

namespace SpurRoguelike.PlayerBot
{
    class BotNavigator : IMapNavigationContext
    {
        private readonly Map _Map;
        private readonly AStarNavigator.ITileNavigator _Navigator;

        private Location _PlayerLocation;
        private int _VisibilityWidth;
        private int _VisibilityHeight;

        private bool _DiscardCache = true;
        private List<AStarNavigator.Tile> _CachedPath = new List<AStarNavigator.Tile>();
        private int _CachedPathPointIndex;
        private Location _CachedPathLastCacheLocation;

        private List<Location> _ClearPathAttempts = new List<Location>();

        public BotNavigator()
        {
            _Map = new Map(this);
            _Navigator = new AStarNavigator.TileNavigator(_Map, _Map, _Map, new AStarNavigator.Algorithms.ManhattanHeuristicAlgorithm());
        }

        #region IPathfindingContext

        public Location TargetLocation { get; set; }

        public bool ApplyWeights { get; set; }

        #endregion

        public void InitializeLevel(ILevelView level)
        {
            _VisibilityWidth = level.Field.VisibilityWidth;
            _VisibilityHeight = level.Field.VisibilityHeight;

            _Map.InitializeLevel(level);
        }

        public void InitializeTurn(Location? exit, Location playerLocation, bool applyWeights)
        {
            _PlayerLocation = playerLocation;
            ApplyWeights = applyWeights;

            _ClearPathAttempts.Clear();

            _Map.InitializeTurn(exit);
        }

        public Turn GoTo(Location location)
        {
            AStarNavigator.Tile tile;
            if (_Map.LocationIsVisible(location) || ApplyWeights)
            {
                _DiscardCache = true;

                IEnumerable<AStarNavigator.Tile> path = Navigate(location);

                if (path == null)
                    return TryClearPath(location);

                tile = path.First();
            }
            else
            {
                Offset offset = _PlayerLocation - _CachedPathLastCacheLocation;
                if (_DiscardCache || System.Math.Abs(offset.XOffset) == _VisibilityWidth - 1 || System.Math.Abs(offset.YOffset) == _VisibilityHeight - 1)
                {
                    _CachedPathPointIndex = 0;

                    _CachedPath = Navigate(location)?.ToList();

                    if (_CachedPath == null)
                    {
                        _DiscardCache = true;

                        return TryClearPath(location);
                    }

                    _DiscardCache = false;
                    _CachedPathLastCacheLocation = _PlayerLocation;
                }

                tile = _CachedPath[_CachedPathPointIndex];
                
                _CachedPathPointIndex++;
            }

            return GetStepTurn(tile);
        }

        public Turn GoToClosest(IEnumerable<Location> locations)
        {
            AStarNavigator.Tile tile;

            _DiscardCache = true;

            List<IEnumerable<AStarNavigator.Tile>> paths = new List<IEnumerable<AStarNavigator.Tile>>();
            foreach (Location location in locations)
            {
                IEnumerable<AStarNavigator.Tile> path = Navigate(location);

                if (path != null)
                    paths.Add(path);
            }

            if (!paths.Any())
                return null;

            tile = paths.OrderBy(p => p.Count()).First().First();

            return GetStepTurn(tile);
        }

        private IEnumerable<AStarNavigator.Tile> Navigate(Location location)
        {
            TargetLocation = location;

            return _Navigator.Navigate(
                new AStarNavigator.Tile(_PlayerLocation.X, _PlayerLocation.Y),
                new AStarNavigator.Tile(location.X, location.Y)
                );
        }

        private Turn GetStepTurn(AStarNavigator.Tile tile)
        {
            return Turn.Step(new Offset((int)tile.X - _PlayerLocation.X, (int)tile.Y - _PlayerLocation.Y));
        }

        private Turn TryClearPath(Location location)
        {
            if (_ClearPathAttempts.Contains(location))
                return null;

            _ClearPathAttempts.Add(location);

            Turn turn = null;
            IEnumerable<Location> packs = _Map.HealthPacks.Where(p => location.IsInStepRange(p));
            foreach (Location pack in packs)
            {
                turn = GoTo(pack);

                if (turn != null)
                    return turn;
            }

            IEnumerable<Location> items = _Map.Items.Where(i => location.IsInStepRange(i));
            foreach (Location item in items)
            {
                turn = GoTo(item);

                if (turn != null)
                    return turn;
            }

            IEnumerable<Location> monsters = _Map.Monsters.Where(m => location.IsInStepRange(m));
            foreach (Location monster in monsters)
            {
                turn = GoTo(monster);

                if (turn != null)
                    return turn;
            }

            return null;
        }
    }
}
