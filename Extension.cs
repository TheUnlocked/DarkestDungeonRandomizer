using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer
{
    static class Extension
    {
        public static int? TryParseInt(this string value)
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return null;
        }

        public static TValue GetValueOrSetDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue @default)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            else
            {
                dict[key] = @default;
                return @default;
            }
        }

        public static T[] Shuffle<T>(this IEnumerable<T> original, Random random)
        {
            T[] newData = original.ToArray();
            int n = newData.Length;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = newData[k];
                newData[k] = newData[n];
                newData[n] = value;
            }
            return newData;
        }
    }
}
