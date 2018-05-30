using System;
using System.Collections.Generic;
using System.Linq;
using Fclp;
using SpurRoguelike.ConsoleGUI;
using SpurRoguelike.ConsoleGUI.TextScreen;
using SpurRoguelike.Content;
using SpurRoguelike.Core;
using SpurRoguelike.Generators;

namespace SpurRoguelike
{
    internal class EntryPoint
    {
        public static void Main(string[] args)
        {
            var commandLineParser = new FluentCommandLineParser<GameOptions>();

            commandLineParser
                .Setup(options => options.PlayerName)
                .As('p')
                .SetDefault("Player")
                .WithDescription("Player name");

            commandLineParser
                .Setup(options => options.PlayerController)
                .As('c')
                .WithDescription("Path to assembly containing player controller");

            commandLineParser
                .Setup(options => options.Seed)
                .As('s')
                .SetDefault(0)
                .WithDescription("Seed for level generation");

            commandLineParser
                .Setup(options => options.LevelCount)
                .As('n')
                .SetDefault(6)
                .WithDescription("Number of levels to generate");

            bool autotests = args.Contains("-tests");

            commandLineParser
                .SetupHelp("h", "help")
                .WithHeader($"{AppDomain.CurrentDomain.FriendlyName} [-p name] [-c controller] [-s seed] [-n number]")
                .Callback(text => Console.WriteLine(text));

            if (commandLineParser.Parse(args).HelpCalled)
                return;

            if (autotests)
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter("autotest_result_" + DateTime.Now.GetHashCode() + ".txt"))
                {
                    List<int> seeds = new List<int>();

#if true
                    for (int i = 0; i < 50; ++i)
                        seeds.Add(i);
#else
                    seeds.Add(0);
                    seeds.Add(1);
                    seeds.Add(2);
                    seeds.Add(3);
                    seeds.Add(4);
                    seeds.Add(5);
                    seeds.Add(6);
                    seeds.Add(7);
                    seeds.Add(8);
                    seeds.Add(9);
                    seeds.Add(10);
#endif

                    writer.WriteLine("Date:" + DateTime.Now + ", Seeds count: " + seeds.Count);
                    writer.WriteLine("SEED | LEVEL | TIME");
                    List<Tuple<int?, TimeSpan>> results = new List<Tuple<int?, TimeSpan>>();
                    foreach (int seed in seeds)
                    {
                        DateTime start = DateTime.Now;
                        int res = RunGame(commandLineParser.Object, seed, true);
                        TimeSpan elapsed = DateTime.Now - start;
                        writer.WriteLine(seed.ToString().PadRight(4) + " | " + res.ToString().PadRight(5) + " | " + elapsed);
                        results.Add(Tuple.Create(res == -1 ? (int?)null : res, elapsed));
                        writer.Flush();
                    }
                    writer.WriteLine();
                    writer.WriteLine("MED LVL: " +
                        results.Where(r => r.Item1.HasValue).Select(r => r.Item1.Value).GroupBy(k => k, (k, r) => new { k, Count = r.Count() }).OrderByDescending(s => s.Count).First().k +
                        ", AVG LVL: " +
                        results.Where(r => r.Item1.HasValue).Select(r => r.Item1.Value).Average());
                    writer.WriteLine("FREEZED: " + results.Count(r => !r.Item1.HasValue));
                    writer.WriteLine("MIN TIME: " + results.Where(r => r.Item1.HasValue).Min(r => r.Item2));
                    writer.WriteLine("MAX TIME: " + results.Where(r => r.Item1.HasValue).Max(r => r.Item2));
                    writer.WriteLine("AVG TIME (min): " + results.Where(r => r.Item1.HasValue).Average(r => r.Item2.TotalMinutes));
                    writer.WriteLine();
                    writer.WriteLine("Ended: " + DateTime.Now);
                }
            }
            else
            {
                RunGame(commandLineParser.Object, commandLineParser.Object.Seed, false);
            }
        }

        private static int RunGame(GameOptions options, int seed, bool antifreeze)
        {
            var levels = GenerateLevels(seed, options.LevelCount);

            var gui = new ConsoleGui(new TextScreen());

            var playerController = options.PlayerController == null ?
                new ConsolePlayerController(gui) :
                BotLoader.LoadPlayerController(options.PlayerController);

            var engine = new Engine(options.PlayerName, playerController, levels.First(), new ConsoleRenderer(gui), new ConsoleEventReporter(gui), new ConsolePlayerController(gui));

            return engine.GameLoop(antifreeze);
        }

        private static List<Level> GenerateLevels(int seed, int count)
        {
            count = Math.Min(6, Math.Max(2, count));

            var nameGenerator = new NameGenerator(seed);
            var levelGenerator = new LevelGenerator(seed, nameGenerator);
            var monsterClassesGenerator = new MonsterClassesGenerator(seed, nameGenerator);
            var itemClassesGenerator = new ItemClassesGenerator(seed, nameGenerator);

            var itemClasses = itemClassesGenerator.Generate(7,
                new ItemClassOptions { Level = 3, Rarity = 1 },
                new ItemClassOptions { Level = 10, Rarity = 0.15 },
                new ItemClassOptions { Level = 30, Rarity = 0.1 });

            var monsterClasses = monsterClassesGenerator.Generate(5,
                new MonsterClassOptions { Skill = 0.5, Rarity = 0.02, Factory = (name, skill, health, attack, defence) => new Dimwit(name, attack, defence, health, health) },
                new MonsterClassOptions { Skill = 0.6, Rarity = 0.04, Factory = (name, skill, health, attack, defence) => new Dimwit(name, attack, defence, health, health) },
                new MonsterClassOptions { Skill = 0.6, Rarity = 0.06, Factory = (name, skill, health, attack, defence) => new Reptiloid(name, attack, defence, health, health, skill) },
                new MonsterClassOptions { Skill = 0.7, Rarity = 0.1, Factory = (name, skill, health, attack, defence) => new Dimwit(name, attack, defence, health, health) },
                new MonsterClassOptions { Skill = 0.7, Rarity = 0.2, Factory = (name, skill, health, attack, defence) => new Reptiloid(name, attack, defence, health, health, skill) },
                new MonsterClassOptions { Skill = 0.8, Rarity = 1, Factory = (name, skill, health, attack, defence) => new Reptiloid(name, attack, defence, health, health, skill) });

            var levels = new List<Level>();

            var settings = FillDefaultSettings();

            var increaseX = settings.Field.MaxWidth / 3;
            var increaseY = settings.Field.MaxHeight / 3;

            for (int i = 0; i < count - 1; i++)
            {
                levels.Add(levelGenerator.Generate(settings, monsterClasses, itemClasses, i + 1));

                if (i > 0)
                    levels[i - 1].SetNextLevel(levels[i]);

                settings.Monsters.MinSkill += 0.1;
                settings.Monsters.MaxSkill += 0.1;

                settings.Field.MaxWidth += increaseX;
                settings.Field.MinWidth += increaseX;

                settings.Field.MaxHeight += increaseY;
                settings.Field.MinHeight += increaseY;

                settings.Field.FreeSpaceShare -= 0.05;

                if (i == 1)
                    settings.Items.MaxLevel = 100;
            }

            var lastLevelSettigns = FillLastLevelSettings();
            var lastLevelMonsterClasses = monsterClassesGenerator.Generate(1,
                new MonsterClassOptions { Skill = 1.5, Rarity = 1, Factory = (name, skill, health, attack, defence) => new ArenaFighter(name, attack, defence, health, health, skill) });

            var lastLevel = new ArenaGenerator(seed, nameGenerator).Generate(lastLevelSettigns, lastLevelMonsterClasses, itemClasses, levels.Count + 1);

            levels[levels.Count - 1].SetNextLevel(lastLevel);

            levels.Add(lastLevel);

            return levels;
        }

        private static LevelGenerationSettings FillDefaultSettings()
        {
            return new LevelGenerationSettings
            {
                Field = new LevelGenerationSettings.FieldOptions
                {
                    FreeSpaceShare = 0.7,
                    MinWidth = 40,
                    MaxWidth = 50,
                    MinHeight = 35,
                    MaxHeight = 45,
                    MinVisibilityWidth = 22,
                    MaxVisibilityWidth = 30,
                    MaxVisibilityHeight = 16,
                    MinVisibilityHeight = 10
                },
                Monsters = new LevelGenerationSettings.MonsterOptions
                {
                    Density = 0.01,
                    MinSkill = 0.4,
                    MaxSkill = 0.5
                },
                Items = new LevelGenerationSettings.ItemOptions
                {
                    Density = 0.01,
                    MinLevel = 0,
                    MaxLevel = 10
                },
                Traps = new LevelGenerationSettings.TrapOptions
                {
                    Density = 0.05
                },
                HealthPacks = new LevelGenerationSettings.HealthPackOptions
                {
                    Density = 0.02
                }
            };
        }

        private static LevelGenerationSettings FillLastLevelSettings()
        {
            return new LevelGenerationSettings
            {
                Field = new LevelGenerationSettings.FieldOptions
                {
                    FreeSpaceShare = 0.9,
                    MinWidth = 50,
                    MaxWidth = 50,
                    MinHeight = 40,
                    MaxHeight = 40,
                    MinVisibilityWidth = int.MaxValue,
                    MaxVisibilityWidth = int.MaxValue,
                    MaxVisibilityHeight = int.MaxValue,
                    MinVisibilityHeight = int.MaxValue
                },
                Monsters = new LevelGenerationSettings.MonsterOptions
                {
                    MinSkill = 0,
                    MaxSkill = 100
                },
                Items = new LevelGenerationSettings.ItemOptions
                {
                    Density = 0.01,
                    MinLevel = 0,
                    MaxLevel = 100
                },
                Traps = new LevelGenerationSettings.TrapOptions
                {
                    Density = 0.01
                },
                HealthPacks = new LevelGenerationSettings.HealthPackOptions
                {
                    Density = 0.01
                }
            };
        }

        private class GameOptions
        {
            public string PlayerName { get; set; }

            public string PlayerController { get; set; }

            public int Seed { get; set; }

            public int LevelCount { get; set; }
        }
    }
}
