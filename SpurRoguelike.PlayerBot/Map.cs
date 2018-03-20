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
        private float?[,] _CachedWallsWeights;
        private List<Location> _CachedTraps;
        private List<Location> _CachedHealthPacks;
        private List<Location> _CachedMonsters;
        private List<Location> _CachedWalls;

        public Map(IMapPathfindingContext context)
        {
            _Context = context;
        }

        public void InitializeTurnCache()
        {
            _CachedTraps = _Context.Level.Field.GetCellsOfType(CellType.Trap).ToList();
            _CachedHealthPacks = _Context.Level.HealthPacks.Select(p => p.Location).ToList();
            _CachedMonsters = _Context.Level.Monsters.Select(m => m.Location).ToList();
            _CachedWalls = _Context.Level.Field.GetCellsOfType(CellType.Wall).ToList();
        }

        public void InitializeLevelCache()
        {
            _CachedWallsWeights = new float?[_Context.Level.Field.Width, _Context.Level.Field.Height];
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

            return IsLocationHidden(ConvertToLocation(to)) ? 10 : 1; //TODO Считать при весах тоже?
        }

        #endregion

        private bool IsLocationHidden(Location location)
        {
            return _Context.Level.Field[location] == CellType.Hidden;
        }

        private float CalculateWeight(Location location)
        {
            float result = 1;

            result /= 0.25f * GetTrapCountInRange(location, 1) + 1; //С 0.25 последний

            int hpsInOneRange = GetHealthPackCountInRange(location, 1);
            int hpsInTwoRange = GetHealthPackCountInRange(location, 2);
            result /= 0.5f * (hpsInTwoRange - hpsInOneRange) + 1;
            result /= hpsInOneRange + 1;

            if (_CachedWallsWeights[location.X, location.Y].HasValue)
            {
                result *= _CachedWallsWeights[location.X, location.Y].Value;
            }
            else
            {
                int wallsInOneRange = GetWallsCountInRange(location, 1);
                int wallsInTwoRange = GetWallsCountInRange(location, 2);

                float wallsCoef = (0.25f * (wallsInTwoRange - wallsInOneRange) + 1) * (0.5f * wallsInOneRange + 1);

                if (AllCellsInRangeAreVisible(location, 2))
                    _CachedWallsWeights[location.X, location.Y] = wallsCoef;

                result *= wallsCoef;
            }


            int monstersInOneRange = GetMonsterCountInRange(location, 1);
            int monstersInTwoRange = GetMonsterCountInRange(location, 2);
            result *= 0.5f * (monstersInTwoRange - monstersInOneRange) + 1; //1.5?
            result *= monstersInOneRange + 1; //1.5?

            return result;
        }

        private Location ConvertToLocation(Tile tile)
        {
            return new Location((int)tile.X, (int)tile.Y);
        }

        private int GetTrapCountInRange(Location location, int range)
        {
            return _CachedTraps.Count(t => location.IsInRange(t, range));
        }

        private int GetHealthPackCountInRange(Location location, int range)
        {
            return _CachedHealthPacks.Count(p => location.IsInRange(p, range));
        }

        private int GetMonsterCountInRange(Location location, int range)
        {
            return _CachedMonsters.Count(m => location.IsInRange(m, range));
        }

        private int GetWallsCountInRange(Location location, int range)
        {
            return _CachedWalls.Count(w => location.IsInRange(w, range));
        }

        private bool AllCellsInRangeAreVisible(Location location, int range)
        {
            return !_Context.Level.Field.GetCellsOfType(CellType.Hidden).Any(c => location.IsInRange(c, range));
        }
    }
}
