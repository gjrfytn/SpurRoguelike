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

        public LevelView _LevelView;
        public PawnView _Player;
        private Location _Exit;

        private readonly Map _Map;
        private BotReporter _Reporter;
        private BotNavigator _Navigator;

        private int _PreviousHealth;
        public List<AStarNavigator.Tile> _CachedPath = new List<AStarNavigator.Tile>();
        public int _CachedPathPointIndex;
        public Location _CachedPathLastCacheLocation;
        public bool _DiscardCache = true;

        public bool _BeingCareful;

        public PlayerBot()
        {
            _Map = new Map(this);
            _Navigator = new BotNavigator(
                new AStarNavigator.TileNavigator(_Map, _Map, _Map, new AStarNavigator.Algorithms.ManhattanHeuristicAlgorithm())
                );
        }

        #region IPlayerController

        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            InitializeTurn(levelView, messageReporter);

            if (_BeingCareful)
            {
                //System.Threading.Thread.Sleep(200);
                _Reporter.Say("Being careful.");
            }

            Turn turn = CheckForHealth();
            if (turn != null)
            {
                _Reporter.Say("Checking for health.");
                return turn;
            }

            turn = KillWeakenedMonster();
            if (turn != null)
            {
                _Reporter.Say("Killing weak monster.");
                return turn;
            }

            turn = CheckForBestItem();
            if (turn != null)
            {
                _Reporter.Say("Going for item.");
                return turn;
            }

            turn = GrindMonsters();
            if (turn != null)
            {
                _Reporter.Say("Grinding monsters.");
                return turn;
            }

            turn = GoToExit();
            if (turn != null)
            {
                _Reporter.Say("Going to exit.");
                return turn;
            }

            _Reporter.Say("DO NOT KNOW WHAT TO DO!!!");
            return Panic();
        }

        #endregion

        #region IPathfindingContext

        public LevelView Level => _LevelView;

        public Location TargetLocation { get; set; }

        public bool ApplyWeights => _BeingCareful;

        #endregion

        private void InitializeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            _LevelView = levelView;
            _Player = levelView.Player;

            _BeingCareful = _LevelView.Monsters.Count(m => m.Location.IsInRange(_Player.Location, 5)) >= 6 / (_Player.Health < 50 ? 2 : 1);
            _ClearPathAttempts.Clear();

            Location exit = _LevelView.Field.GetCellsOfType(CellType.Exit).Single();
            if (_Exit != exit)
            {
                _Exit = exit;
                InitializeLevel(messageReporter);
            }

            _Map.InitializeTurn();
            _Navigator.InitializeTurn(_Player.Location);
        }

        private void InitializeLevel(IMessageReporter messageReporter)
        {
            _Map.InitializeLevel(_Exit);
            _Reporter = new BotReporter(messageReporter);
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
                return _Navigator.GoTo(itemsByDistance.First().Location, this);

            var itemsByPower = _LevelView.Items.OrderByDescending(i => CalulateItemPower(i));

            if (CalulateItemPower(playerItem) + 0.001f > CalulateItemPower(itemsByPower.First())) //TODO Костыль с 0.001f
                return null;

            return _Navigator.GoTo(itemsByPower.First().Location, this);
        }

        private Turn GrindMonsters()
        {
            var monsters = _LevelView.Monsters.OrderBy(m => _Player.Location.CalculateDistance(m.Location));

            if (!monsters.Any())
                return null;

            var closestMonster = monsters.First();

            if (closestMonster.Location.IsInRange(_Player.Location, 1))
                return Turn.Attack(closestMonster.Location - _Player.Location);

            return _Navigator.GoTo(closestMonster.Location, this);
        }

        private Turn GoToExit()
        {
            Turn turn = null;
            if (_Player.Health != _PlayerMaxHealth)
                turn = GoToClosestHealthPack();

            if (turn != null)
                return turn;

            return _Navigator.GoTo(_Exit, this);
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
                return _Navigator.GoToClosest(packs.Take(3).Select(p => p.Location), this);

            return _Navigator.GoTo(packs.First().Location, this);
        }

        private List<Location> _ClearPathAttempts = new List<Location>();

        public Turn TryClearPath(Location location)
        {
            if (_ClearPathAttempts.Contains(location))
                return null;

            _ClearPathAttempts.Add(location);

            Turn turn = null;
            IEnumerable<HealthPackView> packs = _LevelView.HealthPacks.Where(p => location.IsInStepRange(p.Location));
            foreach (HealthPackView pack in packs)
            {
                turn = _Navigator.GoTo(pack.Location, this);

                if (turn != null)
                    return turn;
            }

            IEnumerable<ItemView> items = _LevelView.Items.Where(i => location.IsInStepRange(i.Location));
            foreach (ItemView item in items)
            {
                turn = _Navigator.GoTo(item.Location, this);

                if (turn != null)
                    return turn;
            }

            IEnumerable<PawnView> monsters = _LevelView.Monsters.Where(m => location.IsInStepRange(m.Location));
            foreach (PawnView monster in monsters)
            {
                turn = _Navigator.GoTo(monster.Location, this);

                if (turn != null)
                    return turn;
            }

            return null;
        }

        private static float CalulateItemPower(ItemView item)
        {
            return item.AttackBonus + item.DefenceBonus - (System.Math.Abs(item.AttackBonus - item.DefenceBonus) / 1000f);
        }

        public bool LocationIsVisible(Location location)
        {
            return _LevelView.Field[location] != CellType.Hidden;
        }

        private int CalculateDamage(PawnView attacker, PawnView target, bool maxDamage)
        {
            return (int)(((float)attacker.TotalAttack / target.Defence) * _BaseDamage * (maxDamage ? 1 : 0.95f));
        }
    }
}
