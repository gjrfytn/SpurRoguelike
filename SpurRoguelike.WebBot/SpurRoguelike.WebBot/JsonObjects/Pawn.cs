using System;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.WebBot.JsonObjects
{
    public class Pawn : IPawnView
    {
        public string Name { get; set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public int TotalAttack { get; set; }
        public int TotalDefence { get; set; }
        public int Health { get; set; }
        public Item Equipment { get; set; }

        public bool TryGetEquippedItem(out IItemView item)
        {
            item = Equipment;
            
            return item != null;
        }

        public bool IsDestroyed { get; set; }
        public Location Location { get; set; }
        public bool HasValue { get; set; } = true;
    }
}