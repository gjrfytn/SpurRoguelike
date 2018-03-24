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

        private LevelView _LevelView;
        private PawnView _Player;
        private Location? _Exit;
        private int _LevelWidth;
        private int _LevelHeight;

        private BotReporter _Reporter;
        private BotNavigator _Navigator;

        private int _PreviousHealth;
        private bool _BeingCareful;
        private List<Location> _UnexploredLocations = new List<Location>();

        public PlayerBot()
        {
            _Navigator = new BotNavigator();
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

            turn = Explore();
            if (turn != null)
            {
                _Reporter.Say("Exploring.");
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

        private void InitializeLevel(LevelView level, IMessageReporter messageReporter)
        {
            _Reporter = new BotReporter(messageReporter);

            InitializeLocationsToExplore();

            _Navigator.InitializeLevel(level);
        }

        private void InitializeTurn(LevelView level, IMessageReporter messageReporter)
        {
            _LevelView = level;
            _Player = level.Player;

            _BeingCareful = _LevelView.Monsters.Count(m => m.Location.IsInRange(_Player.Location, 5)) >= 6 / (_Player.Health < 70 ? 2 : 1); //TODO 50

            if (_LevelWidth != _LevelView.Field.Width || _LevelHeight != _LevelView.Field.Height)
            {
                _Exit = null;
                _LevelWidth = _LevelView.Field.Width;
                _LevelHeight = _LevelView.Field.Height;
                InitializeLevel(level, messageReporter);
            }

            Location exit = _LevelView.Field.GetCellsOfType(CellType.Exit).SingleOrDefault();
            if (exit != default(Location))
                _Exit = exit;

            CheckInLocations();

            _Navigator.InitializeTurn(_LevelView, _Exit, _Player.Location, _BeingCareful);
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
                return _Navigator.GoTo(itemsByDistance.First().Location);

            var itemsByPower = _LevelView.Items.OrderByDescending(i => CalulateItemPower(i));

            if (CalulateItemPower(playerItem) + 0.001f > CalulateItemPower(itemsByPower.First())) //TODO Костыль с 0.001f
                return null;

            return _Navigator.GoTo(itemsByPower.First().Location);
        }

        private Turn GrindMonsters()
        {
            var monsters = _LevelView.Monsters.OrderBy(m => _Player.Location.CalculateDistance(m.Location));

            if (!monsters.Any())
                return null;

            var closestMonster = monsters.First();

            if (closestMonster.Location.IsInRange(_Player.Location, 1))
                return Turn.Attack(closestMonster.Location - _Player.Location);

            return _Navigator.GoTo(closestMonster.Location);
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
            return Turn.Step((StepDirection)_LevelView.Random.Next(4));
        }

        private Turn GoToClosestHealthPack()
        {
            var packs = _LevelView.HealthPacks.OrderBy(p => _Player.Location.CalculateDistance(p.Location));

            if (!packs.Any())
                return null;

            if (_BeingCareful)
                return _Navigator.GoToClosest(packs.Take(3).Select(p => p.Location));

            return _Navigator.GoTo(packs.First().Location);
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

            for (int x = 0; x < _LevelWidth; x += 10)
            {
                _UnexploredLocations.Add(new Location(x, _LevelHeight - 1));

                for (int y = 0; y < _LevelHeight; y += 10)
                {
                    _UnexploredLocations.Add(new Location(x, y));
                }
            }

            for (int y = 0; y < _LevelHeight; y += 10)
                _UnexploredLocations.Add(new Location(_LevelWidth - 1, y));
        }

        private void CheckInLocations()
        {
            _UnexploredLocations.RemoveAll(l => _LevelView.Field[l] != CellType.Hidden);
        }
    }
}
