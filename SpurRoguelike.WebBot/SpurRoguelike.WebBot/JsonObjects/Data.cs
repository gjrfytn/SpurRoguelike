using Newtonsoft.Json;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.WebBot.JsonObjects
{
    public class Data
    {
        [JsonProperty("levelData")]
        public Level Level { get; set; }
        public Location NorthWestCorner { get; set; }
        public string[] Render { get; set; }
    }
}