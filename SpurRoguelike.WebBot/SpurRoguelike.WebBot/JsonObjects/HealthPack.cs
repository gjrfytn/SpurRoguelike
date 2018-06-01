using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.WebBot.JsonObjects
{
    public class HealthPack:IHealthPackView
    {
        public int Health { get; set; }
        public Location Location { get; set; }
        public bool HasValue { get; set; } = true;
    }
}