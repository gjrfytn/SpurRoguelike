using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot.Extensions
{
    public static class LocationExtension
    {
        public static int CalculateDistance(this Location location1, Location location2)
        {
            return System.Math.Abs(location1.X - location2.X) + System.Math.Abs(location1.Y - location2.Y);
        }

        public static bool IsInStepRange(this Location location1, Location location2)
        {
            return (location1 - location2).IsStep();
        }
    }
}
