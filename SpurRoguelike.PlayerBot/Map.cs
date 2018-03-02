using System.Collections.Generic;
using AStarNavigator;
using AStarNavigator.Providers;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot
{
    class Map : IBlockedProvider, INeighborProvider
    {
        private IMapPathfindingContext _Context;

        public Map(IMapPathfindingContext context)
        {
            _Context = context;
        }

        #region IBlockedProvider

        public bool IsBlocked(Tile coord)
        {
            Location location = new Location((int)coord.X, (int)coord.Y);

            if (location == _Context.TargetLocation)
                return false;

            if (location.X < 0 || location.Y < 0 || location.X >= _Context.Level.Field.Width || location.Y >= _Context.Level.Field.Height)
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
    }
}
