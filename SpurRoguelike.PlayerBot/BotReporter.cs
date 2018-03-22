using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    class BotReporter
    {
        private readonly IMessageReporter _MessageReporter;

        public BotReporter(IMessageReporter messageReporter)
        {
            _MessageReporter = messageReporter;
        }

        public void Say(string message)
        {
            _MessageReporter.ReportMessage("[BOT]: " + message);
        }
    }
}
