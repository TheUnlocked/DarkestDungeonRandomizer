using DarkestDungeonRandomizer.DDFileTypes;
using DarkestDungeonRandomizer.DDTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer
{
    public class CurioShuffler
    {
        private readonly MainViewModel model;
        private readonly Random random;

        public CurioShuffler(MainViewModel model, Random random)
        {
            this.model = model;
            this.random = random;
        }

        public void Randomize()
        {
            var curiosFolder = model.ModDirectory.CreateSubdirectory("curios");
            var curios = CurioTypeLibrary.LoadFromFile(model.GetGameDataPath(Path.Combine("curios", "curio_type_library.csv")));
            var shuffledCurios = ShuffleCurioTypeLibrary(curios);
            var dungeonCurioPropsFiles = new[]
            {
                (Region.Cove, new[] { "dungeons", "cove", "cove.props.darkest" }),
                (Region.Ruins, new[] { "dungeons", "crypts", "crypts.props.darkest" }),
                (Region.Warrens, new[] { "dungeons", "warrens", "warrens.props.darkest" }),
                (Region.Weald, new[] { "dungeons", "weald", "weald.props.darkest" }),
                (Region.DarkestDungeon, new[] { "dungeons", "darkestdungeon", "darkestdungeon.props.darkest" }),
                (Region.Town, new[] { "dungeons", "town", "town.props.darkest" })
            };
            var syncedDungeonCurioProps = AlignDungeonPropsToCurioRegions(
                dungeonCurioPropsFiles
                    .Select(x => (x.Item1, Darkest.LoadFromFile(model.GetGameDataPath(Path.Combine(x.Item2)))))
                    .ToArray(),
                shuffledCurios);

            var dungeonsFolder = model.ModDirectory.CreateSubdirectory("dungeons");
            dungeonCurioPropsFiles.Zip(syncedDungeonCurioProps, (i, r) => {
                var dungeonFolder = dungeonsFolder.CreateSubdirectory(i.Item2[1]);
                r.Item2.WriteToFile(Path.Combine(dungeonFolder.FullName, i.Item2[2]));
                return 0;
            }).ToArray(); // ToArray to get side effects to compute.

            shuffledCurios.WriteToFile(Path.Combine(curiosFolder.FullName, "curio_type_library.csv"));
        }

        private CurioTypeLibrary ShuffleCurioTypeLibrary(CurioTypeLibrary curioTypeLibrary)
        {
            var curios = curioTypeLibrary.Curios;
            if (!model.IncludeShamblerAltar)
            {
                curios = curioTypeLibrary.Curios.Where(x => x.IdString != "shamblers_altar").ToArray();
            }
            if (!model.IncludeStoryCurios)
            {
                curios = curioTypeLibrary.Curios.Where(x => x.FullCurio).ToArray();
            }

            var regions = curioTypeLibrary.Curios.Select(x => x.RegionFound).ToArray();
            var curioEffectLists = curioTypeLibrary.Curios.Select(x => x.Effects);
            if (model.RandomizeCurioRegions)
            {
                regions = regions.Shuffle(random);
            }
            var nothingEffects = new WeightedCurioEffect[curioEffectLists.Count()];
            var lootEffects = new WeightedCurioEffect[curioEffectLists.Count()];
            var quirkEffects = new WeightedCurioEffect[curioEffectLists.Count()];
            var effectEffects = new WeightedCurioEffect[curioEffectLists.Count()];
            var purgeEffects = new WeightedCurioEffect[curioEffectLists.Count()];
            var scoutingEffects = new WeightedCurioEffect[curioEffectLists.Count()];
            var teleportEffects = new WeightedCurioEffect[curioEffectLists.Count()];
            var diseaseEffects = new WeightedCurioEffect[curioEffectLists.Count()];
            {
                int i = 0;
                foreach (var curioEffectList in curioEffectLists)
                {
                    nothingEffects[i] = curioEffectList.Nothing;
                    lootEffects[i] = curioEffectList.Loot;
                    quirkEffects[i] = curioEffectList.Quirk;
                    effectEffects[i] = curioEffectList.Effect;
                    purgeEffects[i] = curioEffectList.Purge;
                    scoutingEffects[i] = curioEffectList.Scouting;
                    teleportEffects[i] = curioEffectList.Teleport;
                    diseaseEffects[i] = curioEffectList.Disease;
                    i++;
                }
            }
            if (model.RandomizeCurioEffects)
            {
                nothingEffects = nothingEffects.Shuffle(random);
                lootEffects = lootEffects.Shuffle(random);
                quirkEffects = quirkEffects.Shuffle(random);
                effectEffects = effectEffects.Shuffle(random);
                purgeEffects = purgeEffects.Shuffle(random);
                scoutingEffects = scoutingEffects.Shuffle(random);
                teleportEffects = teleportEffects.Shuffle(random);
                diseaseEffects = diseaseEffects.Shuffle(random);
            }
            
            Dictionary<string, CurioEffect?[]> itemInteractionsByItem = new Dictionary<string, CurioEffect?[]>();
            for (int i = 0; i < curios.Count; i++)
            {
                foreach (var (item, effect) in curios[i].ItemIteractions)
                {
                    if (!itemInteractionsByItem.ContainsKey(item))
                    {
                        itemInteractionsByItem[item] = new CurioEffect?[curios.Count];
                    }
                    itemInteractionsByItem[item][i] = effect;
                }
            }
            if (model.RandomizeCurioInteractions)
            {
                foreach (var pair in itemInteractionsByItem)
                {
                    itemInteractionsByItem[pair.Key] = pair.Value.Shuffle(random);
                }
            }
            List<(string, CurioEffect)>[] itemInteractionByCurio = new List<(string, CurioEffect)>[curios.Count];
            for (int i = 0; i < curios.Count; i++)
            {
                itemInteractionByCurio[i] = new List<(string, CurioEffect)>();
                foreach (var pair in itemInteractionsByItem)
                {
                    if (pair.Value[i] != null)
                    {
                        itemInteractionByCurio[i].Add((pair.Key, pair.Value[i]!));
                    }
                }
            }

            curios = curios.Select((c, i) => c with
            {
                RegionFound = regions[i],
                Effects = new CurioEffectList
                {
                    Nothing = nothingEffects[i],
                    Loot = lootEffects[i],
                    Quirk = quirkEffects[i],
                    Effect = effectEffects[i],
                    Purge = purgeEffects[i],
                    Scouting = scoutingEffects[i],
                    Teleport = teleportEffects[i],
                    Disease = diseaseEffects[i]
                },
                ItemIteractions = itemInteractionByCurio[i]
            }).ToArray();

            if (!model.IncludeShamblerAltar)
            {
                curios = curios.Concat(new[] { curios.First(x => x.IdString != "shamblers_altar") }).ToArray();
            }
            if (!model.IncludeStoryCurios)
            {
                curios = curios.Concat(curioTypeLibrary.Curios.Where(x => !x.FullCurio)).ToArray();
            }

            return new CurioTypeLibrary(curios);
        }

        private (Region, Darkest)[] AlignDungeonPropsToCurioRegions((Region, Darkest)[] regionPropFiles, CurioTypeLibrary curioTypeLibrary)
        {
            if (!model.RandomizeCurioRegions)
            {
                return regionPropFiles;
            }
            var newFiles = new (Region, Darkest)[regionPropFiles.Length];
            for (int i = 0; i < regionPropFiles.Length; i++)
            {
                var (region, file) = regionPropFiles[i];
                var newDict = file.Entries.ToDictionary(p => p.Key, p => new List<Darkest.DarkestEntry>(p.Value));

                var availableCurios = curioTypeLibrary.Curios
                    .Where(x => x.RegionFound == region || x.RegionFound == Region.All)
                    .ToArray()
                    .Shuffle(random);
                var nonTreasures = availableCurios.Where(x => !x.Tags.Contains("Treasure"));
                var treasures = availableCurios.Where(x => x.Tags.Contains("Treasure"));
                var hallCurios = nonTreasures.Skip(6).Take(random.Next(8, 13));
                var roomCurios = nonTreasures.Take(6);
                var roomTreasures = treasures.Take(random.Next(4, 8));

                newDict["hall_curios"] = hallCurios
                    .Select(x => new Darkest.DarkestEntry("hall_curios", new Dictionary<string, IReadOnlyList<string>>()
                    {
                        { "chance", new[] { random.Next(1, 11).ToString() } },
                        { "types", new[] { x.IdString } }
                    }))
                    .ToList();
                if (!model.IncludeShamblerAltar)
                {
                    newDict["hall_curios"].Add(new Darkest.DarkestEntry("hall_curios", new Dictionary<string, IReadOnlyList<string>>() {
                        { "chance", new[] { "1" } },
                        { "types", new[] { "shamblers_altar" } }
                    }));
                }
                newDict["room_curios"] = roomCurios
                    .Select(x => new Darkest.DarkestEntry("room_curios", new Dictionary<string, IReadOnlyList<string>>()
                    {
                        { "chance", new[] { random.Next(1, 11).ToString() } },
                        { "types", new[] { x.IdString } }
                    }))
                    .ToList();
                newDict["room_treasures"] = roomCurios
                    .Select(x => new Darkest.DarkestEntry("room_curios", new Dictionary<string, IReadOnlyList<string>>()
                    {
                        { "chance", new[] { random.Next(1, 11).ToString() } },
                        { "types", new[] { x.IdString } }
                    }))
                    .ToList();

                newFiles[i] = (region, new Darkest(newDict.ToImmutableDictionary(p => p.Key, p => (IReadOnlyList<Darkest.DarkestEntry>)p.Value)));
            }
            return newFiles;
        }
    }
}
