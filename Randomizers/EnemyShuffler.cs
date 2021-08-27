using DarkestDungeonRandomizer.DDFileTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer.Randomizers
{
    public class EnemyShuffler : IRandomizer
    {
        private readonly MainViewModel model;
        private readonly Random random;

        public EnemyShuffler(MainViewModel model, Random random)
        {
            this.model = model;
            this.random = random;
        }

        public void Randomize()
        {
            if (model.RandomizeMonsters || model.RandomizeBosses)
            {
                var dungeons = new[] { "cove", "crypts", "warrens", "weald" };
                var levels = new[] { 1, 3, 5 };

                foreach (var dungeon in dungeons)
                {
                    var dungeonsDir = model.ModDirectory.CreateSubdirectory("dungeons");
                    var dungeonDir = dungeonsDir.CreateSubdirectory(dungeon);
                }

                foreach (var level in levels)
                {
                    var dungeonFiles = dungeons.Select(dungeon => Darkest.LoadFromFile(model.GetGameDataPath(Path.Combine("dungeons", dungeon, $"{dungeon}.{level}.mash.darkest"))));
                    if (model.RandomizeMonsters)
                    {
                        var (hallEnemies, roomEnemies, stallEnemies) = GetAllEnemies(dungeonFiles);
                        var hallEnemyReplacements = ShuffleMap(hallEnemies);
                        var roomEnemyReplacements = ShuffleMap(roomEnemies);
                        var stallEnemyReplacements = ShuffleMap(stallEnemies);
                        dungeonFiles = ReplaceEnemies(dungeonFiles, hallEnemyReplacements, roomEnemyReplacements, stallEnemyReplacements);
                    }
                    if (model.RandomizeBosses)
                    {
                        // Exclude shrieker!
                        var bossLayouts = GetAllBossLayouts(dungeonFiles);
                        var shuffledBossLayouts = bossLayouts.Shuffle(random);
                        dungeonFiles = ReplaceBosses(dungeonFiles, shuffledBossLayouts);

                        JObject questTypeFile = JObject.Parse(File.ReadAllText(model.GetGameDataPath(Path.Combine("campaign", "quest", "quest.types.json"))));
                        var bossLayoutConversion = bossLayouts.Zip(shuffledBossLayouts, (original, shuffled) => (original, shuffled));
                        foreach (var goal in questTypeFile["goals"]!) {
                            if (goal?["data"]?["monster_class_ids"] != null)
                            {
                                goal["data"]!["monster_class_ids"] = bossLayoutConversion
                                    .FirstOrDefault(bossLayout => goal["data"]!["monster_class_ids"]!
                                        .Any(questMonster => bossLayout.original.Contains((string)questMonster!)))
                                    switch
                                {
                                    (null, null) => goal["data"]!["monster_class_ids"],
                                    var x => JToken.FromObject(x.shuffled)
                                };
                            }
                        }
                        var campaignDir = model.ModDirectory.CreateSubdirectory("campaign");
                        var questDir = campaignDir.CreateSubdirectory("quest");
                        File.WriteAllText(Path.Combine(questDir.FullName, "quest.types.json"), questTypeFile.ToString());
                    }

                    _ = dungeons.Zip(dungeonFiles, (dungeon, darkest) =>
                    {
                        darkest.WriteToFile(Path.Combine(model.ModDirectory.FullName, "dungeons", dungeon, $"{dungeon}.{level}.mash.darkest"));
                        return 0;
                    }).ToArray();
                }
            }
        }

        private Dictionary<string, string> ShuffleMap(IEnumerable<string> enemyNames)
        {
            var size1 = enemyNames.Where(x => model.Monsters[x].Size == 1);
            var size2 = enemyNames.Where(x => model.Monsters[x].Size == 2);
            var size3 = enemyNames.Where(x => model.Monsters[x].Size == 3);
            var size4 = enemyNames.Where(x => model.Monsters[x].Size == 4);
            return size1.ToArray().Shuffle(random).Zip(size1, (a, b) => (b, a))
                .Concat(size2.ToArray().Shuffle(random).Zip(size2, (a, b) => (b, a)))
                .Concat(size3.ToArray().Shuffle(random).Zip(size3, (a, b) => (b, a)))
                .Concat(size4.ToArray().Shuffle(random).Zip(size4, (a, b) => (b, a)))
                .ToDictionary(p => p.a, p => p.b);
        }

        private (IEnumerable<string> hall, IEnumerable<string> room, IEnumerable<string> stall) GetAllEnemies(IEnumerable<Darkest> files)
        {
            HashSet<string> hallEnemies = new HashSet<string>();
            HashSet<string> roomEnemies = new HashSet<string>();
            HashSet<string> stallEnemies = new HashSet<string>();
            foreach (var file in files)
            {
                foreach (var (entryTag, entries) in file.Entries)
                {
                    switch (entryTag)
                    {
                        case "hall":
                            foreach (var entry in entries)
                            {
                                foreach (var enemy in entry.Properties["types"])
                                {
                                    hallEnemies.Add(enemy);
                                }
                            }
                            break;
                        case "room":
                            foreach (var entry in entries)
                            {
                                foreach (var enemy in entry.Properties["types"])
                                {
                                    roomEnemies.Add(enemy);
                                }
                            }
                            break;
                        case "stall":
                            foreach (var entry in entries)
                            {
                                foreach (var enemy in entry.Properties["types"])
                                {
                                    stallEnemies.Add(enemy);
                                }
                            }
                            break;
                    }
                }
            }
            return (hallEnemies, roomEnemies, stallEnemies);
        }

        private IEnumerable<Darkest> ReplaceEnemies(
            IEnumerable<Darkest> files,
            Dictionary<string, string> hallReplacements,
            Dictionary<string, string> roomReplacements,
            Dictionary<string, string> stallReplacements)
        {
            return files.Select(darkest =>
            {
                var newEntries = darkest.Entries.ToDictionary(p => p.Key, p => p.Value);

                newEntries["hall"] = newEntries["hall"].Select(entry =>
                {
                    var newProps = entry.Properties.ToDictionary(p => p.Key, p => p.Value);
                    newProps["types"] = newProps["types"].Select(x => hallReplacements[x]).ToArray();
                    return entry with { Properties = newProps };
                }).ToImmutableArray();

                newEntries["room"] = newEntries["room"].Select(entry =>
                {
                    var newProps = entry.Properties.ToDictionary(p => p.Key, p => p.Value);
                    newProps["types"] = newProps["types"].Select(x => roomReplacements[x]).ToArray();
                    return entry with { Properties = newProps };
                }).ToImmutableArray();

                newEntries["stall"] = newEntries["stall"].Select(entry =>
                {
                    var newProps = entry.Properties.ToDictionary(p => p.Key, p => p.Value);
                    newProps["types"] = newProps["types"].Select(x => stallReplacements[x]).ToArray();
                    return entry with { Properties = newProps };
                }).ToImmutableArray();

                return darkest with { Entries = newEntries };
            });
        }

        private IEnumerable<IReadOnlyList<string>> GetAllBossLayouts(IEnumerable<Darkest> files)
        {
            List<IReadOnlyList<string>> bossLayouts = new List<IReadOnlyList<string>>();
            foreach (var file in files)
            {
                foreach (var (entryTag, entries) in file.Entries)
                {
                    if (entryTag == "boss")
                    {
                        foreach (var entry in entries)
                        {
                            // No Shrieker
                            if (entry.Properties["types"].Any(x => x.StartsWith("crow")))
                            {
                                continue;
                            }
                            bossLayouts.Add(entry.Properties["types"]);
                        }
                    }
                }
            }
            return bossLayouts;
        }

        private IEnumerable<Darkest> ReplaceBosses(IEnumerable<Darkest> files, IReadOnlyList<string>[] bossLayouts)
        {
            int i = 0;

            return files.Select(darkest =>
            {
                var newEntries = darkest.Entries.ToDictionary(p => p.Key, p => p.Value);
                newEntries["boss"] = newEntries["boss"].Select(entry =>
                {
                    if (entry.Properties["types"].Any(x => x.StartsWith("crow")))
                    {
                        return entry;
                    }
                    var newProps = entry.Properties.ToDictionary(p => p.Key, p => p.Value);
                    newProps["types"] = bossLayouts[i++];
                    return entry with { Properties = newProps };
                }).ToArray();
                return darkest with { Entries = newEntries };
            });
        }
    }
}
