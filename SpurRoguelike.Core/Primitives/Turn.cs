using System;
using SpurRoguelike.Core.Entities;

namespace SpurRoguelike.Core.Primitives
{
    public enum TurnType
    {
        None,
        StepWest,
        StepNorth,
        StepEast,
        StepSouth,
        AttackWest,
        AttackWN,
        AttackNorth,
        AttackNE,
        AttackEast,
        AttackES,
        AttackSouth,
        AttackSW
    }
    
    public class Turn
    {
        public TurnType Type { get; }
        
        private Turn(Action<Player> action, TurnType type)
        {
            this.action = action;
            Type = type;
        }

        public static Turn Step(StepDirection direction)
        {
            TurnType type;

            switch (direction)
            {
                case StepDirection.North:
                    type = TurnType.StepNorth;
                    break;
                case StepDirection.East:
                    type = TurnType.StepEast;
                    break;
                case StepDirection.South:
                    type = TurnType.StepSouth;
                    break;
                case StepDirection.West:
                    type = TurnType.StepWest;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
            
            return new Turn(player =>
                {
                    player.Move(player.Location + Offset.FromDirection(direction), player.Level);
                },
                type
                );
        }

        public static Turn Step(Offset offset)
        {
            TurnType type;
            
            var stepOffset2 = offset.SnapToStep();
            if (stepOffset2.XOffset < 0 && stepOffset2.YOffset == 0)
                type = TurnType.StepWest;
            else if (stepOffset2.XOffset == 0 && stepOffset2.YOffset < 0)
                type = TurnType.StepNorth;
            else if (stepOffset2.XOffset > 0 && stepOffset2.YOffset == 0)
                type = TurnType.StepEast;
            else if (stepOffset2.XOffset == 0 && stepOffset2.YOffset > 0)
                type = TurnType.StepSouth;
            else
                throw new ArgumentOutOfRangeException();
            
            return new Turn(player =>
                {
                    var stepOffset = offset.SnapToStep();
    
                    if (stepOffset.XOffset != 0 || stepOffset.YOffset != 0)
                        player.Move(player.Location + stepOffset, player.Level);
                },
                type
                );
        }

        public static Turn Attack(AttackDirection direction)
        {
            TurnType type;

            switch (direction)
            {
                case AttackDirection.North:
                    type = TurnType.AttackNorth;
                    break;
                case AttackDirection.NorthEast:
                    type = TurnType.AttackNE;
                    break;
                case AttackDirection.East:
                    type = TurnType.AttackEast;
                    break;
                case AttackDirection.SouthEast:
                    type = TurnType.AttackES;
                    break;
                case AttackDirection.South:
                    type = TurnType.AttackSouth;
                    break;
                case AttackDirection.SouthWest:
                    type = TurnType.AttackSW;
                    break;
                case AttackDirection.West:
                    type = TurnType.AttackWest;
                    break;
                case AttackDirection.NorthWest:
                    type = TurnType.AttackWN;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
            
            return new Turn(player =>
                {
                    var targetLocation = player.Location + Offset.FromDirection(direction);
    
                    var target = player.Level.GetEntity<Monster>(targetLocation);
    
                    if (target == null)
                        return;
    
                    player.PerformAttack(target);
                },
                type
                );
        }
        public static Turn Attack(Offset offset)
        {
            TurnType type;

            if (offset.XOffset < 0 && offset.YOffset == 0)
                type = TurnType.AttackWest;
            else if (offset.XOffset < 0 && offset.YOffset < 0)
                type = TurnType.AttackWN;
            else if (offset.XOffset == 0 && offset.YOffset < 0)
                type = TurnType.AttackNorth;
            else if (offset.XOffset > 0 && offset.YOffset < 0)
                type = TurnType.AttackNE;
            else if (offset.XOffset > 0 && offset.YOffset == 0)
                type = TurnType.AttackEast;
            else if (offset.XOffset > 0 && offset.YOffset > 0)
                type = TurnType.AttackES;
            else if (offset.XOffset == 0 && offset.YOffset > 0)
                type = TurnType.AttackSouth;
            else if (offset.XOffset < 0 && offset.YOffset > 0)
                type = TurnType.AttackSW;
            else
                throw new ArgumentOutOfRangeException();
            
            return new Turn(player =>
                {
                    var attackOffset = offset.Normalize();
    
                    if (attackOffset.XOffset == 0 && attackOffset.YOffset == 0)
                        return;
    
                    var targetLocation = player.Location + attackOffset;
    
                    var target = player.Level.GetEntity<Monster>(targetLocation);
    
                    if (target == null)
                        return;
    
                    player.PerformAttack(target);
                },
                type
                );
        }

        public void Apply(Player player)
        {
            action(player);
        }

        public static readonly Turn None = new Turn(player => { }, TurnType.None);

        private readonly Action<Player> action;
    }
}