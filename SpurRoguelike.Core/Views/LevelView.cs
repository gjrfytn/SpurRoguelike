using System;
using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Entities;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.Core.Views
{
    public class LevelView : ILevelView
    {
        public LevelView(Level level)
        {
            this.level = level;
        }

        public IFieldView Field => level?.Field?.CreateView(level?.Player?.Location) ?? default(FieldView);

        public IPawnView Player => level?.Player?.CreateView() ?? default(PawnView);

        public IEnumerable<IPawnView> Monsters => level?.Monsters.Where(m => IsVisible(m.Location)).Select(m => m.CreateView());

        public IEnumerable<IItemView> Items => level?.Items.Where(m => IsVisible(m.Location)).Select(i => i.CreateView());

        public IEnumerable<IHealthPackView> HealthPacks => level?.HealthPacks.Where(hp => IsVisible(hp.Location)).Select(hp => hp.CreateView());

        public Random Random => level?.Random;

        public bool HasValue => level != null;

        public IPawnView GetMonsterAt(Location location)
        {
            if (!IsVisible(location))
                return default(PawnView);
            return level?.GetEntity<Monster>(location)?.CreateView() ?? default(PawnView);
        }

        public IItemView GetItemAt(Location location)
        {
            if (!IsVisible(location))
                return default(ItemView);
            return level?.GetEntity<Item>(location)?.CreateView() ?? default(ItemView);
        }

        public IHealthPackView GetHealthPackAt(Location location)
        {
            if (!IsVisible(location))
                return default(HealthPackView);
            return level?.GetEntity<HealthPack>(location)?.CreateView() ?? default(HealthPackView);
        }

        private bool IsVisible(Location location)
        {
            if (!Player.HasValue)
                return true;
            var offset = (Player.Location - location).Abs();
            return offset.XOffset <= level.Field.VisibilityWidth && offset.YOffset <= level.Field.VisibilityHeight;
        }

        private readonly Level level;
    }
}