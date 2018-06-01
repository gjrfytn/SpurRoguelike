using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.Core
{
    public interface IPlayerController
    {
        Turn MakeTurn(ILevelView levelView, IMessageReporter messageReporter);
    }
}