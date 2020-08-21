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
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static DirectoryInfo CreateMod(MainViewModel options)
        {
            var uuid = GetRandomizerUUID(options);
            var dir = Directory.CreateDirectory(Path.Combine(options.DDPath, "mods", $"Randomizer {uuid}"));
            File.WriteAllText(Path.Combine(dir.FullName, "project.xml"),
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<project>
	<PreviewIconFile></PreviewIconFile>
	<ItemDescriptionShort/>
	<ModDataPath>{dir.FullName}</ModDataPath>
	<Title>Randomizer [{uuid}] (Seed: {options.Seed})</Title>
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
                (options.RandomizeCurioInteractions ? 2 : 0) +
                (options.RandomizeCurioRegions ? 4 : 0) +
                (options.IncludeShamblerAltar ? 8 : 0) +
                (options.IncludeStoryCurios ? 16 : 0);
            return addin.ToString("x").Trim('0') + options.Seed.ToString("x");
        }
    }
}
