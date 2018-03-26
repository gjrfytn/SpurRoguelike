using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;
using SpurRoguelike.PlayerBot.Extensions;

namespace SpurRoguelike.PlayerBot
{
    public class PlayerBot : IPlayerController
    {
        private const int _PlayerMaxHealth = 100;
        private const int _BaseDamage = 10;

        private readonly System.Func<Turn>[] _Behaviours;

        private LevelView _Level;
        private PawnView _Player;
        private Location? _Exit;
        private int _LevelWidth;
        private int _LevelHeight;

        private BotReporter _Reporter;
        private BotNavigator _Navigator;

        private int _PreviousHealth;
        private bool _InDanger;
        private List<Location> _FoundHealthPacks = new List<Location>();
        private List<Location> _UnexploredLocations = new List<Location>();
        private Location? _TargetMonster;
        private Location? _TargetItem;

        public PlayerBot()
        {
            _Navigator = new BotNavigator();

            _Behaviours = new System.Func<Turn>[]
            {
                CheckForHealth,
                KillWeakenedMonster,
                CheckForBestItem,
                GrindMonsters,
                Explore,
                GoToExit,
                Panic
            };
        }

        #region IPlayerController

        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            InitializeTurn(levelView, messageReporter);

            foreach (System.Func<Turn> behaviour in _Behaviours)
            {
                Turn turn = behaviour();

                if (turn != null)
                {
                    _Reporter.Say(behaviour.Method.Name);

                    return turn;
                }
            }

            throw new System.InvalidOperationException();
        }

        #endregion

        private void InitializeLevel(LevelView level, IMessageReporter messageReporter)
        {
            _Exit = null;
            _Reporter = new BotReporter(messageReporter);

            _FoundHealthPacks.Clear();
            InitializeLocationsToExplore();

            _Navigator.InitializeLevel(level);
        }

        private void InitializeTurn(LevelView level, IMessageReporter messageReporter)
        {
            _Level = level;
            _Player = level.Player;

            if (_LevelWidth != _Level.Field.Width || _LevelHeight != _Level.Field.Height)
            {
                _LevelWidth = _Level.Field.Width;
                _LevelHeight = _Level.Field.Height;
                InitializeLevel(level, messageReporter);
            }

            Location exit = _Level.Field.GetCellsOfType(CellType.Exit).SingleOrDefault();
            if (exit != default(Location))
                _Exit = exit;

            CheckInLocations();

            EstimateDanger();

            _Navigator.InitializeTurn(_Exit, _Player.Location, _InDanger);
        }

        #region Behaviours

        private Turn CheckForHealth()
        {
            CheckInHealthPacks();

            if (_PreviousHealth >= _Player.Health)
                return GoToClosestHealthPack();

            _PreviousHealth = 0;

            if (!_InDanger)
                return null;

            _PreviousHealth = _Player.Health;

            return GoToClosestHealthPack();
        }

        private Turn KillWeakenedMonster()
        {
            var monsters = _Level.Monsters.Select(m => new { Monster = m, Distance = _Player.Location.CalculateDistance(m.Location) }).OrderBy(m => m.Distance);

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
            if (_TargetItem.HasValue && !Map.LocationIsVisible(_Level.Field, _TargetItem.Value))
                return _Navigator.GoTo(_TargetItem.Value);

            _TargetItem = null;

            var items = _Level.Items;

            if (!items.Any())
                return null;

            if (!_Player.TryGetEquippedItem(out ItemView playerItem))
                return _Navigator.GoTo(items.OrderBy(p => _Player.Location.CalculateDistance(p.Location)).First().Location);

            ItemView item = items.OrderByDescending(i => CalulateItemPower(i)).First();
            if (CalulateItemPower(playerItem) + 0.001f > CalulateItemPower(item)) //TODO Костыль с 0.001f
                return null;

            _TargetItem = item.Location;
            return _Navigator.GoTo(_TargetItem.Value);
        }

        private Turn GrindMonsters()
        {
            var monsters = _Level.Monsters.OrderBy(m => _Player.Location.CalculateDistance(m.Location));

            if (!monsters.Any())
            {
                if (_TargetMonster.HasValue && !Map.LocationIsVisible(_Level.Field, _TargetMonster.Value))
                    return _Navigator.GoTo(_TargetMonster.Value);

                _TargetMonster = null;

                return null;
            }

            _TargetMonster = monsters.First().Location;

            if (_TargetMonster.Value.IsInRange(_Player.Location, 1))
                return Turn.Attack(_TargetMonster.Value - _Player.Location);

            return _Navigator.GoTo(_TargetMonster.Value);
        }

        private Turn Explore()
        {
            var locations = _UnexploredLocations.OrderBy(l => _Player.Location.CalculateDistance(l));

            if (!locations.Any())
                return null;

            foreach (Location location in locations)
            {
                Turn turn = _Navigator.GoTo(location);
                if (turn != null)
                    return turn;
                else
                    _UnexploredLocations.Remove(location);
            }

            return null;
        }

        private Turn GoToExit()
        {
            Turn turn = null;
            if (_Player.Health != _PlayerMaxHealth)
                turn = GoToClosestHealthPack();

            if (turn != null)
                return turn;

            return _Navigator.GoTo(_Exit.Value);
        }

        private Turn Panic()
        {
            return Turn.Step((StepDirection)_Level.Random.Next(4));
        }

        #endregion

        private Turn GoToClosestHealthPack()
        {
            var packs = _FoundHealthPacks.OrderBy(p => _Player.Location.CalculateDistance(p));

            if (!packs.Any())
                return null;

            if (_InDanger)
                return _Navigator.GoToClosest(packs.Take(3).Select(p => p));

            return _Navigator.GoTo(packs.First());
        }

        private static float CalulateItemPower(ItemView item)
        {
            return item.AttackBonus + item.DefenceBonus - (System.Math.Abs(item.AttackBonus - item.DefenceBonus) / 1000f);
        }

        private static int CalculateDamage(PawnView attacker, PawnView target, bool maxDamage)
        {
            return (int)(((float)attacker.TotalAttack / target.Defence) * _BaseDamage * (maxDamage ? 1 : 0.95f));
        }

        private void InitializeLocationsToExplore()
        {
            _UnexploredLocations.Clear();

            if (_Level.Field.VisibilityWidth == int.MaxValue)
                return;

            int stepX = 2 * _Level.Field.VisibilityWidth;
            int stepY = 2 * _Level.Field.VisibilityHeight;

            for (int x = 0; x < _LevelWidth; x += stepX)
            {
                _UnexploredLocations.Add(new Location(x, _LevelHeight - 1));

                for (int y = 0; y < _LevelHeight; y += stepY)
                {
                    _UnexploredLocations.Add(new Location(x, y));
                }
            }

            for (int y = 0; y < _LevelHeight; y += stepY)
                _UnexploredLocations.Add(new Location(_LevelWidth - 1, y));

            _UnexploredLocations.Add(new Location(_LevelWidth - 1, _LevelHeight - 1));
        }

        private void CheckInLocations()
        {
            _UnexploredLocations.RemoveAll(l => _Level.Field[l] != CellType.Hidden);
        }

        private void CheckInHealthPacks()
        {
            for (int i = _FoundHealthPacks.Count - 1; i >= 0; --i)
                if (Map.LocationIsVisible(_Level.Field, _FoundHealthPacks[i]) && !_Level.GetHealthPackAt(_FoundHealthPacks[i]).HasValue)
                    _FoundHealthPacks.RemoveAt(i);

            foreach (HealthPackView pack in _Level.HealthPacks)
                if (!_FoundHealthPacks.Contains(pack.Location))
                    _FoundHealthPacks.Add(pack.Location);
        }

        private void EstimateDanger()
        {
            int monstersCount = _Level.Monsters.Count(m => m.Location.IsInRange(_Player.Location, 8));

            double multiplier = 0.3 / (1 + System.Math.Exp(-2 * (monstersCount - 4))) + 0.5;

            _InDanger = _Player.Health < _PlayerMaxHealth * multiplier;
        }
    }
}
