using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class PlayerBot : IPlayerController, IPathfindingContext
    {
        private readonly AStarNavigator.TileNavigator _Navigator;

        private LevelView _LevelView;
        private PawnView _Player;

        private readonly Map _Map;

        private Location _Exit => _LevelView.Field.GetCellsOfType(CellType.Exit).Single();

        public PlayerBot()
        {
            _Map = new Map(this);
            _Navigator = new AStarNavigator.TileNavigator(_Map, _Map, new AStarNavigator.Algorithms.PythagorasAlgorithm(), new AStarNavigator.Algorithms.ManhattanHeuristicAlgorithm());
        }

        #region IPlayerController

        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            InitializeTurn(levelView);

            return GoTo(_Exit);
        }

        #endregion

        #region IPathfindingContext

        public Location TargetLocation { get; set; }

        #endregion

        private void InitializeTurn(LevelView levelView)
        {
            _LevelView = levelView;
            _Player = levelView.Player;

            _Map.Update(levelView);
        }

        private Turn GoTo(Location location)
        {
            TargetLocation = location;

            IEnumerable<AStarNavigator.Tile> path = _Navigator.Navigate(
                new AStarNavigator.Tile(_Player.Location.X, _Player.Location.Y),
                new AStarNavigator.Tile(location.X, location.Y)
                );

            Offset nextStep = new Offset((int)path.First().X - _Player.Location.X, (int)path.First().Y - _Player.Location.Y);

            return Turn.Step(nextStep);
        }
    }
}