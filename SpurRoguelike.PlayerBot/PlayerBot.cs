using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;
using SpurRoguelike.PlayerBot.Extensions;

namespace SpurRoguelike.PlayerBot
{
    public class PlayerBot : IPlayerController, IMapPathfindingContext
    {
        private const int _PlayerMaxHealth = 100;
        private const int _BaseDamage = 10;

        private readonly AStarNavigator.TileNavigator _Navigator;

        private LevelView _LevelView;
        private PawnView _Player;
        private Location _Exit;

        private readonly Map _Map;

        private int _PreviousHealth;
        private List<AStarNavigator.Tile> _CachedPath = new List<AStarNavigator.Tile>();
        private int _CachedPathPointIndex;
        private Location _CachedPathLastCacheLocation;
        private bool _DiscardCache = true;
        private bool[,] CacheLocations { get; set; }

        private bool _BeingCareful;


        public PlayerBot()
        {
            _Map = new Map(this);
            _Navigator = new AStarNavigator.TileNavigator(_Map, _Map, _Map, new AStarNavigator.Algorithms.ManhattanHeuristicAlgorithm());
        }

        #region IPlayerController

        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            InitializeTurn(levelView);

            if (_BeingCareful)
            {
                //System.Threading.Thread.Sleep(200);
                Say(messageReporter, "Being careful.");
            }

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

        public bool ApplyWeights => _BeingCareful;

        public bool[,] CachedWalls { get; set; }

        #endregion

        private void InitializeTurn(LevelView levelView)
        {
            _LevelView = levelView;
            _Player = levelView.Player;
            _Exit = _LevelView.Field.GetCellsOfType(CellType.Exit).Single();

            _BeingCareful = _LevelView.Monsters.Count(m => m.Location.IsInRange(_Player.Location, 5)) >= 6 / (_Player.Health < 50 ? 2 : 1);
            _ClearPathAttempts.Clear();

            if (_LevelView.Field.Width != CachedWalls?.GetLength(0) || _LevelView.Field.Height != CachedWalls?.GetLength(1))
            {
                CacheLocations = new bool[_LevelView.Field.Width, _LevelView.Field.Height];
                CachedWalls = new bool[_LevelView.Field.Width, _LevelView.Field.Height];
                CachedWalls[_Exit.X, _Exit.Y] = true;
                CacheWalls();
                _Map.InitializeLevelCache();
            }

            if (!CacheLocations[_Player.Location.X, _Player.Location.Y])
                CacheWalls();

            _Map.InitializeTurnCache();
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
            var monsters = _LevelView.Monsters.Select(m => new { Monster = m, Distance = _Player.Location.CalculateDistance(m.Location) }).OrderBy(m => m.Distance);

            if (!monsters.Any())
                return null;

            var closestMonster = monsters.First();

            if (!closestMonster.Monster.Location.IsInRange(_Player.Location, 1))
                return null;

            int averagePlayerDamage = CalculateDamage(_Player, closestMonster.Monster, false);
            int turnsToKill = (int)System.Math.Ceiling((float)closestMonster.Monster.Health / averagePlayerDamage);

            if (monsters.Count(m => m.Distance < turnsToKill) > 2)
                return null;

            return Turn.Attack(closestMonster.Monster.Location - _Player.Location);
        }

        private Turn CheckForBestItem()
        {
            var itemsByDistance = _LevelView.Items.OrderBy(p => _Player.Location.CalculateDistance(p.Location));

            if (!itemsByDistance.Any())
                return null;

            if (!_Player.TryGetEquippedItem(out ItemView playerItem))
                return GoTo(itemsByDistance.First().Location);

            var itemsByPower = _LevelView.Items.OrderByDescending(i => CalulateItemPower(i));

            if (CalulateItemPower(playerItem) + 0.001f > CalulateItemPower(itemsByPower.First())) //TODO Костыль с 0.001f
                return null;

            return GoTo(itemsByPower.First().Location);
        }

        private Turn GrindMonsters()
        {
            var monsters = _LevelView.Monsters.OrderBy(m => _Player.Location.CalculateDistance(m.Location));

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
            var packs = _LevelView.HealthPacks.OrderBy(p => _Player.Location.CalculateDistance(p.Location));

            if (!packs.Any())
                return null;

            if (_BeingCareful)
                return GoToClosest(packs.Take(3).Select(p => p.Location));

            return GoTo(packs.First().Location);
        }

        private Turn GoTo(Location location)
        {
            AStarNavigator.Tile tile;
            if (LocationIsVisible(location) || _BeingCareful) //TODO _BeingCareful - костыль?
            {
                _DiscardCache = true;

                TargetLocation = location;

                IEnumerable<AStarNavigator.Tile> path = _Navigator.Navigate(
                    new AStarNavigator.Tile(_Player.Location.X, _Player.Location.Y),
                    new AStarNavigator.Tile(location.X, location.Y)
                    );

                if (path == null)
                    return TryClearPath(location);

                tile = path.First();
            }
            else
            {
                Offset offset = _Player.Location - _CachedPathLastCacheLocation;
                if (_DiscardCache || System.Math.Abs(offset.XOffset) == _LevelView.Field.VisibilityWidth - 1 || System.Math.Abs(offset.YOffset) == _LevelView.Field.VisibilityHeight - 1)
                {
                    TargetLocation = location;

                    _CachedPathPointIndex = 0;

                    _CachedPath = _Navigator.Navigate(
                        new AStarNavigator.Tile(_Player.Location.X, _Player.Location.Y),
                        new AStarNavigator.Tile(location.X, location.Y)
                        )?.ToList();

                    if (_CachedPath == null)
                    {
                        _DiscardCache = true;

                        return TryClearPath(location);
                    }

                    _DiscardCache = false;
                    _CachedPathLastCacheLocation = _Player.Location;
                }

                tile = _CachedPath[_CachedPathPointIndex];

                _CachedPathPointIndex++;
            }

            return Turn.Step(new Offset((int)tile.X - _Player.Location.X, (int)tile.Y - _Player.Location.Y));
        }

        private Turn GoToClosest(IEnumerable<Location> locations)
        {
            AStarNavigator.Tile tile;

            _DiscardCache = true;

            List<IEnumerable<AStarNavigator.Tile>> paths = new List<IEnumerable<AStarNavigator.Tile>>();
            foreach (Location location in locations)
            {
                TargetLocation = location;

                IEnumerable<AStarNavigator.Tile> path = _Navigator.Navigate(
                    new AStarNavigator.Tile(_Player.Location.X, _Player.Location.Y),
                    new AStarNavigator.Tile(location.X, location.Y)
                    );

                if (path != null)
                    paths.Add(path);
            }

            if (!paths.Any())
                return null;

            tile = paths.OrderBy(p => p.Count()).First().First();

            return Turn.Step(new Offset((int)tile.X - _Player.Location.X, (int)tile.Y - _Player.Location.Y));
        }

        private List<Location> _ClearPathAttempts = new List<Location>();

        private Turn TryClearPath(Location location)
        {
            if (_ClearPathAttempts.Contains(location))
                return null;

            _ClearPathAttempts.Add(location);

            IEnumerable<HealthPackView> packs = _LevelView.HealthPacks.Where(p => location.IsInStepRange(p.Location));
            IEnumerable<ItemView> items = _LevelView.Items.Where(i => location.IsInStepRange(i.Location));
            IEnumerable<PawnView> monsters = _LevelView.Monsters.Where(m => location.IsInStepRange(m.Location));

            Turn turn = null;
            foreach (HealthPackView pack in packs)
            {
                turn = GoTo(pack.Location);

                if (turn != null)
                    return turn;
            }

            foreach (ItemView item in items)
            {
                turn = GoTo(item.Location);

                if (turn != null)
                    return turn;
            }

            foreach (PawnView monster in monsters)
            {
                turn = GoTo(monster.Location);

                if (turn != null)
                    return turn;
            }

            return null;
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

        private int CalculateDamage(PawnView attacker, PawnView target, bool maxDamage)
        {
            return (int)(((float)attacker.TotalAttack / target.Defence) * _BaseDamage * (maxDamage ? 1 : 0.95f));
        }

        private void CacheWalls()
        {
            Location location;
            for (int x = 0; x < _LevelView.Field.Width; ++x)
            {
                for (int y = 0; y < _LevelView.Field.Height; ++y)
                {
                    location = new Location(x, y);
                    if (_LevelView.Field[location] == CellType.Wall && !location.IsInStepRange(_Exit)) // TODO _Exit - для уровня с боссом
                        CachedWalls[x, y] = true;
                }
            }

            CacheLocations[_Player.Location.X, _Player.Location.Y] = true;
        }
    }
}