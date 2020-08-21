using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer.DDFileTypes
{
    public record Darkest(IReadOnlyDictionary<string, IReadOnlyList<Darkest.DarkestEntry>> Entries)
    {
        public record DarkestEntry(string Type, IReadOnlyDictionary<string, string[]> Properties);

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

            string? currentType = null;
            Dictionary<string, string[]> currentProps = null!;
            string? currentPropName = null;
            List<string> currentPropValues = null!;

            foreach (var str in strings)
            {
                if (str.EndsWith(':'))
                {
                    if (currentType != null)
                    {
                        if (currentPropName != null)
                        {
                            currentProps[currentPropName] = currentPropValues.ToArray();
                        }
                        entries.GetValueOrSetDefault(currentType, new List<DarkestEntry>()).Add(new DarkestEntry(currentType, currentProps));
                    }
                    currentType = str[0..^1];
                    currentProps = new Dictionary<string, string[]>();
                }
                else if (currentType != null)
                {
                    if (str.StartsWith('.'))
                    {
                        if (currentPropName != null)
                        {
                            currentProps[currentPropName] = currentPropValues.ToArray();
                        }
                        currentPropName = str[1..];
                        currentPropValues = new List<string>();
                    }
                    else if (currentPropName != null)
                    {
                        currentPropValues.Add(str);
                    }
                }
            }

            if (currentType != null)
            {
                if (currentPropName != null)
                {
                    currentProps[currentPropName] = currentPropValues.ToArray();
                }
                entries.GetValueOrSetDefault(currentType, new List<DarkestEntry>()).Add(new DarkestEntry(currentType, currentProps));
            }

            return new Darkest(entries.ToImmutableDictionary(p => p.Key, p => (IReadOnlyList<DarkestEntry>)p.Value));
        }

        public void WriteToFile(string path)
        {
            File.WriteAllText(path, WriteToString());
        }

        public async void WriteToFileAsync(string path)
        {
            await File.WriteAllTextAsync(path, WriteToString());
        }

        public string WriteToString()
        {
            var result = new StringBuilder();
            foreach (var pair in Entries)
            {
                foreach (var entry in pair.Value)
                {
                    result.Append(entry.Type).Append(": ");
                    foreach (var propPair in entry.Properties.OrderBy(p => p.Key))
                    {
                        result.Append('.').Append(propPair.Key).Append(' ');
                        foreach (var value in propPair.Value)
                        {
                            result.Append(value).Append(' ');
                        }
                    }
                    result.Append('\n');
                }
            }
            result.Length -= 1;
            return result.ToString();
        }
    }
}
