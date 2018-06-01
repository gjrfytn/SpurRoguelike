using System;
using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;
using SpurRoguelike.WebBot.JsonObjects;

namespace SpurRoguelike.WebBot
{
    public class LevelView : ILevelView
    {
        private readonly Data _Data;

        public LevelView(Data data)
        {
            _Data = data;
            Field = new FieldView(_Data);
        }

        public IFieldView Field { get; set; }

        public IPawnView Player => _Data.Level.Player;

        public IEnumerable<IPawnView> Monsters => _Data.Level.Monsters;

        public IEnumerable<IItemView> Items => _Data.Level.Items;

        public IEnumerable<IHealthPackView> HealthPacks => _Data.Level.HealthPacks;

        public Random Random
        {
            get { throw new NotImplementedException(); }
        }

        public bool HasValue => true;

        public IPawnView GetMonsterAt(Location location)
        {
            IPawnView monster=  Monsters.SingleOrDefault(m => m.Location == location);

            if (monster == null)
            {
                Pawn unexistingMonster = new Pawn {HasValue = false};

                monster = unexistingMonster;
            }

            return monster;
        }

        public IItemView GetItemAt(Location location)
        {
            IItemView item= Items.SingleOrDefault(i => i.Location == location);
            
            if (item == null)
            {
                Item unexistingItem = new Item {HasValue = false};

                item= unexistingItem;
            }

            return item;
        }

        public IHealthPackView GetHealthPackAt(Location location)
        {
            IHealthPackView pack=  HealthPacks.SingleOrDefault(p => p.Location == location);
            
            if (pack == null)
            {
                HealthPack unexistingHP = new HealthPack {HasValue = false};

                pack= unexistingHP;
            }

            return pack;
        }
    }
}