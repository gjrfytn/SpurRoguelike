using SpurRoguelike.Core.Entities;

namespace SpurRoguelike.Core
{
    public class Engine
    {
        public Engine(string playerName, IPlayerController playerController, Level entryLevel, IRenderer renderer, IEventReporter eventReporter, IPlayerController humanController)
        {
            this.playerName = playerName;
            this.playerController = playerController;
            this.entryLevel = entryLevel;
            this.renderer = renderer;
            this.eventReporter = eventReporter;
            _HumanController = humanController;
        }

        public int GameLoop(bool antifreeze)
        {
            var player = new Player(playerName, 10, 10, 100, 100, playerController, eventReporter);
            entryLevel.Spawn(entryLevel.Field.PlayerStart, player);

            System.DateTime start = System.DateTime.Now;
            while (!player.IsDestroyed)
            {
                renderer.RenderLevel(player.Level);

                //if (player.Level.Number == 4)
                //    player.playerController = _HumanController;

                //if (player.Level.Number == 2)
                //    System.Threading.Thread.Sleep(500);

                player.Level.Tick();

                if (antifreeze && System.DateTime.Now - start > System.TimeSpan.FromMinutes(1.5)) //TODO 1.16
                    return -1;
            }
            if (!antifreeze)
             renderer.RenderGameEnd(player.Level.IsCompleted);

            return player.Level.Number;
        }

        private readonly string playerName;
        private readonly IPlayerController playerController;
        private readonly Level entryLevel;
        private readonly IRenderer renderer;
        private readonly IEventReporter eventReporter;
        private readonly IPlayerController _HumanController;
    }
}