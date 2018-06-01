//#define LOG

using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.WebBot.JsonObjects;

namespace SpurRoguelike.WebBot
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            while (true)
            {
                PlayerBot.PlayerBot bot = new PlayerBot.PlayerBot();

#if LOG
                Console.WriteLine("Starting game...");     
#endif

                WebRequest startRequest = WebRequest.Create("http://e03078:666/api/spur/start/");
                startRequest.Method = "POST";
                startRequest.Headers.Add("X-Spur-Token", "alugfoup");
                startRequest.ContentLength = 0;

                using (HttpWebResponse response = (HttpWebResponse) startRequest.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.Created)
                        throw new Exception("Bad status.");

#if LOG
                    Console.WriteLine("Done...");
#endif
                }

                while (true)
                {
#if LOG
                Console.WriteLine("Quering level...");     
#endif


                    WebRequest request = WebRequest.Create("http://e03078:666/api/spur/view/");
                    request.Method = "GET";
                    request.Headers.Add("X-Spur-Token", "alugfoup");

                    //using (Stream requestStream= request.GetRequestStream())
                    //{
                    string data;
                    using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
                    {

                        if (response.StatusCode != HttpStatusCode.OK)
                            throw new Exception("Bad status.");

                        using (Stream responseStreamstream = response.GetResponseStream())
                        {
                            using (StreamReader reader = new StreamReader(responseStreamstream))
                            {
                                data = reader.ReadToEnd();
                            }
                        }
                    }
                    // }

#if LOG
                Console.WriteLine("Done.");
                
                Console.WriteLine("Serializing level...");
#endif

                    Data jsonData = JsonConvert.DeserializeObject<Data>(data);

                    if (jsonData.Level.Player.IsDestroyed)
                        break;

                    Console.Clear();
                    foreach (string row in jsonData.Render)
                    Console.WriteLine(row);

                    LevelView levelView = new LevelView(jsonData);

#if LOG
                Console.WriteLine("Done.");
                
                Console.WriteLine("Making turn...");
#endif

                    Turn turn = bot.MakeTurn(levelView, new MessageReporter());

#if LOG
                Console.WriteLine("Done.");
                
                Console.WriteLine("Sending turn...");
#endif

                    request = WebRequest.Create("http://e03078:666/api/spur/" + MakeTurnUrl(turn));
                    request.Method = "POST";
                    request.Headers.Add("X-Spur-Token", "alugfoup");
                    request.ContentLength = 0;

                    using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                            throw new Exception("Bad status.");

#if LOG
                    Console.WriteLine("Done...");
#endif
                    }
                }
            }

            Console.WriteLine("TERMINATED.");
            Console.ReadKey();
        }

        private static string MakeTurnUrl(Turn turn)
        {
            switch (turn.Type)
            {
                case TurnType.None:
                    return "none";
                case TurnType.StepWest:
                    return "step/west";
                case TurnType.StepNorth:
                    return "step/north";
                case TurnType.StepEast:
                    return "step/east";
                case TurnType.StepSouth:
                    return "step/south";
                case TurnType.AttackWest:
                    return "attack/west";
                case TurnType.AttackWN:
                    return "attack/northwest";
                case TurnType.AttackNorth:
                    return "attack/north";
                case TurnType.AttackNE:
                    return "attack/northeast";
                case TurnType.AttackEast:
                    return "attack/east";
                case TurnType.AttackES:
                    return "attack/southeast";
                case TurnType.AttackSouth:
                    return "attack/south";
                case TurnType.AttackSW:
                    return "attack/southwest";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}