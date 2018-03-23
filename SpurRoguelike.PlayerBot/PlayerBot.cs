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
        private Location _Exit;

        private BotReporter _Reporter;
        private BotNavigator _Navigator;

        private int _PreviousHealth;
        private bool _BeingCareful;

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

            _Navigator.InitializeLevel(level, _Exit);
        }

        private void InitializeTurn(LevelView level, IMessageReporter messageReporter)
        {
            _LevelView = level;
            _Player = level.Player;

            _BeingCareful = _LevelView.Monsters.Count(m => m.Location.IsInRange(_Player.Location, 5)) >= 6 / (_Player.Health < 50 ? 2 : 1);

            Location exit = _LevelView.Field.GetCellsOfType(CellType.Exit).Single();
            if (_Exit != exit)
            {
                _Exit = exit;
                InitializeLevel(level, messageReporter);
            }

            _Navigator.InitializeTurn(_LevelView, _Player.Location, _BeingCareful);
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

        private Turn GoToExit()
        {
            Turn turn = null;
            if (_Player.Health != _PlayerMaxHealth)
                turn = GoToClosestHealthPack();

            if (turn != null)
                return turn;

            return _Navigator.GoTo(_Exit);
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

        private int CalculateDamage(PawnView attacker, PawnView target, bool maxDamage)
        {
            return (int)(((float)attacker.TotalAttack / target.Defence) * _BaseDamage * (maxDamage ? 1 : 0.95f));
        }
    }
}
