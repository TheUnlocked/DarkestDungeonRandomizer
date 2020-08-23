﻿using Avalonia.Controls;
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

namespace DarkestDungeonRandomizer
{
    public class MainViewModel : ReactiveObject
    {
        /// <summary>
        /// If attempting to read game data, use GetGameDataPath instead.
        /// </summary>
        public string DDPath { get; set; } = "";

        public int Seed { get; set; } = 0;

        public bool RandomizeCurioRegions { get; set; } = true;
        public bool RandomizeCurioEffects { get; set; } = true;
        public bool RandomizeCurioInteractions { get; set; } = true;
        public bool IncludeShamblerAltar { get; set; } = false;
        public bool IncludeStoryCurios { get; set; } = true;
        public bool RandomizeMonsters { get; set; } = true;
        public bool RandomizeBosses { get; set; } = true;
        public double RandomizeHeroStats { get; set; } = 1;

        public DirectoryInfo ModDirectory { get; private set; } = null!;
        public Dictionary<string, Monster> Monsters { get; private set; } = null!;


        private readonly Window window;

        public MainViewModel(Window window)
        {
            this.window = window;
        }

        public async Task SelectGameDirectory()
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            DDPath = await dialog.ShowAsync(window);
            this.RaisePropertyChanged("DDPath");
        }

        public void RandomizeSeed()
        {
            Seed = new Random().Next();
            this.RaisePropertyChanged("Seed");
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
                MessageBox.Show(
                    window,
                    $"The randomizer mod has been created. Its tag is {ModCreator.GetRandomizerUUID(this)}",
                    "Randomizer Finished",
                    MessageBox.MessageBoxButtons.Ok);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine();
                
                //MessageBoxManager.GetMessageBoxStandardWindow(
                //    e.GetType().FullName, $"{e.Message}\n{e.StackTrace}",
                //    MessageBox.Avalonia.Enums.ButtonEnum.Ok,
                //    MessageBox.Avalonia.Enums.Icon.Error
                //).Show();
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
