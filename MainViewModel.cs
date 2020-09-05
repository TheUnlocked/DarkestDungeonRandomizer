using Avalonia.Controls;
using DarkestDungeonRandomizer.DDFileTypes;
using DarkestDungeonRandomizer.DDTypes;
using DarkestDungeonRandomizer.Randomizers;
using DarkestDungeonRandomizer.MsgBox;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Globalization;

namespace DarkestDungeonRandomizer
{
    public class MainViewModel : ReactiveObject
    {
        /// <summary>
        /// If attempting to read game data, use GetGameDataPath instead.
        /// </summary>
        public string DDPath { get; set; } = "";

        public string Tag { get; set; } = "";
        public int Seed { get; set; } = 0;

        public bool RandomizeCurioRegions { get; set; } = true;
        public bool RandomizeCurioEffects { get; set; } = true;
        public bool RandomizeCurioInteractions { get; set; } = true;
        public bool IncludeShamblerAltar { get; set; } = false;
        public bool IncludeStoryCurios { get; set; } = true;
        public bool RandomizeMonsters { get; set; } = true;
        public bool RandomizeBosses { get; set; } = true;
        public double RandomizeHeroStats { get; set; } = 1;
        public bool RandomizeCampingSkills { get; set; } = true;
        public bool RandomizeHeroSkills { get; set; } = true;

        public DirectoryInfo ModDirectory { get; private set; } = null!;
        public Dictionary<string, Monster> Monsters { get; private set; } = null!;
        public string[] HeroNames { get; private set; } = null!;

        public string BuildDate { get; } =
            File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location)
            .ToString("g", CultureInfo.GetCultureInfo("en-US"));

        private readonly Window window;

        public MainViewModel(Window window)
        {
            this.window = window;
            this.RaisePropertyChanged();
        }

        public async Task SelectGameDirectory()
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            DDPath = await dialog.ShowAsync(window);
            this.RaisePropertyChanged(nameof(DDPath));
        }

        public void RandomizeSeed()
        {
            Seed = new Random().Next();
            this.RaisePropertyChanged(nameof(Seed));
        }

        public void LoadOptionsFromTag()
        {
            try
            {
                ModCreator.PopulateRandomizerOptionsFromUUID(this, Tag);
                this.RaisePropertyChanged("");
            }
            catch
            {
                MessageBox.Show(window, $"Provided tag \"{Tag}\" is either invalid or was created with an incompatible version of the randomizer.", "Invalid Tag", MessageBox.MessageBoxButtons.Ok);
            }
        }

        public void CreateRandomizerMod()
        {
            if (DDPath == "")
            {
                MessageBox.Show(window, "Please set the Darkest Dungeon game folder.", "Folder Not Found", MessageBox.MessageBoxButtons.Ok);
                return;
            }
            try
            {
                ReadBaseGameData();

                ModDirectory = ModCreator.CreateMod(this);
                var rand = new Random(Seed);

                new CurioShuffler(this, rand).Randomize();
                new EnemyShuffler(this, rand).Randomize();
                new HeroStatRandomizer(this, rand).Randomize();
                new CampingSkillRandomizer(this, rand).Randomize();
                new HeroSkillShuffler(this, rand).Randomize();

                Process.Start(new ProcessStartInfo("cmd.exe", $"/C echo/ | \"{Path.Combine(DDPath, "_windows", "steam_workshop_upload.exe")}\" \"{Path.Combine(ModDirectory.FullName, "project.xml")}\"")
                {
                    WorkingDirectory = Path.Combine(DDPath, "_windows"),
                    CreateNoWindow = true,
                    UseShellExecute = false
                })?.WaitForExit();

                Tag = ModCreator.GetRandomizerUUID(this);
                this.RaisePropertyChanged(nameof(Tag));
                MessageBox.Show(
                    window,
                    $"The randomizer mod has been created. Its tag is {Tag}",
                    "Randomizer Finished",
                    MessageBox.MessageBoxButtons.Ok);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine();

                MessageBox.Show(
                    window,
                    $"{e.Message}\n{e.StackTrace}",
                    e.GetType().FullName ?? "Exception",
                    MessageBox.MessageBoxButtons.Ok
                );
            }
        }

        private void ReadBaseGameData()
        {
            Monsters = new Dictionary<string, Monster>();
            foreach (var monsterType in Directory.GetDirectories(Path.Combine(DDPath, "monsters")).Select(Path.GetFileName))
            {
                if (monsterType == null) continue;
                foreach (var monsterName in Directory.GetDirectories(Path.Combine(DDPath, "monsters", monsterType))
                    .Select(Path.GetFileName)
                    .Where(x => x != null && x.StartsWith(monsterType)))
                {
                    if (monsterName == null) continue; // Extraneous but makes NRT stuff go away
                    Monsters[monsterName] = Monster.FromDarkest(
                        monsterName,
                        Darkest.LoadFromFile(Path.Combine(DDPath, "monsters", monsterType, monsterName, $"{monsterName}.info.darkest"))
                    );
                }
            }
            HeroNames = Directory.GetDirectories(Path.Combine(DDPath, "heroes")).Select(x => Path.GetFileName(x)).ToArray();
        }

        public string GetGameDataPath(string partialPath)
        {
            if (File.Exists(Path.Combine(ModDirectory.FullName, partialPath)))
            {
                return Path.Combine(ModDirectory.FullName, partialPath);
            }
            else
            {
                return Path.Combine(DDPath, partialPath);
            }
        }

        public void OpenModsFolder()
        {
            try
            {
                Process.Start(new ProcessStartInfo() { FileName = Path.Combine(DDPath, "mods/"), UseShellExecute = true });
            }
            catch
            {
                MessageBox.Show(window, "Please set the Darkest Dungeon game folder.", "Folder Not Found", MessageBox.MessageBoxButtons.Ok);
            }
        }

        public void StartDarkestDungeon()
        {
            try
            {
                Process.Start(new ProcessStartInfo(Path.Combine(DDPath, "_windowsnosteam", "Darkest.exe"))
                {
                    WorkingDirectory = Path.Combine(DDPath, "_windowsnosteam")
                });
            }
            catch
            {
                MessageBox.Show(window, "Please set the Darkest Dungeon game folder.", "Folder Not Found", MessageBox.MessageBoxButtons.Ok);
            }
        }
    }
}
