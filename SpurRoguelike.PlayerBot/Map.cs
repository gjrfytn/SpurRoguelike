using System.Collections.Generic;
using System.Linq;
using AStarNavigator;
using AStarNavigator.Algorithms;
using AStarNavigator.Providers;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot
{
    class Map : IBlockedProvider, INeighborProvider, IDistanceAlgorithm
    {
        private IMapPathfindingContext _Context;

        public Map(IMapPathfindingContext context)
        {
            _Context = context;
        }

        #region IBlockedProvider

        public bool IsBlocked(Tile coord)
        {
            Location location = ConvertToLocation(coord);

            if (location == _Context.TargetLocation)
                return false;

            if (location.X < 0 || location.Y < 0 || location.X >= _Context.Level.Field.Width || location.Y >= _Context.Level.Field.Height)
                return true;

            if (_Context.CachedWalls[location.X, location.Y])
                return true;

            CellType cellType = _Context.Level.Field[location];

            if (cellType == CellType.Wall || cellType == CellType.Trap || cellType == CellType.Exit)
                return true;

            return _Context.Level.GetHealthPackAt(location).HasValue ||
                   _Context.Level.GetItemAt(location).HasValue ||
                   _Context.Level.GetMonsterAt(location).HasValue;
        }

        #endregion

        #region INeighborProvider

        public IEnumerable<Tile> GetNeighbors(Tile tile)
        {
            return new Tile[]
            {
                new Tile(tile.X - 1, tile.Y),
                new Tile(tile.X, tile.Y - 1),
                new Tile(tile.X + 1, tile.Y),
                new Tile(tile.X, tile.Y + 1)
            };
        }

        #endregion

        #region IDistanceAlgorithm

        public double Calculate(Tile from, Tile to)
        {
            if (_Context.ApplyWeights)
                return CalculateWeight(ConvertToLocation(to));

            return IsLocationHidden(ConvertToLocation(to)) ? 10 : 1;
        }

        #endregion

        private bool IsLocationHidden(Location location)
        {
            return _Context.Level.Field[location] == CellType.Hidden;
        }

        private float CalculateWeight(Location location)
        {
            float result = 1;

            result /= 0.2f * GetTrapCountInRange(location, 1) + 1;

            result /= 0.5f * GetHealthPackCountInRange(location, 2) + 1;

            result *= GetMonsterCountInRange(location, 1) + 1;

            return result;
        }

        private Location ConvertToLocation(Tile tile)
        {
            return new Location((int)tile.X, (int)tile.Y);
        }

        private int GetTrapCountInRange(Location location, int range)
        {
            return _Context.Level.Field.GetCellsOfType(CellType.Trap).Count(t => location.IsInRange(t, range));
        }

        private int GetHealthPackCountInRange(Location location, int range)
        {
            return _Context.Level.HealthPacks.Count(p => location.IsInRange(p.Location, range));
        }

        private int GetMonsterCountInRange(Location location, int range)
        {
            return _Context.Level.Monsters.Count(p => location.IsInRange(p.Location, range));
        }
    }
}
