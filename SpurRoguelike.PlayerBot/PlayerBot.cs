using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class PlayerBot : IPlayerController, IMapPathfindingContext
    {
        private const int _PlayerMaxHealth = 100;
        private const int _BaseDamage = 10;

        private readonly AStarNavigator.TileNavigator _Navigator;

        private LevelView _LevelView;
        private PawnView _Player;

        private readonly Map _Map;

        private Location _Exit => _LevelView.Field.GetCellsOfType(CellType.Exit).Single();

        private int _PreviousHealth;
        private List<AStarNavigator.Tile> _CachedPath = new List<AStarNavigator.Tile>();
        private int _CachedPathPointIndex;
        private int _PathfindingRetriesCount;

        private bool _BeCareful;

        public PlayerBot()
        {
            _Map = new Map(this);
            _Navigator = new AStarNavigator.TileNavigator(_Map, _Map, _Map, new AStarNavigator.Algorithms.ManhattanHeuristicAlgorithm());
        }

        #region IPlayerController

        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            InitializeTurn(levelView);

            if (_BeCareful)
                Say(messageReporter, "Being careful.");

            Turn turn = CheckForHealth();
            if (turn != null)
            {
                Say(messageReporter, "Checking for health.");
                return turn;
            }

            turn = KillWeakenedMonster();
            if (turn != null)
            {
                Say(messageReporter, "Killing weak monster.");
                return turn;
            }

            turn = CheckForBestItem();
            if (turn != null)
            {
                Say(messageReporter, "Going for item.");
                return turn;
            }

            turn = GrindMonsters();
            if (turn != null)
            {
                Say(messageReporter, "Grinding monsters.");
                return turn;
            }

            turn = GoToExit();
            if (turn != null)
            {
                Say(messageReporter, "Going to exit.");
                return turn;
            }

            Say(messageReporter, "DO NOT KNOW WHAT TO DO!!!");
            return Panic();
        }

        #endregion

        #region IPathfindingContext

        public LevelView Level => _LevelView;

        public Location TargetLocation { get; set; }

        public bool ApplyWeights => _BeCareful;

        #endregion

        private void InitializeTurn(LevelView levelView)
        {
            _LevelView = levelView;
            _Player = levelView.Player;

            _BeCareful = _LevelView.Monsters.Count(m => m.Location.IsInRange(_Player.Location, 5)) >= 6 / (_Player.Health < 50 ? 2 : 1);
        }

        private Turn CheckForHealth()
        {
            if (_PreviousHealth >= _Player.Health)
                return GoToClosestHealthPack();

            _PreviousHealth = 0;

            int monstersCount = _LevelView.Monsters.Count(m => m.Location.IsInRange(_Player.Location, 10));

            double multiplier = 0.3 / (1 + System.Math.Exp(-2 * (monstersCount - 4))) + 0.5;

            if (_Player.Health >= _PlayerMaxHealth * multiplier)
                return null;

            _PreviousHealth = _Player.Health;

            return GoToClosestHealthPack();
        }

        private Turn KillWeakenedMonster()
        {
            var monsters = _LevelView.Monsters.Select(m => new { Monster = m, Distance = CalculateDistance(_Player.Location, m.Location) }).OrderBy(m => m.Distance);

            if (!monsters.Any())
                return null;

            var closestMonster = monsters.First();

            if (!closestMonster.Monster.Location.IsInRange(_Player.Location, 1))
                return null;

            int turnsToKill = (int)System.Math.Ceiling(closestMonster.Monster.Health / (((float)_Player.TotalAttack / closestMonster.Monster.Defence) * _BaseDamage * 0.95f));

            if (monsters.Count(m => m.Distance < turnsToKill) > 2)
                return null;

            return Turn.Attack(closestMonster.Monster.Location - _Player.Location);
        }

        private Turn CheckForBestItem()
        {
            var itemsByDistance = _LevelView.Items.OrderBy(p => CalculateDistance(_Player.Location, p.Location));

            if (!itemsByDistance.Any())
                return null;

            if (!_Player.TryGetEquippedItem(out ItemView playerItem))
                return GoTo(itemsByDistance.First().Location);

            var itemsByPower = _LevelView.Items.OrderByDescending(i => CalulateItemPower(i));

            if (CalulateItemPower(playerItem) >= CalulateItemPower(itemsByPower.First()))
                return null;

            return GoTo(itemsByPower.First().Location);
        }

        private Turn GrindMonsters()
        {
            var monsters = _LevelView.Monsters.OrderBy(m => CalculateDistance(_Player.Location, m.Location));

            if (!monsters.Any())
                return null;

            var closestMonster = monsters.First();

            if (closestMonster.Location.IsInRange(_Player.Location, 1))
                return Turn.Attack(closestMonster.Location - _Player.Location);

            return GoTo(closestMonster.Location);
        }

        private Turn GoToExit()
        {
            Turn turn = null;
            if (_Player.Health != _PlayerMaxHealth)
                turn = GoToClosestHealthPack();

            if (turn != null)
                return turn;

            return GoTo(_Exit);
        }

        private Turn Panic()
        {
            return Turn.Step((StepDirection)_LevelView.Random.Next(4));
        }

        private Turn GoToClosestHealthPack()
        {
            var packs = _LevelView.HealthPacks.OrderBy(p => CalculateDistance(_Player.Location, p.Location));

            if (!packs.Any())
                return null;

            return GoTo(packs.First().Location);
        }

        private Turn GoTo(Location location)
        {
            AStarNavigator.Tile tile;
            if (LocationIsVisible(location))
            {
                TargetLocation = location;

                IEnumerable<AStarNavigator.Tile> path = _Navigator.Navigate(
                    new AStarNavigator.Tile(_Player.Location.X, _Player.Location.Y),
                    new AStarNavigator.Tile(location.X, location.Y)
                    );

                if (path == null)
                {
                    _PathfindingRetriesCount++;

                    if (_PathfindingRetriesCount == 3)
                    {
                        _PathfindingRetriesCount = 0;

                        return null;
                    }

                    return Turn.None;
                }

                tile = path.First();
            }
            else
            {
                if (_CachedPathPointIndex == _CachedPath.Count || _CachedPathPointIndex == System.Math.Min(_LevelView.Field.VisibilityWidth, _LevelView.Field.VisibilityHeight) - 1)
                {
                    TargetLocation = location;

                    _CachedPathPointIndex = 0;

                    _CachedPath = _Navigator.Navigate(
                        new AStarNavigator.Tile(_Player.Location.X, _Player.Location.Y),
                        new AStarNavigator.Tile(location.X, location.Y)
                        )?.ToList();

                    if (_CachedPath == null)
                    {
                        _PathfindingRetriesCount++;

                        if (_PathfindingRetriesCount == 3)
                        {
                            _PathfindingRetriesCount = 0;

                            return null;
                        }

                        return Turn.None;
                    }
                }

                tile = _CachedPath[_CachedPathPointIndex];

                _CachedPathPointIndex++;
            }

            return Turn.Step(new Offset((int)tile.X - _Player.Location.X, (int)tile.Y - _Player.Location.Y));
        }

        private static int CalculateDistance(Location location1, Location location2)
        {
            return System.Math.Abs(location1.X - location2.X) + System.Math.Abs(location1.Y - location2.Y);
        }

        private static float CalulateItemPower(ItemView item)
        {
            return item.AttackBonus + item.DefenceBonus - (System.Math.Abs(item.AttackBonus - item.DefenceBonus) / 1000f);
        }

        private void Say(IMessageReporter reporter, string message)
        {
            reporter.ReportMessage("[BOT]: " + message);
        }

        private bool LocationIsVisible(Location location)
        {
            return _LevelView.Field[location] != CellType.Hidden;
        }
    }
}