using DarkestDungeonRandomizer.DDFileTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer.Randomizers
{
    public class HeroStatRandomizer : IRandomizer
    {
        private readonly MainViewModel model;
        private readonly Random random;

        public HeroStatRandomizer(MainViewModel model, Random random)
        {
            this.model = model;
            this.random = random;
        }

        const int baseResistance = 40;

        public void Randomize()
        {
            if (model.RandomizeHeroStats > 0)
            {
                var heroesDir = model.ModDirectory.CreateSubdirectory("heroes");
            
                foreach (var heroName in model.HeroNames)
                {
                    var res = GenerateBalancedModifiers(7).Select(x => Math.Round(x * baseResistance).ToString()).ToArray();
                    var battle = GenerateBalancedModifiers(5);

                    var darkest = Darkest.LoadFromFile(model.GetGameDataPath(Path.Combine("heroes", heroName, $"{heroName}.info.darkest")));

                    var randomized = darkest.Replace(new[] {
                        ("resistances", new(string, Darkest.DarkestPropertyConversionFunction)[]
                        {
                            ("stun", (_, _) => $"{res[0]}%"), ("poison", (_, _) => $"{res[1]}%"), ("bleed", (_, _) => $"{res[2]}%"),
                            ("disease", (_, _) => $"{res[3]}%"), ("move", (_, _) => $"{res[4]}%"), ("debuff", (_, _) => $"{res[5]}%"),
                            ("trap", (_, _) => $"{res[6]}%")
                        }.AsEnumerable()),
                        ("weapon", new(string, Darkest.DarkestPropertyConversionFunction)[]
                        {
                            ("dmg", (x, _) => Math.Round(int.Parse(x) * battle[0]).ToString()),
                            ("crit", (x, _) => $"{Math.Round(int.Parse(x[..^1]) * battle[1])}%"),
                            ("spd", (x, _) => Math.Round(int.Parse(x) * battle[2]).ToString())
                        }.AsEnumerable()),
                        ("armour", new(string, Darkest.DarkestPropertyConversionFunction)[]
                        {
                            ("def", (x, _) => $"{Math.Round(double.Parse(x[..^1]) * battle[3], 1)}%"),
                            ("hp", (x, _) => Math.Round(int.Parse(x) * battle[4]).ToString())
                        }.AsEnumerable())
                    });

                    var heroDir = heroesDir.CreateSubdirectory(heroName);
                    randomized.WriteToFile(Path.Combine(heroDir.FullName, $"{heroName}.info.darkest"));
                }
            }
        }

        /// <summary>
        /// Generates random doubles whose mean is approximately 1.
        /// </summary>
        /// <param name="amount">The number of floats to obtain</param>
        /// <returns></returns>
        private double[] GenerateBalancedModifiers(int amount)
        {
            var values = Enumerable.Repeat(0, amount).Select(x => random.NextDouble() * model.RandomizeHeroStats);
            var mean = values.Average();
            values = values.Select(x => x - mean + 1);
            return values.ToArray();
        }
    }
}
