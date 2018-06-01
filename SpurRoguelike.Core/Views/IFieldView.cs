using System.Collections.Generic;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.Core.Views
{
    public interface IFieldView : IView
    {
        CellType this[Location index] { get; }

        int Width { get; }

        int Height { get; }

        int VisibilityHeight { get; }

        int VisibilityWidth { get; }

        bool Contains(Location location);

        IEnumerable<Location> GetCellsOfType(CellType type);
    }
}