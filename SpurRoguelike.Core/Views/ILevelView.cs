using System;
using System.Collections.Generic;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.Core.Views
{
    public interface ILevelView : IView
    {
        IFieldView Field { get; }
        IPawnView Player { get; }
        IEnumerable<IPawnView> Monsters { get; }
        IEnumerable<IItemView> Items { get; }
        IEnumerable<IHealthPackView> HealthPacks { get; }
        Random Random { get; }

        IPawnView GetMonsterAt(Location location);
        IItemView GetItemAt(Location location);
        IHealthPackView GetHealthPackAt(Location location);
    }
}