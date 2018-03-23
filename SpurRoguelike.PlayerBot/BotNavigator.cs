using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot
{
    class BotNavigator
    {
        private readonly AStarNavigator.ITileNavigator _Navigator;
        private Location _PlayerLocation;

        public BotNavigator(AStarNavigator.ITileNavigator navigator)
        {
            _Navigator = navigator;
        }

        public void InitializeTurn(Location playerLocation)
        {
            _PlayerLocation = playerLocation;
        }

        public Turn GoTo(Location location, PlayerBot bot)
        {
            AStarNavigator.Tile tile;
            if (bot.LocationIsVisible(location) || bot._BeingCareful)
            {
                bot._DiscardCache = true;

                IEnumerable<AStarNavigator.Tile> path = Navigate(location, bot);

                if (path == null)
                    return bot.TryClearPath(location);

                tile = path.First();
            }
            else
            {
                Offset offset = _PlayerLocation - bot._CachedPathLastCacheLocation;
                if (bot._DiscardCache || System.Math.Abs(offset.XOffset) == bot._LevelView.Field.VisibilityWidth - 1 || System.Math.Abs(offset.YOffset) == bot._LevelView.Field.VisibilityHeight - 1)
                {
                    bot._CachedPathPointIndex = 0;

                    bot._CachedPath = Navigate(location, bot)?.ToList();

                    if (bot._CachedPath == null)
                    {
                        bot._DiscardCache = true;

                        return bot.TryClearPath(location);
                    }

                    bot._DiscardCache = false;
                    bot._CachedPathLastCacheLocation = _PlayerLocation;
                }

                tile = bot._CachedPath[bot._CachedPathPointIndex];

                bot._CachedPathPointIndex++;
            }

            return GetStepTurn(tile);
        }

        public Turn GoToClosest(IEnumerable<Location> locations, PlayerBot bot)
        {
            AStarNavigator.Tile tile;

            bot._DiscardCache = true;

            List<IEnumerable<AStarNavigator.Tile>> paths = new List<IEnumerable<AStarNavigator.Tile>>();
            foreach (Location location in locations)
            {
                IEnumerable<AStarNavigator.Tile> path = Navigate(location, bot);

                if (path != null)
                    paths.Add(path);
            }

            if (!paths.Any())
                return null;

            tile = paths.OrderBy(p => p.Count()).First().First();

            return GetStepTurn(tile);
        }

        private IEnumerable<AStarNavigator.Tile> Navigate(Location location, PlayerBot bot)
        {
            bot.TargetLocation = location;

            return _Navigator.Navigate(
                new AStarNavigator.Tile(_PlayerLocation.X, _PlayerLocation.Y),
                new AStarNavigator.Tile(location.X, location.Y)
                );
        }

        private Turn GetStepTurn(AStarNavigator.Tile tile)
        {
            return Turn.Step(new Offset((int)tile.X - _PlayerLocation.X, (int)tile.Y - _PlayerLocation.Y));
        }
    }
}
