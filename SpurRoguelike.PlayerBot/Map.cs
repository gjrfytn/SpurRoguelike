using System.Collections.Generic;
using AStarNavigator;
using AStarNavigator.Providers;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    class Map : IBlockedProvider, INeighborProvider
    {
        private LevelView _Level;
        private IPathfindingContext _Context;

        public Map(IPathfindingContext context)
        {
            _Context = context;
        }

        public void Update(LevelView level)
        {
            _Level = level;
        }

        #region IBlockedProvider

        public bool IsBlocked(Tile coord)
        {
            Location location = new Location((int)coord.X, (int)coord.Y);

            if (location == _Context.TargetLocation)
                return false;

            if (location.X < 0 || location.Y < 0 || location.X >= _Level.Field.Width || location.Y >= _Level.Field.Height)
                return true;

            CellType cellType = _Level.Field[location];

            if (cellType == CellType.Wall || cellType == CellType.Trap || cellType == CellType.Exit)
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
    }
}
