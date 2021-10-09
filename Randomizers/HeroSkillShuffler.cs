using DarkestDungeonRandomizer.DDFileTypes;
using DynamicData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace DarkestDungeonRandomizer.Randomizers;

class HeroSkillShuffler : IRandomizer
{
    private const string ABOM = "abomination";

    private readonly MainViewModel model;
    private readonly Random random;

    public HeroSkillShuffler(MainViewModel model, Random random)
    {
        this.model = model;
        this.random = random;
    }

    public void Randomize()
    {
        if (model.RandomizeHeroSkills)
        {
            var heroesDir = model.ModDirectory.CreateSubdirectory("heroes");
            foreach (var hero in model.HeroNames)
            {
                heroesDir.CreateSubdirectory(hero);
            }

            var heroSkillMappings = GenerateHeroSkillMappings();

            var heroFiles = model.HeroNames.ToDictionary(
                name => name,
                name => (
                    info: Darkest.LoadFromFile(model.GetGameDataPath(Path.Combine("heroes", name, $"{name}.info.darkest"))),
                    art: Darkest.LoadFromFile(model.GetGameDataPath(Path.Combine("heroes", name, $"{name}.art.darkest")))
                ));

            Darkest highwaymanInfo = null!;
            Darkest crusaderInfo = null!;

            XmlDocument heroStrings = new XmlDocument();
            heroStrings.Load(model.GetGameDataPath(Path.Combine("localization", "heroes.string_table.xml")));

            var heroSkillNames = heroFiles.ToDictionary(p => p.Key, p => GetSkillsInOrder(p.Value.art));

            foreach (var hero in model.HeroNames)
            {
                var (info, art) = SwapCombatSkills(heroFiles, hero, heroSkillMappings);
                info.WriteToFile(Path.Combine(model.ModDirectory.FullName, "heroes", hero, $"{hero}.info.darkest"));
                art.WriteToFile(Path.Combine(model.ModDirectory.FullName, "heroes", hero, $"{hero}.art.darkest"));

                if (hero == "highwayman") highwaymanInfo = info;
                else if (hero == "crusader") crusaderInfo = info;

                var skillNames = heroSkillNames[hero];

                // Localization
                for (int i = 0; i < 7; i++)
                {
                    if (heroSkillMappings[hero][i] == hero) continue;

                    var combatNodes = heroStrings.SelectNodes($"//entry[@id='combat_skill_name_{heroSkillMappings[hero][i]}_{heroSkillNames[heroSkillMappings[hero][i]][i]}']")!
                        .Cast<XmlNode>()
                        .Zip(heroStrings.SelectNodes($"//entry[@id='combat_skill_name_{hero}_{skillNames[i]}']")!
                            .Cast<XmlNode>(),
                            (a, b) => (from: b, to: a));
                    foreach (var (from, to) in combatNodes)
                    {
                        var originalAttr = heroStrings.CreateAttribute("original");
                        originalAttr.Value = from!.InnerXml;
                        from!.Attributes!.Append(originalAttr);

                        if (to?.Attributes?.GetNamedItem("original") is XmlAttribute a)
                        {
                            from!.InnerXml = a.Value;
                        }
                        else
                        {
                            from!.InnerXml = to!.InnerXml;
                        }
                    }

                    var upgradeNodes = heroStrings.SelectNodes($"//entry[@id='upgrade_tree_name_{heroSkillMappings[hero][i]}.{heroSkillNames[heroSkillMappings[hero][i]][i]}']")!
                        .Cast<XmlNode>()
                        .Zip(heroStrings.SelectNodes($"//entry[@id='upgrade_tree_name_{hero}.{skillNames[i]}']")!
                            .Cast<XmlNode>(),
                            (a, b) => (from: b, to: a));
                    foreach (var (from, to) in upgradeNodes)
                    {
                        var originalAttr = heroStrings.CreateAttribute("original");
                        originalAttr.Value = from!.InnerXml;
                        from!.Attributes!.Append(originalAttr);

                        if (to?.Attributes?.GetNamedItem("original") is XmlAttribute a)
                        {
                            from!.InnerXml = a.Value;
                        }
                        else
                        {
                            from!.InnerXml = to!.InnerXml;
                        }
                    }
                }
            }

            foreach (var node in heroStrings!.SelectNodes("//*[@original]")!.Cast<XmlNode>())
            {
                node?.Attributes!.RemoveNamedItem("original");
            }

            heroStrings.Save(Path.Combine(model.ModDirectory.CreateSubdirectory("localization").FullName, "heroes.string_table.xml"));

            SwapSkillIcons(heroSkillMappings);
            //UpdateStartingSave(highwaymanInfo, crusaderInfo);
        }
    }

    private Dictionary<string, string[]> GenerateHeroSkillMappings()
    {
        Dictionary<string, string[]> heroSkillMappings = new();
        foreach (var hero in model.HeroNames)
        {
            heroSkillMappings[hero] = new string[model.HeroNames.Length];
        }

        // Making sure nothing wonky happens with transform getting swapped out
        var heroesWithoutAbom = model.HeroNames.Where(x => x != ABOM);
        foreach (var pair in heroesWithoutAbom.Shuffle(random).Zip(heroesWithoutAbom, (shuffled, original) => (original, shuffled)))
        {
            heroSkillMappings[pair.shuffled][0] = pair.original;
        }
        heroSkillMappings[ABOM][0] = ABOM;

        for (int i = 1; i < 7; i++)
        {
            foreach (var pair in model.HeroNames.Shuffle(random).Zip(model.HeroNames, (shuffled, original) => (original, shuffled)))
            {
                heroSkillMappings[pair.shuffled][i] = pair.original;
            }
        }
        return heroSkillMappings;
    }

    private (Darkest info, Darkest art) SwapCombatSkills(Dictionary<string, (Darkest info, Darkest art)> files, string hero, Dictionary<string, string[]> mappings)
    {
        var (info, art) = files[hero];

        var skills = GetSkillsInOrder(art);

        var skillMappings = new Dictionary<string, string>();
        var newCombatEntries = new List<Darkest.DarkestEntry>();

        string? riposte = null;

        for (int i = 0; i < 7; i++)
        {
            var newSkill = GetSkillsInOrder(files[mappings[hero][i]].art)[i];
            skillMappings[skills[i]] = newSkill;

            riposte ??= newSkill switch
            {
                "retribution" => "man_at_arms",
                "duelist_advance" => "highwayman",
                _ => null
            };

            newCombatEntries.AddRange(files[mappings[hero][i]].info.Entries["combat_skill"]
                .Where(x => x.Properties["id"][0][1..^1] == newSkill)
                .Select(x => x with
                {
                    Properties = x.Properties.ToDictionary(p => p.Key, p => p.Key == "id" ? new[] { $@"""{skills[i]}""" } : p.Value)
                })
                .OrderBy(x => x.Properties["level"][0].TryParseInt()));
        }

        var newInfoEntries = info.Entries.ToDictionary(p => p.Key, p => p.Value);
        newInfoEntries["combat_skill"] = newCombatEntries;

        var newInfo = (info with { Entries = newInfoEntries })
            .WithoutProperty("combat_skill", "valid_modes");

        if (hero == ABOM)
        {
            var HUMAN = new[] { "human" };
            var BEAST = new[] { "beast" };
            var BOTH = new[] { "human", "beast" };
            newInfo = newInfo.WithProperty("combat_skill", "valid_modes", i => (i / 5) switch
            {
                0 => BOTH,
                1 => HUMAN,
                2 => HUMAN,
                3 => HUMAN,
                4 => BEAST,
                5 => BEAST,
                6 => BEAST,
                _ => BOTH
            }).WithProperty("combat_skill", "generation_guaranteed", i => i == 0 ? new[] { "true" } : new[] { "false" });
        }

        //void SwapSkillUpgradesRefStrings(JToken obj, string prop)
        //{
        //    if (obj.Type == JTokenType.Object && obj[prop] != null)
        //    {
        //        if (skillMappings.ContainsKey(obj[prop]!.ToString()[(hero.Length + 1)..]))
        //        {
        //            obj[prop] = JToken.FromObject($"{hero}.{skillMappings[obj[prop]!.ToString()[(hero.Length + 1)..]]}");
        //        }
        //    }
        //}

        //var heroUpgradeFile = JObject.Parse(File.ReadAllText(
        //    model.GetGameDataPath(Path.Combine("upgrades", "heroes", $"{hero}.upgrades.json"))));
        //foreach (var item in heroUpgradeFile["trees"]!)
        //{
        //    SwapSkillUpgradesRefStrings(item, "id");
        //    foreach (var req in item["requirements"]!)
        //    {
        //        foreach (var treeReq in (req["prerequisite_requirements"] as JArray)!.Where(x => x["tree_id"] != null))
        //        {
        //            SwapSkillUpgradesRefStrings(treeReq, "tree_id");
        //        }
        //    }
        //}
        //File.WriteAllText(
        //    Path.Combine(
        //        model.ModDirectory
        //            .CreateSubdirectory("upgrades")
        //            .CreateSubdirectory("heroes")
        //            .FullName,
        //        $"{hero}.upgrades.json"),
        //    heroUpgradeFile.ToString());

        //var newArt = art.Replace("combat_skill", "id", (_, i, _) => files[mappings[hero][i]].art.Entries["combat_skill"][i].Properties["id"][0]);
        var newArt = art;

        if (riposte != null)
        {
            newInfo = newInfo.AddEntries(files[riposte].info.Entries["riposte_skill"][0])
                with
            { EntryTypeOrder = files[riposte].info.EntryTypeOrder };

            var replacementGraphics = newArt.Entries["combat_skill"]
                .Where(x => x.Properties.ContainsKey("anim") && x.Properties.ContainsKey("fx") && x.Properties.ContainsKey("targchestfx"))
                .First();
            newArt = newArt.AddEntries(files[riposte].art.Entries["riposte_skill"][0])
                .Replace("riposte_skill", new[] {
                    ("anim", (Darkest.DarkestPropertyConversionFunction)((_, _, _) => replacementGraphics.Properties["anim"][0])),
                    ("fx", (Darkest.DarkestPropertyConversionFunction)((_, _, _) => replacementGraphics.Properties["fx"][0])),
                    ("targchestfx", (Darkest.DarkestPropertyConversionFunction)((_, _, _) => replacementGraphics.Properties["targchestfx"][0]))
                })
                with
            { EntryTypeOrder = files[riposte].art.EntryTypeOrder };
        }

        return (newInfo, newArt);
    }

    private static readonly string[] numberNames = new[]
    {
        "one", "two", "three", "four",
        "five", "six", "seven"
    };

    private string[] GetSkillsInOrder(Darkest artFile)
    {
        return artFile.Entries["combat_skill"]
            .OrderBy(x => numberNames.IndexOf(x.Properties["icon"][0][1..^1]))
            .Select(x => x.Properties["id"][0][1..^1])
            .ToArray();
    }

    private void SwapSkillIcons(Dictionary<string, string[]> heroSkillMappings)
    {
        foreach (var mapping in heroSkillMappings)
        {
            for (int i = 0; i < 7; i++)
            {
                File.Copy(
                    Path.Combine(model.DDPath, "heroes", mapping.Value[i], $"{mapping.Value[i]}.ability.{numberNames[i]}.png"),
                    Path.Combine(model.ModDirectory.FullName, "heroes", mapping.Key, $"{mapping.Key}.ability.{numberNames[i]}.png"));
            }
        }
    }

    //private void UpdateStartingSave(Darkest highwayman, Darkest crusader)
    //{
    //    var startingRoster = JObject.Parse(File.ReadAllText(model.GetGameDataPath(Path.Combine("scripts", "starting_save", "persist.roster.json"))));
    //    var heroes = startingRoster["data"]?["heroes"];
    //    var reynauldSkills = heroes?["1"]?["skills"];
    //    if (reynauldSkills != null)
    //    {
    //        reynauldSkills["selected_combat_skills"] =
    //            JObject.FromObject(crusader
    //                .Entries["combat_skill"]
    //                .Select(x => x.Properties["id"][0][1..^1])
    //                .Distinct()
    //                .Shuffle(random)
    //                .Take(4)
    //                .ToDictionary(x => x, _ => 0));
    //    }
    //    var dismasSkills = heroes?["2"]?["skills"];
    //    if (dismasSkills != null)
    //    {
    //        dismasSkills["selected_combat_skills"] =
    //            JObject.FromObject(highwayman
    //                .Entries["combat_skill"]
    //                .Select(x => x.Properties["id"][0][1..^1])
    //                .Distinct()
    //                .Shuffle(random)
    //                .Take(4)
    //                .ToDictionary(x => x, _ => 0));
    //    }

    //    File.WriteAllText(
    //        Path.Combine(
    //            model.ModDirectory
    //                .CreateSubdirectory("scripts")
    //                .CreateSubdirectory("starting_save")
    //                .FullName,
    //            "persist.roster.json"),
    //        startingRoster.ToString());
    //}
}
