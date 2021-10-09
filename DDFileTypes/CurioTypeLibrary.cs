using DarkestDungeonRandomizer.DDTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer.DDFileTypes;

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
        string[] splitAllowQuotes(string str)
        {
            List<string> result = new();

            string? buffer = null;
            foreach (var part in str.Split(','))
            {
                if (buffer != null)
                {
                    buffer += ',' + part;
                    if (part.EndsWith('"'))
                    {
                        result.Add(buffer);
                        buffer = null;
                    }
                }
                else if (part.StartsWith('"'))
                {
                    buffer = part;
                }
                else
                {
                    result.Add(part);
                }
            }
            return result.ToArray();
        }

        var curios = new List<Curio>();
        var lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToArray();
        for (int line = 0; line < lines.Length; line++)
        {
            if (lines[line].StartsWith(",,")) continue; // Not the start of a curio entry
            var row1 = splitAllowQuotes(lines[line])[1..];
            var id = row1[0].TryParseInt();
            // Assertion -- ID must exist
            if (!id.HasValue)
            {
                throw new Exception("Corrupt Curio Type Library File");
            }
            var name = row1[1];
            CurioQuality quality = Enum.Parse<CurioQuality>(row1[3], true);

            line += 2;
            var row2 = splitAllowQuotes(lines[line])[2..];
            var idName = row2[0];
            var nothing = ParseWeightedCurioEffect(row2.AsSpan()[2..]);

            line += 1;
            var row3 = splitAllowQuotes(lines[line])[2..];
            var loot = ParseWeightedCurioEffect(row3.AsSpan()[2..]);

            line += 1;
            var row4 = splitAllowQuotes(lines[line])[2..];
            Region region = row4[0] switch
            {
                "Darkest Dungeon" => Region.DarkestDungeon,
                string x => Enum.Parse<Region>(x, true)
            };
            var quirk = ParseWeightedCurioEffect(row4.AsSpan()[2..]);

            line += 1;
            var row5 = splitAllowQuotes(lines[line])[2..];
            var effect = ParseWeightedCurioEffect(row5.AsSpan()[2..]);

            line += 1;
            var row6 = splitAllowQuotes(lines[line])[2..];
            var full = row6[0] switch
            {
                "Yes" => true,
                "No" => false,
                _ => true
            };
            var purge = ParseWeightedCurioEffect(row6.AsSpan()[2..]);

            line += 1;
            var row7 = splitAllowQuotes(lines[line])[2..];
            var scouting = ParseWeightedCurioEffect(row7.AsSpan()[2..]);

            line += 1;
            var row8 = splitAllowQuotes(lines[line])[2..];
            var tags = new string[] { row8[0], row8[1], "", "", "", "" };
            var teleport = ParseWeightedCurioEffect(row8.AsSpan()[2..]);

            line += 1;
            var row9 = splitAllowQuotes(lines[line])[2..];
            tags[2] = row9[0]; tags[3] = row9[1];
            var disease = ParseWeightedCurioEffect(row9.AsSpan()[2..]);

            line += 1;
            var row10 = splitAllowQuotes(lines[line])[2..];
            tags[4] = row10[0]; tags[5] = row10[1];

            line += 1;
            var itemInteractions = new List<(string, CurioEffect)>();
            while (lines.Length > line + 1 && !lines[line + 1].StartsWith(",,,,,")) // No item field
            {
                line += 1;
                itemInteractions.Add(ParseItemInteraction(splitAllowQuotes(lines[line]).AsSpan()[4..]));
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
                "stress" => CurioTracker.Stress,
                "heal_stress" => CurioTracker.HealStress,
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
                "stress" => CurioTracker.Stress,
                "heal_stress" => CurioTracker.HealStress,
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
            float totalWeight = (effect?.Result1Weight ?? 0) + (effect?.Result2Weight ?? 0) + (effect?.Result3Weight ?? 0);

            string getWeightText(int? weight)
            {
                if (effect!.Type == CurioEffectType.Loot) return "<- # Draws";
                if (weight == null) return "";
                return $"{weight!.Value / totalWeight * 100:N2}%";
            }

            if (effect?.Type == CurioEffectType.Nothing)
            {
                return Enumerable.Repeat("N/A", 9)
                    .Concat(new[]
                    {
                        "",
                        effect?.Notes ?? ""
                    }).ToArray();
            }

            return new string[]
            {
                effect?.Result1 ?? "",
                effect?.Result1Weight switch { 0 => "", null => "", var x => x.ToString()! },
                getWeightText(effect?.Result1Weight),
                effect?.Result2 ?? "",
                effect?.Result2Weight switch { 0 => "", null => "", var x => x.ToString()! },
                getWeightText(effect?.Result2Weight),
                effect?.Result3 ?? "",
                effect?.Result3Weight switch { 0 => "", null => "", var x => x.ToString()! },
                getWeightText(effect?.Result3Weight),
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
                    CurioTracker.Stress => "stress",
                    CurioTracker.HealStress => "heal_stress",
                    _ => ""
                },
                effect?.Notes ?? ""
            };
        }

        addRow();
        addRow();
        foreach (var curio in Curios.OrderBy(x => x.Id))
        {
            string getWeightText(WeightedCurioEffect effect)
            {
                if (effect.Weight == 0) return "";
                return effect.Weight.ToString();
            }
            string getWeightPercentageText(WeightedCurioEffect effect)
            {
                if (effect.Weight == 0) return "";
                return $"{effect.Weight / curio!.Effects.TotalWeight * 100:N2}%";
            }

            addRow("", curio.Id.ToString(), curio.Name, "", Enum.GetName(typeof(CurioQuality), curio.Quality) ?? "Mixed");
            addRow("", "", "ID STRING", "", "RESULT TYPES", "WEIGHT", "% CHANCE", "RESULT 1", "R1 WEIGHT", "R1 %", "RESULT 2", "R2 WEIGHT", "R2 %", "RESULT 3", "R3 WEIGHT", "R3 %", "", "STRING", "NOTES");
            addRow(new[] { "", "", curio.IdString, "", "Nothing", getWeightText(curio.Effects.Nothing), getWeightPercentageText(curio.Effects.Nothing) }.Concat(makeCurioEffectStrings(curio.Effects.Nothing)).ToArray());
            addRow(new[] { "", "", "REGION FOUND", "", "Loot", getWeightText(curio.Effects.Loot), getWeightPercentageText(curio.Effects.Loot) }.Concat(makeCurioEffectStrings(curio.Effects.Loot)).ToArray());
            addRow(new[] { "", "", curio.RegionFound switch
                {
                    Region.All => "ALL",
                    Region.DarkestDungeon => "Darkest Dungeon",
                    Region x => Enum.GetName(typeof(Region), x) ?? ""
                }, "", "Quirk", getWeightText(curio.Effects.Quirk), getWeightPercentageText(curio.Effects.Quirk) }.Concat(makeCurioEffectStrings(curio.Effects.Quirk)).ToArray());
            addRow(new[] { "", "", "FULL CURIO?", "", "Effect", getWeightText(curio.Effects.Effect), getWeightPercentageText(curio.Effects.Effect) }.Concat(makeCurioEffectStrings(curio.Effects.Effect)).ToArray());
            addRow(new[] { "", "", curio.FullCurio ? "Yes" : "No", "", "Purge", getWeightText(curio.Effects.Purge), getWeightPercentageText(curio.Effects.Purge) }.Concat(makeCurioEffectStrings(curio.Effects.Purge)).ToArray());
            addRow(new[] { "", "", "TAGS", "", "Scouting", getWeightText(curio.Effects.Scouting), getWeightPercentageText(curio.Effects.Scouting) }.Concat(makeCurioEffectStrings(curio.Effects.Scouting)).ToArray());
            addRow(new[] { "", "", curio.Tags[0], curio.Tags[1], "Teleport", getWeightText(curio.Effects.Teleport), getWeightPercentageText(curio.Effects.Teleport) }.Concat(makeCurioEffectStrings(curio.Effects.Teleport)).ToArray());
            addRow(new[] { "", "", curio.Tags[2], curio.Tags[3], "Disease", getWeightText(curio.Effects.Disease), getWeightPercentageText(curio.Effects.Disease) }.Concat(makeCurioEffectStrings(curio.Effects.Disease)).ToArray());
            addRow("", "", curio.Tags[4], curio.Tags[5]);
            addRow("", "", "Item Interactions", "", "ITEM", "RESULT TYPE", "", "RESULT 1", "R1 WEIGHT", "R1 %", "RESULT 2", "R2 WEIGHT", "R2 %", "RESULT 3", "R3 WEIGHT", "R3 %", "CURIO TRACKER ID", "STRING", "NOTES");
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
