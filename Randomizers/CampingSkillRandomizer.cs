using DarkestDungeonRandomizer.DDFileTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace DarkestDungeonRandomizer.Randomizers;

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
            var file = JsonNode.Parse(File.ReadAllText(model.GetGameDataPath(Path.Combine("raid", "camping", "default.camping_skills.json"))))
                ?.AsObject();

            if (file == null || file["skills"] is not JsonArray skills || skills.Count < 7)
            {
                throw new Exception("default.camping_skills.json could not be parsed properly.");
            }

            // Make in-game layout look prettier :)
            file["configuration"]!.AsObject()["class_specific_number_of_classes_threshold"] = JsonValue.Create(100);
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
                skill!.AsObject()["hero_classes"] = new JsonArray();
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
                    skills[skillIndex]?.AsObject()["hero_classes"]?.AsArray().Add(hero);
                }
            }

            File.WriteAllText(Path.Combine(
                model.ModDirectory
                    .CreateSubdirectory("raid")
                    .CreateSubdirectory("camping")
                    .FullName,
                "default.camping_skills.json"), file.ToJsonString(new () { WriteIndented = true }));
        }
    }
}
