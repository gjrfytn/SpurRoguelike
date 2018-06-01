using System;
using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;
using SpurRoguelike.WebBot.JsonObjects;

namespace SpurRoguelike.WebBot
{
    public class FieldView : IFieldView
    {
        private readonly Data _Data;
        private readonly CellType[,] _Field;

        public FieldView(Data data)
        {
            _Data = data;
            
            _Field=new CellType[_Data.Level.Field.Width,_Data.Level.Field.Height];
            
            for (int y = 0; y < _Field.GetLength(1); ++y)
            {
                for (int x = 0; x < _Field.GetLength(0); ++x)
                {
                    _Field[x, y] = CellType.Hidden;
                }
            }
            
            for (int y = 0; y < _Data.Level.Field.VisibilityHeight*2+1; ++y)
            {
                for (int x = 0; x < _Data.Level.Field.VisibilityWidth*2+1; ++x)
                {
                    try
                    {
                    switch (_Data.Render.ElementAt(y)[x])
                    {
                        case ' ':
                        case '+':
                        case '@':
                        case '$':
                            _Field[x+_Data.NorthWestCorner.X, y+_Data.NorthWestCorner.Y]= CellType.Empty;
                            break;
                        case '#':
                            _Field[x+_Data.NorthWestCorner.X, y+_Data.NorthWestCorner.Y]= CellType.Wall;
                            break;
                        case '*':
                            _Field[x+_Data.NorthWestCorner.X, y+_Data.NorthWestCorner.Y]= CellType.Trap;
                            break;
                        case '.':
                            _Field[x+_Data.NorthWestCorner.X, y+_Data.NorthWestCorner.Y]= CellType.PlayerStart;
                            break;
                        case '!':
                            _Field[x+_Data.NorthWestCorner.X, y+_Data.NorthWestCorner.Y]= CellType.Exit;
                            break;
                        default: throw new NotImplementedException();
                    }
                        
                    }
                    catch (Exception e)
                    {
                        
                    }
                }
            }
        }

        public bool HasValue => true;

        public CellType this[Location index]
        {
            get
            {
                if (!Contains(index)/*||
                    index.Y-_Data.NorthWestCorner.Y < 0||
                    index.X-_Data.NorthWestCorner.X < 0||
                     index.Y- _Data.NorthWestCorner.Y > _Data.Level.Field.VisibilityHeight*2||
                     index.X- _Data.NorthWestCorner.X > _Data.Level.Field.VisibilityWidth*2*/)
                    return CellType.Hidden;//TODO

                //try
                //{
                return  _Field[index.X, index.Y];
                //}
                //catch (ArgumentOutOfRangeException e)
                //{
                //return CellType.Hidden;
                //}
                //catch (IndexOutOfRangeException e)
                //{
                //    return CellType.Hidden;
                //}
            }
    }

        public int Width => _Data.Level.Field.Width;
        public int Height => _Data.Level.Field.Height;
        public int VisibilityHeight => _Data.Level.Field.VisibilityHeight;
        public int VisibilityWidth => _Data.Level.Field.VisibilityWidth;
        
        public bool Contains(Location location)
        {
            return location.X >= 0 && location.X < Width && location.Y >= 0 && location.Y < Height;
        }

        public IEnumerable<Location> GetCellsOfType(CellType type)
        {
            for (int i = 0; i < Width; i++)
            for (int j = 0; j < Height; j++)
            {
                var location = new Location(i, j);
                if (this[location] == type)
                    yield return location;
            }
        }
    }
}