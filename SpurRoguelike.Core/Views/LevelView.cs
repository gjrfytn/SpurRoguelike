using System;
using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Entities;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.Core.Views
{
    public class LevelView : IView
    {
        public LevelView(Level level)
        {
            this.level = level;
        }

        public FieldView Field => level?.Field?.CreateView(level?.Player?.Location) ?? default(FieldView);

        public PawnView Player => level?.Player?.CreateView() ?? default(PawnView);

        public IEnumerable<PawnView> Monsters => level?.Monsters.Where(m => IsVisible(m.Location)).Select(m => m.CreateView());

        public IEnumerable<ItemView> Items => level?.Items.Where(m => IsVisible(m.Location)).Select(i => i.CreateView());

        public IEnumerable<HealthPackView> HealthPacks => level?.HealthPacks.Where(hp => IsVisible(hp.Location)).Select(hp => hp.CreateView());

        public Random Random => level?.Random;

        public bool HasValue => level != null;

        public PawnView GetMonsterAt(Location location)
        {
            if (!IsVisible(location))
                return default(PawnView);
            return level?.GetEntity<Monster>(location)?.CreateView() ?? default(PawnView);
        }

        public ItemView GetItemAt(Location location)
        {
            if (!IsVisible(location))
                return default(ItemView);
            return level?.GetEntity<Item>(location)?.CreateView() ?? default(ItemView);
        }

        public HealthPackView GetHealthPackAt(Location location)
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