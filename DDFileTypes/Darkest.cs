using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer.DDFileTypes
{
    public record Darkest(IReadOnlyDictionary<string, IReadOnlyList<Darkest.DarkestEntry>> Entries, IReadOnlyDictionary<string, int> EntryTypeOrder)
    {
        public record DarkestEntry(string Type, IReadOnlyDictionary<string, IReadOnlyList<string>> Properties, IReadOnlyDictionary<string, int> PropOrder);

        public static Darkest LoadFromFile(string filename)
        {
            using var file = File.OpenText(filename);
            return Load(file.ReadToEnd());
        }

        public static async Task<Darkest> LoadFromFileAsync(string filename)
        {
            using var file = File.OpenText(filename);
            return Load(await file.ReadToEndAsync());
        }

        public static Darkest Load(string data)
        {
            var decommented = string.Join('\n', data.Split('\n').Where(x => !x.StartsWith("//")));
            var strings = decommented.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, List<DarkestEntry>> entries = new Dictionary<string, List<DarkestEntry>>();

            Dictionary<string, int> typeOrder = new Dictionary<string, int>();
            string? currentType = null;

            Dictionary<string, int> propOrder = null!;
            Dictionary<string, IReadOnlyList<string>> currentProps = null!;
            string? currentPropName = null;

            List<string> currentPropValues = null!;

            void ClearTypeBuffer(string? nextType)
            {
                if (currentType != null)
                {
                    ClearPropBuffer(null);
                    entries.GetValueOrSetDefault(currentType, new List<DarkestEntry>()).Add(new DarkestEntry(currentType, currentProps, propOrder));
                    if (!typeOrder.ContainsKey(currentType))
                    {
                        typeOrder[currentType] = typeOrder.Count;
                    }
                }
                if (nextType != null)
                {
                    currentType = nextType;

                    propOrder = new Dictionary<string, int>();
                    currentProps = new Dictionary<string, IReadOnlyList<string>>();
                    currentPropName = null;
                }
            }

            void ClearPropBuffer(string? nextProp)
            {
                if (currentPropName != null)
                {
                    currentProps[currentPropName] = currentPropValues.ToArray();
                    if (!propOrder.ContainsKey(currentPropName))
                    {
                        propOrder[currentPropName] = propOrder.Count;
                    }
                }
                if (nextProp != null)
                {
                    currentPropName = nextProp;
                    currentPropValues = new List<string>();
                }
            }

            foreach (var str in strings)
            {
                if (str.EndsWith(':'))
                {
                    ClearTypeBuffer(str[0..^1]);
                }
                else if (currentType != null)
                {
                    if (str.StartsWith('.'))
                    {
                        ClearPropBuffer(str[1..]);
                        
                    }
                    else if (currentPropName != null)
                    {
                        currentPropValues.Add(str);
                    }
                }
            }
            ClearTypeBuffer(null);

            return new Darkest(entries.ToImmutableDictionary(p => p.Key, p => (IReadOnlyList<DarkestEntry>)p.Value), typeOrder);
        }

        public void WriteToFile(string path)
        {
            File.WriteAllText(path, ToString());
        }

        public async void WriteToFileAsync(string path)
        {
            await File.WriteAllTextAsync(path, ToString());
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            foreach (var pair in Entries.OrderBy(p => EntryTypeOrder.GetValueOrDefault(p.Key, int.MaxValue)))
            {
                foreach (var entry in pair.Value)
                {
                    result.Append(entry.Type).Append(": ");
                    foreach (var propPair in entry.Properties.OrderBy(p => entry.PropOrder.GetValueOrDefault(p.Key, int.MaxValue)))
                    {
                        result.Append('.').Append(propPair.Key).Append(' ');
                        foreach (var value in propPair.Value)
                        {
                            result.Append(value).Append(' ');
                        }
                    }
                    result.Append("\r\n");
                }
            }
            return result.ToString();
        }


        public delegate string DarkestPropertyConversionFunction(string original, int entryIndex, int propertyIndex);
        public Darkest Replace(IEnumerable<(string entryType, IEnumerable<(string property, DarkestPropertyConversionFunction conversion)> propReplacements)> entryMatches)
        {
            var newEntries = Entries.ToDictionary(p => p.Key, p => p.Value);

            foreach (var (entryType, propReplacements) in entryMatches)
            {
                newEntries[entryType] = newEntries[entryType].Select((entry, entryIndex) =>
                {
                    var newProps = entry.Properties.ToDictionary(p => p.Key, p => p.Value);
                    foreach (var (property, conversion) in propReplacements)
                    {
                        newProps[property] = newProps[property].Select((x, propIndex) => conversion(x, entryIndex, propIndex)).ToImmutableArray();
                    }
                    return entry with { Properties = newProps };
                }).ToImmutableArray();
            }

            return this with { Entries = newEntries };
        }

        public Darkest Replace(string entryType, IEnumerable<(string property, DarkestPropertyConversionFunction conversion)> replacements)
        {
            return Replace(new[] { (entryType, replacements) });
        }

        public Darkest Replace(string entryType, string property, DarkestPropertyConversionFunction conversion)
        {
            return Replace(entryType, new[] { (property, conversion) });
        }

        public Darkest WithoutProperty(string entryType, string property)
        {
            var newEntries = Entries.ToDictionary(p => p.Key, p => p.Value);

            newEntries[entryType] = newEntries[entryType].Select(entry =>
            {
                var newProps = entry.Properties.ToDictionary(p => p.Key, p => p.Value);
                newProps.Remove(property);
                return entry with { Properties = newProps };
            }).ToImmutableArray();

            return this with { Entries = newEntries };
        }

        public delegate string[] DarkestPropertyAdd(int entryIndex);
        public Darkest WithProperty(string entryType, string property, DarkestPropertyAdd adder)
        {
            var newEntries = Entries.ToDictionary(p => p.Key, p => p.Value);

            newEntries[entryType] = newEntries[entryType].Select((entry, entryIndex) =>
            {
                var newProps = entry.Properties.ToDictionary(p => p.Key, p => p.Value);
                newProps[property] = adder(entryIndex);
                return entry with { Properties = newProps };
            }).ToImmutableArray();

            return this with { Entries = newEntries };
        }
    }
}
