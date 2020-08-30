using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer
{
    static class ModCreator
    {
        /// <summary>
        /// Creates a mod on disk and returns the directory it's contained within.
        /// If the mod already exists on disk, it will be deleted.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static DirectoryInfo CreateMod(MainViewModel options)
        {
            var uuid = GetRandomizerUUID(options);
            var modDirectory = Path.Combine(options.DDPath, "mods", $"Randomizer {uuid}");
            if (Directory.Exists(modDirectory))
            {
                Directory.Delete(modDirectory, true);
            }
            var dir = Directory.CreateDirectory(modDirectory);
            File.WriteAllText(Path.Combine(dir.FullName, "project.xml"),
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<project>
	<PreviewIconFile></PreviewIconFile>
	<ItemDescriptionShort/>
	<ModDataPath>{dir.FullName}</ModDataPath>
	<Title>Randomizer [Tag {uuid} | Seed {options.Seed}]</Title>
	<Language>english</Language>
	<UpdateDetails/>
	<Visibility>hidden</Visibility>
	<UploadMode>direct_upload</UploadMode>
	<VersionMajor>0</VersionMajor>
	<VersionMinor>0</VersionMinor>
	<TargetBuild>0</TargetBuild>
	<Tags></Tags>
	<ItemDescription>If playing the same randomizer as another player, make sure that the IDs match on both clients.</ItemDescription>
	<PublishedFileId></PublishedFileId>
</project>");
            return dir;
        }

        public static string GetRandomizerUUID(MainViewModel options)
        {
            int addin =
                (options.RandomizeCurioEffects ? 1 : 0) +
                (options.RandomizeCurioInteractions ? 1 << 1 : 0) +
                (options.RandomizeCurioRegions ? 1 << 2 : 0) +
                (options.IncludeShamblerAltar ? 1 << 3 : 0) +
                (options.IncludeStoryCurios ? 1 << 4 : 0) +
                (options.RandomizeMonsters ? 1 << 5 : 0) +
                (options.RandomizeBosses ? 1 << 6 : 0) +
                ((int)(options.RandomizeHeroStats * 4) << 7) + /* 3 bits */
                (options.RandomizeCampingSkills ? 1 << 10 : 0);
            return addin.ToString("x").Trim('0') + options.Seed.ToString("x");
        }

        public static void PopulateRandomizerOptionsFromUUID(MainViewModel model, string tag)
        {
            model.Seed = int.Parse(tag[^8..], System.Globalization.NumberStyles.HexNumber);
            var addin = int.Parse(tag[..^8], System.Globalization.NumberStyles.HexNumber);
            model.RandomizeCurioEffects = (addin & 1) != 0;
            model.RandomizeCurioInteractions = (addin & (1 << 1)) != 0;
            model.RandomizeCurioRegions = (addin & (1 << 2)) != 0;
            model.IncludeShamblerAltar = (addin & (1 << 3)) != 0;
            model.IncludeStoryCurios = (addin & (1 << 4)) != 0;
            model.RandomizeMonsters = (addin & (1 << 5)) != 0;
            model.RandomizeBosses = (addin & (1 << 6)) != 0;
            model.RandomizeHeroStats = ((addin & (7 << 7)) >> 7) / 4d;
            model.RandomizeCampingSkills = (addin & (1 << 10)) != 0;
        }
    }
}
