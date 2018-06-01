using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.WebBot.JsonObjects
{
    public class Item : IItemView
    {
        public int AttackBonus { get; set; }
        public int DefenceBonus { get; set; }
        public Location Location { get; set; }
        public bool HasValue { get; set; } = true;
    }
}