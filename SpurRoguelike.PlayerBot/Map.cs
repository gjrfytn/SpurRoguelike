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
        public IEnumerable<Location> HealthPacks { get; private set; }
        public IEnumerable<Location> Monsters { get; private set; }
        public IEnumerable<Location> Items => _Level.Items.Select(i => i.Location);

        private const int _DefaultCellWeight = 1;
        private const int _HiddenCellsWeight = 10;

        private IMapNavigationContext _Context;
        private ILevelView _Level;
        private IFieldView _Field;
        private Location? _Exit;

        private List<Location> _Traps;
        private List<Location> _HiddenCells;

        private float?[,] _CachedWallsWeights;

        private bool[,] _CachedWalls;
        private bool[,] _WallsCacheLocations;

        public Map(IMapNavigationContext context)
        {
            _Context = context;
        }

        public void InitializeTurn(Location? exit)
        {
            _Field = _Level.Field;
            _Exit = exit;

            _Traps = _Field.GetCellsOfType(CellType.Trap).ToList();
            HealthPacks = _Level.HealthPacks.Select(p => p.Location).ToList();
            Monsters = _Level.Monsters.Select(m => m.Location).ToList();
            _HiddenCells = _Field.GetCellsOfType(CellType.Hidden).ToList();

            if (!_WallsCacheLocations[_Level.Player.Location.X, _Level.Player.Location.Y])
                CacheWalls();
        }

        public void InitializeLevel(ILevelView level)
        {
            _Level = level;
            _Field = _Level.Field;

            _CachedWallsWeights = new float?[_Field.Width, _Field.Height];
            _WallsCacheLocations = new bool[_Field.Width, _Field.Height];
            _CachedWalls = new bool[_Field.Width, _Field.Height];
        }

        #region IBlockedProvider

        public bool IsBlocked(Tile coord)
        {
            Location location = ConvertToLocation(coord);

            if (location == _Context.TargetLocation)
                return false;

            if (location.X < 0 || location.Y < 0 || location.X >= _Field.Width || location.Y >= _Field.Height)
                return true;

            if (_CachedWalls[location.X, location.Y])
                return true;

            CellType cellType = _Field[location];

            if (cellType == CellType.Trap || cellType == CellType.Exit)
                return true;

            return _Level.GetHealthPackAt(location).HasValue ||
                   _Level.GetItemAt(location).HasValue ||
                   _Level.GetMonsterAt(location).HasValue;
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

        public bool LocationIsVisible(Location location)
        {
            return _Field[location] != CellType.Hidden;
        }

        public static bool LocationIsVisible(IFieldView field, Location location)
        {
            return field[location] != CellType.Hidden;
        }

        private void CacheWalls()
        {
            for (int x = 0; x < _Field.Width; ++x)
                for (int y = 0; y < _Field.Height; ++y)
                {
                    Location location = new Location(x, y);
                    if (_Field[location] == CellType.Wall && !(_Exit.HasValue && location.IsInStepRange(_Exit.Value))) // _Exit - для уровня с боссом
                        _CachedWalls[x, y] = true;
                }

            _WallsCacheLocations[_Level.Player.Location.X, _Level.Player.Location.Y] = true;
        }

        private bool IsLocationHidden(Location location)
        {
            return _Field[location] == CellType.Hidden;
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
            weight /= 0.25f * GetTrapCountInRange(location, 1) + 1;
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

            result *= 0.7f * (monstersInTwoRange - monstersInOneRange) + 1;
            result *= 1.5f * monstersInOneRange + 1;
        }

        private static Location ConvertToLocation(Tile tile)
        {
            return new Location((int)tile.X, (int)tile.Y);
        }

        private int GetTrapCountInRange(Location location, int range)
        {
            return _Traps.Count(t => location.IsInRange(t, range));
        }

        private int GetHealthPackCountInRange(Location location, int range)
        {
            return HealthPacks.Count(p => location.IsInRange(p, range));
        }

        private int GetMonsterCountInRange(Location location, int range)
        {
            return Monsters.Count(m => location.IsInRange(m, range));
        }

        private int GetWallsCountInRange(Location location, int range)
        {
            int count = 0;
            for (int x = location.X - range; x <= location.X + range; ++x)
            {
                if (x >= 0 && x < _CachedWalls.GetLength(0))
                    for (int y = location.Y - range; y <= location.Y + range; ++y)
                    {
                        if (y >= 0 && y < _CachedWalls.GetLength(1) && _CachedWalls[x, y])
                            count++;
                    }
            }

            return count;
        }

        private bool AllCellsInRangeAreVisible(Location location, int range)
        {
            return !_HiddenCells.Any(c => location.IsInRange(c, range));
        }
    }
}
