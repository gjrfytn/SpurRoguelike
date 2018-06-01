using System;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.WebBot
{
    public class MessageReporter:IMessageReporter
    {
        public void ReportMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}