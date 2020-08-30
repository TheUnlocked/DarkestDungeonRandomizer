using DarkestDungeonRandomizer.DDFileTypes;
using DynamicData.Kernel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer.Randomizers
{
    public class CampingSkillRandomizer : IRandomizer
    {
        private readonly MainViewModel model;
        private readonly Random random;

        public CampingSkillRandomizer(MainViewModel model, Random random)
        {
            this.model = model;
            this.random = random;
        }

        public void Randomize()
        {
            if (model.RandomizeCampingSkills)
            {
                var file = JObject.Parse(File.ReadAllText(model.GetGameDataPath(Path.Combine("raid", "camping", "default.camping_skills.json"))));

                if (!(file["skills"] is JArray skills) || skills.Count < 7)
                {
                    throw new Exception("default.camping_skills.json could not be parsed properly.");
                }

                // Make in-game layout look prettier :)
                file["configuration"]!["class_specific_number_of_classes_threshold"] = 100;
                var layoutFile = Darkest.LoadFromFile(model.GetGameDataPath(Path.Combine("campaign", "town", "buildings", "camping_trainer", "camping_trainer.layout.darkest")));
                layoutFile.Replace("camping_trainer_class_specific_skill_grid_layout", "skill_spacing", (x, _, i) => i == 1 ? "170" : x)
                    .WriteToFile(Path.Combine(
                        model.ModDirectory
                            .CreateSubdirectory("campaign")
                            .CreateSubdirectory("town")
                            .CreateSubdirectory("buildings")
                            .CreateSubdirectory("camping_trainer")
                            .FullName,
                        "camping_trainer.layout.darkest"));

                foreach (var skill in skills)
                {
                    skill["hero_classes"] = new JArray();
                }

                foreach (var hero in model.HeroNames)
                {
                    HashSet<int> skillIndicies = new HashSet<int>();
                    while (skillIndicies.Count < 7)
                    {
                        skillIndicies.Add(random.Next(0, skills.Count));
                    }
                    foreach (var skillIndex in skillIndicies)
                    {
                        (skills[skillIndex]["hero_classes"] as JArray)!.Add(hero);
                    }
                }

                File.WriteAllText(Path.Combine(
                    model.ModDirectory
                        .CreateSubdirectory("raid")
                        .CreateSubdirectory("camping")
                        .FullName,
                    "default.camping_skills.json"), file.ToString());
            }
        }
    }
}
