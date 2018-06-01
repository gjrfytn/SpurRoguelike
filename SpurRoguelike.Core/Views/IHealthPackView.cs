using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.Core.Views
{
    public interface IHealthPackView : IView
    {
        Location Location { get; }
    }
}