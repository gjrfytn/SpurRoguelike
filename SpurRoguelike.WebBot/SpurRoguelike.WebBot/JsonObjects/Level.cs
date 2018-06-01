namespace SpurRoguelike.WebBot.JsonObjects
{
    public class Level
    {
        public Field Field { get; set; }
        public Pawn Player { get; set; }
        public Pawn[] Monsters { get; set; }
        public Item[] Items { get; set; }
        public HealthPack[] HealthPacks { get; set; }
    }
}