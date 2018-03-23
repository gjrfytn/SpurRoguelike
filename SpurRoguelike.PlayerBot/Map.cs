using System.Collections.Generic;
using System.Linq;
using AStarNavigator;
using AStarNavigator.Algorithms;
using AStarNavigator.Providers;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;
using SpurRoguelike.PlayerBot.Extensions;

namespace SpurRoguelike.PlayerBot
{
    class Map : IBlockedProvider, INeighborProvider, IDistanceAlgorithm
    {
        private const int _DefaultCellWeight = 1;
        private const int _HiddenCellsWeight = 10;

        private IMapPathfindingContext _Context;
        private Location _Exit;
        private float?[,] _CachedWallsWeights;
        private List<Location> _CachedTraps;
        private List<Location> _CachedHealthPacks;
        private List<Location> _CachedMonsters;
        private List<Location> _CachedWalls_Old; //TODO

        private bool[,] _CachedWalls;
        private bool[,] _CacheLocations;

        public Map(IMapPathfindingContext context)
        {
            _Context = context;
        }

        public void InitializeTurn()
        {
            _CachedTraps = _Context.Level.Field.GetCellsOfType(CellType.Trap).ToList();
            _CachedHealthPacks = _Context.Level.HealthPacks.Select(p => p.Location).ToList();
            _CachedMonsters = _Context.Level.Monsters.Select(m => m.Location).ToList();
            _CachedWalls_Old = _Context.Level.Field.GetCellsOfType(CellType.Wall).ToList();

            if (!_CacheLocations[_Context.Level.Player.Location.X, _Context.Level.Player.Location.Y])
                CacheWalls();
        }

        public void InitializeLevel(Location exit)
        {
            _Exit = exit;

            _CachedWallsWeights = new float?[_Context.Level.Field.Width, _Context.Level.Field.Height];

            _CacheLocations = new bool[_Context.Level.Field.Width, _Context.Level.Field.Height];
            _CachedWalls = new bool[_Context.Level.Field.Width, _Context.Level.Field.Height];
            _CachedWalls[_Exit.X, _Exit.Y] = true;
            CacheWalls();
        }

        #region IBlockedProvider

        public bool IsBlocked(Tile coord)
        {
            Location location = ConvertToLocation(coord);

            if (location == _Context.TargetLocation)
                return false;

            if (location.X < 0 || location.Y < 0 || location.X >= _Context.Level.Field.Width || location.Y >= _Context.Level.Field.Height)
                return true;

            if (_CachedWalls[location.X, location.Y])
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

            return IsLocationHidden(ConvertToLocation(to)) ? _HiddenCellsWeight : _DefaultCellWeight;
        }

        #endregion

        private void CacheWalls()
        {
            FieldView field = _Context.Level.Field;
            for (int x = 0; x < field.Width; ++x)
                for (int y = 0; y < field.Height; ++y)
                {
                    Location location = new Location(x, y);
                    if (field[location] == CellType.Wall && !location.IsInStepRange(_Exit)) // TODO _Exit - для уровня с боссом
                        _CachedWalls[x, y] = true;
                }

            _CacheLocations[_Context.Level.Player.Location.X, _Context.Level.Player.Location.Y] = true;
        }

        private bool IsLocationHidden(Location location)
        {
            return _Context.Level.Field[location] == CellType.Hidden;
        }

        private float CalculateWeight(Location location)
        {
            float result = _DefaultCellWeight;

            ApplyTrapsWeight(location, ref result);
            ApplyHealthPacksWeight(location, ref result);
            ApplyWallsWeight(location, ref result);
            ApplyMonstersWeight(location, ref result);

            return result;
        }

        private void ApplyTrapsWeight(Location location, ref float weight)
        {
            weight /= 0.25f * GetTrapCountInRange(location, 1) + 1; //С 0.25 последний
        }

        private void ApplyHealthPacksWeight(Location location, ref float result)
        {
            int hpsInOneRange = GetHealthPackCountInRange(location, 1);
            int hpsInTwoRange = GetHealthPackCountInRange(location, 2);

            result /= 0.5f * (hpsInTwoRange - hpsInOneRange) + 1;
            result /= hpsInOneRange + 1;
        }

        private void ApplyWallsWeight(Location location, ref float result)
        {
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
        }

        private void ApplyMonstersWeight(Location location, ref float result)
        {
            int monstersInOneRange = GetMonsterCountInRange(location, 1);
            int monstersInTwoRange = GetMonsterCountInRange(location, 2);

            result *= 0.5f * (monstersInTwoRange - monstersInOneRange) + 1; //1.5?
            result *= monstersInOneRange + 1; //1.5?
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
            //int count = 0;
            //for (int x = location.X - range; x <= location.X + range; ++x)
            //{
            //    if (x >= 0 && x < CachedWalls.GetLength(0))
            //        for (int y = location.Y - range; y <= location.Y + range; ++y)
            //        {
            //            if (y >= 0 && y < CachedWalls.GetLength(1) && CachedWalls[x, y])
            //                count++;
            //        }
            //}

            //var r= _Context.Level.Field.GetCellsOfType(CellType.Wall).ToList().Where(w => location.IsInRange(w, range));
            return _CachedWalls_Old.Count(w => location.IsInRange(w, range));

            //if (count != count2)
            //    throw new System.Exception(count + " " + count2);
        }

        private bool AllCellsInRangeAreVisible(Location location, int range)
        {
            return !_Context.Level.Field.GetCellsOfType(CellType.Hidden).Any(c => location.IsInRange(c, range));
        }
    }
}
