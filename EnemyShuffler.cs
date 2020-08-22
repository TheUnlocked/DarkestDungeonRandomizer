using DarkestDungeonRandomizer.DDFileTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer
{
    public class EnemyShuffler
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
            if (model.RandomizeMonsters)
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
                    var (hallEnemies, roomEnemies) = GetAllEnemies(dungeonFiles);
                    var hallEnemyReplacements = ShuffleMap(hallEnemies);
                    var roomEnemyReplacements = ShuffleMap(roomEnemies);
                    var shuffledDungeonFiles = ReplaceEnemies(dungeonFiles, hallEnemyReplacements, roomEnemyReplacements).ToArray();

                    dungeons.Zip(shuffledDungeonFiles, (dungeon, darkest) =>
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

        private (IEnumerable<string> hall, IEnumerable<string> room) GetAllEnemies(IEnumerable<Darkest> files)
        {
            HashSet<string> hallEnemies = new HashSet<string>();
            HashSet<string> roomEnemies = new HashSet<string>();
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
                    }
                }
            }
            return (hallEnemies, roomEnemies);
        }

        private IEnumerable<Darkest> ReplaceEnemies(
            IEnumerable<Darkest> files,
            Dictionary<string, string> hallReplacements,
            Dictionary<string, string> roomReplacements)
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

                return darkest with { Entries = newEntries };
            });
        }
    }
}
