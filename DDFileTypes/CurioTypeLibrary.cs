using DarkestDungeonRandomizer.DDTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer.DDFileTypes
{
    public class CurioTypeLibrary
    {
        public IReadOnlyList<Curio> Curios { get; set; }
        
        public CurioTypeLibrary(IReadOnlyList<Curio> curios)
        {
            Curios = curios;
        }

        public static CurioTypeLibrary LoadFromFile(string filename)
        {
            using var file = File.OpenText(filename);
            return Load(file.ReadToEnd());
        }
        public static async Task<CurioTypeLibrary> LoadFromFileAsync(string filename)
        {
            using var file = File.OpenText(filename);
            return Load(await file.ReadToEndAsync());
        }

        public static CurioTypeLibrary Load(string text)
        {
            var curios = new List<Curio>();
            var lines = text.Split(new[] { "\n" , "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            for (int line = 0; line < lines.Length; line++)
            {
                if (lines[line].StartsWith(",,")) continue; // Not the start of a curio entry
                var row1 = lines[line].Split(',')[1..];
                var id = row1[0].TryParseInt();
                // Assertion -- ID must exist
                if (!id.HasValue)
                {
                    throw new Exception("Corrupt Curio Type Library File");
                }
                var name = row1[1];
                CurioQuality quality = Enum.Parse<CurioQuality>(row1[3], true);

                line += 2;
                var row2 = lines[line].Split(',')[2..];
                var idName = row2[0];
                var nothing = ParseWeightedCurioEffect(row2.AsSpan()[2..]);

                line += 1;
                var row3 = lines[line].Split(',')[2..];
                var loot = ParseWeightedCurioEffect(row3.AsSpan()[2..]);

                line += 1;
                var row4 = lines[line].Split(',')[2..];
                Region region = row4[0] switch {
                    "Darkest Dungeon" => Region.DarkestDungeon,
                    string x => Enum.Parse<Region>(x, true)
                };
                var quirk = ParseWeightedCurioEffect(row4.AsSpan()[2..]);

                line += 1;
                var row5 = lines[line].Split(',')[2..];
                var effect = ParseWeightedCurioEffect(row5.AsSpan()[2..]);

                line += 1;
                var row6 = lines[line].Split(',')[2..];
                var full = row6[0] switch
                {
                    "Yes" => true,
                    "No" => false,
                    _ => true
                };
                var purge = ParseWeightedCurioEffect(row6.AsSpan()[2..]);

                line += 1;
                var row7 = lines[line].Split(',')[2..];
                var scouting = ParseWeightedCurioEffect(row7.AsSpan()[2..]);

                line += 1;
                var row8 = lines[line].Split(',')[2..];
                var tags = new string[] { row8[0], row8[1], "", "", "", "" };
                var teleport = ParseWeightedCurioEffect(row8.AsSpan()[2..]);

                line += 1;
                var row9 = lines[line].Split(',')[2..];
                tags[2] = row9[0]; tags[3] = row9[1];
                var disease = ParseWeightedCurioEffect(row9.AsSpan()[2..]);

                line += 1;
                var row10 = lines[line].Split(',')[2..];
                tags[4] = row10[0]; tags[5] = row10[1];

                line += 1;
                var itemInteractions = new List<(string, CurioEffect)>();
                while (lines.Length > line + 1 && !lines[line + 1].StartsWith(",,,,,")) // No item field
                {
                    line += 1;
                    itemInteractions.Add(ParseItemInteraction(lines[line].Split(',').AsSpan()[4..]));
                }
                curios.Add(new Curio(id.Value, name, idName, quality, region, full)
                {
                    Effects = new CurioEffectList()
                    {
                        Nothing = nothing,
                        Loot = loot,
                        Quirk = quirk,
                        Effect = effect,
                        Purge = purge,
                        Scouting = scouting,
                        Teleport = teleport,
                        Disease = disease
                    },
                    Tags = tags,
                    ItemIteractions = itemInteractions.ToArray()
                });
            }
            return new CurioTypeLibrary(curios);
        }

        private static WeightedCurioEffect ParseWeightedCurioEffect(ReadOnlySpan<string> cells)
        {
            return new WeightedCurioEffect(Enum.Parse<CurioEffectType>(cells[0], true))
            {
                Weight = cells[1].TryParseInt() ?? 0,
                Result1 = cells[3],
                Result1Weight = cells[4].TryParseInt(),
                Result2 = cells[6],
                Result2Weight = cells[7].TryParseInt(),
                Result3 = cells[9],
                Result3Weight = cells[10].TryParseInt(),
                CurioTrackerId = cells[12] switch {
                    "nothing" => CurioTracker.Nothing,
                    "loot" => CurioTracker.Loot,
                    "heal_gen" => CurioTracker.HealGeneral,
                    "purge_neg" => CurioTracker.PurgeNegative,
                    "buff" => CurioTracker.Buff,
                    "debuff" => CurioTracker.Debuff,
                    "quirk_pos" => CurioTracker.QuirkPositive,
                    "quirk_neg" => CurioTracker.QuirkNegative,
                    "summon" => CurioTracker.Summon,
                    _ => null
                },
                Notes = cells[13]
            };
        }

        private static (string, CurioEffect) ParseItemInteraction(ReadOnlySpan<string> cells)
        {
            return (cells[0], new CurioEffect(Enum.Parse<CurioEffectType>(cells[1], true))
            {
                Result1 = cells[3],
                Result1Weight = cells[4].TryParseInt(),
                Result2 = cells[6],
                Result2Weight = cells[7].TryParseInt(),
                Result3 = cells[9],
                Result3Weight = cells[10].TryParseInt(),
                CurioTrackerId = cells[12] switch
                {
                    "nothing" => CurioTracker.Nothing,
                    "loot" => CurioTracker.Loot,
                    "heal_gen" => CurioTracker.HealGeneral,
                    "purge_neg" => CurioTracker.PurgeNegative,
                    "buff" => CurioTracker.Buff,
                    "debuff" => CurioTracker.Debuff,
                    "quirk_pos" => CurioTracker.QuirkPositive,
                    "quirk_neg" => CurioTracker.QuirkNegative,
                    "summon" => CurioTracker.Summon,
                    _ => null
                },
                Notes = cells[13]
            });
        }

        public void WriteToFile(string path)
        {
            File.WriteAllText(path, WriteToString());
        }

        public void WriteToFileAsync(string path)
        {
            File.WriteAllTextAsync(path, WriteToString());
        }

        public string WriteToString()
        {
            StringBuilder fullText = new StringBuilder();
            void addRow(params string[] cells)
            {
                string[] fullRow = new string[19];
                cells.CopyTo(fullRow.AsMemory());
                fullText.Append(string.Join(",", fullRow)).Append(Environment.NewLine);
            }
            string[] makeCurioEffectStrings(CurioEffect? effect)
            {
                return new string[]
                {
                    effect?.Result1 ?? "",
                    effect?.Result1Weight.ToString() ?? "",
                    "",
                    effect?.Result2 ?? "",
                    effect?.Result2Weight.ToString() ?? "",
                    "",
                    effect?.Result3 ?? "",
                    effect?.Result3Weight.ToString() ?? "",
                    "",
                    effect?.CurioTrackerId switch
                    {
                        CurioTracker.Nothing => "nothing",
                        CurioTracker.Loot => "loot",
                        CurioTracker.HealGeneral => "heal_gen",
                        CurioTracker.PurgeNegative => "purge_neg",
                        CurioTracker.Buff => "buff",
                        CurioTracker.Debuff => "debuff",
                        CurioTracker.QuirkPositive => "quirk_pos",
                        CurioTracker.QuirkNegative => "quirk_neg",
                        CurioTracker.Summon => "summon",
                        _ => ""
                    },
                    effect?.Notes ?? ""
                };
            }

            addRow();
            addRow();
            foreach (var curio in Curios.OrderBy(x => x.Id))
            {
                addRow("", curio.Id.ToString(), curio.Name, "", Enum.GetName(typeof(CurioQuality), curio.Quality) ?? "Mixed");
                addRow("", "", "ID STRING", "", "RESULT TYPES", "WEIGHT", "% CHANCE", "RESULT 1", "R1 WEIGHT", "R1 %", "RESULT 2", "R2 WEIGHT", "R2 %", "RESULT 3", "R3 WEIGHT", "R3 %", "CURIO TACKER ID", "STRING", "NOTES");
                addRow("", "", curio.IdString, "", "Nothing", curio.Effects.Nothing.Weight.ToString());
                addRow(new[] { "", "", "REGION FOUND", "", "Loot", curio.Effects.Loot.Weight.ToString(), "" }.Concat(makeCurioEffectStrings(curio.Effects.Loot)).ToArray());
                addRow(new[] { "", "", curio.RegionFound switch
                    {
                        Region.DarkestDungeon => "Darkest Dungeon",
                        Region x => Enum.GetName(typeof(Region), x) ?? ""
                    }, "", "Quirk", curio.Effects.Quirk.Weight.ToString(), "" }.Concat(makeCurioEffectStrings(curio.Effects.Quirk)).ToArray());
                addRow(new[] { "", "", "FULL CURIO?", "", "Effect", curio.Effects.Effect.Weight.ToString(), "" }.Concat(makeCurioEffectStrings(curio.Effects.Effect)).ToArray());
                addRow(new[] { "", "", curio.FullCurio ? "Yes" : "No", "", "Purge", curio.Effects.Purge.Weight.ToString(), "" }.Concat(makeCurioEffectStrings(curio.Effects.Purge)).ToArray());
                addRow(new[] { "", "", "TAGS", "", "Scouting", curio.Effects.Scouting.Weight.ToString(), "" }.Concat(makeCurioEffectStrings(curio.Effects.Scouting)).ToArray());
                addRow(new[] { "", "", curio.Tags[0], curio.Tags[1], "Teleport", curio.Effects.Teleport.Weight.ToString(), "" }.Concat(makeCurioEffectStrings(curio.Effects.Teleport)).ToArray());
                addRow(new[] { "", "", curio.Tags[2], curio.Tags[3], "Disease", curio.Effects.Disease.Weight.ToString(), "" }.Concat(makeCurioEffectStrings(curio.Effects.Disease)).ToArray());
                addRow("", "", curio.Tags[4], curio.Tags[5]);
                addRow("", "", "Item Interactions", "", "ITEM", "RESULT TYPE", "", "RESULT 1", "R1 WEIGHT", "R1 %", "RESULT 2", "R2 WEIGHT", "R2 %", "RESULT 3", "R3 WEIGHT", "R3 %", "CURIO TACKER ID", "STRING", "NOTES");
                for (int interactionIndex = 0; interactionIndex < 4; interactionIndex++)
                {
                    if (interactionIndex < curio.ItemIteractions.Count)
                    {
                        addRow(new[]
                        {
                            "", "", "", "",
                            curio.ItemIteractions[interactionIndex].item,
                            Enum.GetName(typeof(CurioEffectType), curio.ItemIteractions[interactionIndex].effect.Type) ?? "",
                            ""
                        }.Concat(makeCurioEffectStrings(curio.ItemIteractions[interactionIndex].effect)).ToArray());
                    }
                    else
                    {
                        addRow();
                    }
                }
            }

            return fullText.ToString();
        }
    }
}
