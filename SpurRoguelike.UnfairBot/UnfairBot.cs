using SpurRoguelike.Core;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;
using System.Linq;
using System.Reflection;

namespace SpurRoguelike.UnfairBot
{
    public class UnfairBot : PlayerBot.PlayerBot
    {
        FieldInfo _LevelField;
        FieldInfo _ExitField;

        public UnfairBot()
        {
            RearrangeBehaviours();

            System.Type levelViewType = typeof(LevelView);
            _LevelField = levelViewType.GetField("level", BindingFlags.NonPublic | BindingFlags.Instance);
            _ExitField = GetType().BaseType.GetField("_Exit", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        protected override void UnfairBotBackdoor(ILevelView levelView)
        {
            Level level = (Level)_LevelField.GetValue(levelView);
            level.Player.Upgrade(200, 200);
            _ExitField.SetValue(this, level.Field.GetCellsOfType(CellType.Exit).Single());
        }

        private void RearrangeBehaviours()
        {
            FieldInfo behavioursField;
            System.Type thisType = GetType().BaseType;
            behavioursField = thisType.GetField("_Behaviours", BindingFlags.NonPublic | BindingFlags.Instance);
            System.Func<Turn>[] behaviours = (System.Func<Turn>[])behavioursField.GetValue(this);

            MethodInfo grindMonstersMethod;
            grindMonstersMethod = thisType.GetMethod("GrindMonsters", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo goToExitMethod;
            goToExitMethod = thisType.GetMethod("GoToExit", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo panicMethod;
            panicMethod = thisType.GetMethod("Panic", BindingFlags.NonPublic | BindingFlags.Instance);

            behavioursField.SetValue(
                this,
                new System.Func<Turn>[]
                {
                    (System.Func<Turn>)grindMonstersMethod.CreateDelegate(typeof(System.Func<Turn>), this),
                    (System.Func<Turn>)goToExitMethod.CreateDelegate(typeof(System.Func<Turn>), this),
                    (System.Func<Turn>)panicMethod.CreateDelegate(typeof(System.Func<Turn>), this)
                });
        }
    }
}