using Avalonia.Controls;
using DarkestDungeonRandomizer.DDFileTypes;
using DarkestDungeonRandomizer.DDTypes;
using MessageBox.Avalonia;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkestDungeonRandomizer
{
    public class MainViewModel : ReactiveObject
    {
        public string DDPath { get; set; } = "";

        public int Seed { get; set; } = 0;

        public bool RandomizeCurioRegions { get; set; } = true;
        public bool RandomizeCurioEffects { get; set; } = true;
        public bool RandomizeCurioInteractions { get; set; } = true;
        public bool IncludeShamblerAltar { get; set; } = false;
        public bool IncludeStoryCurios { get; set; } = true;

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
                return;
            }
            try
            {
                var shuffler = new Shuffler(this);
                var modFolder = ModCreator.CreateMod(this);
                var curiosFolder = modFolder.CreateSubdirectory("curios");
                var curios = CurioTypeLibrary.LoadFromFile(Path.Combine(DDPath, "curios", "curio_type_library.csv"));
                var shuffledCurios = shuffler.ShuffleCurios(curios);
                var dungeonCurioPropsFiles = new[]
                {
                    (Region.Cove, new[] { DDPath, "dungeons", "cove", "cove.props.darkest" }),
                    (Region.Ruins, new[] { DDPath, "dungeons", "crypts", "crypts.props.darkest" }),
                    (Region.Warrens, new[] { DDPath, "dungeons", "warrens", "warrens.props.darkest" }),
                    (Region.Weald, new[] { DDPath, "dungeons", "weald", "weald.props.darkest" }),
                    (Region.DarkestDungeon, new[] { DDPath, "dungeons", "darkestdungeon", "darkestdungeon.props.darkest" }),
                    (Region.Town, new[] { DDPath, "dungeons", "town", "town.props.darkest" })
                };
                var syncedDungeonCurioProps = shuffler.AlignDungeonPropsToCurioRegions(
                    dungeonCurioPropsFiles.Select(x => (x.Item1, Darkest.LoadFromFile(Path.Combine(x.Item2)))).ToArray(),
                    shuffledCurios);

                var dungeonsFolder = modFolder.CreateSubdirectory("dungeons");
                dungeonCurioPropsFiles.Zip(syncedDungeonCurioProps, (i, r) => {
                    var dungeonFolder = dungeonsFolder.CreateSubdirectory(i.Item2[2]);
                    r.Item2.WriteToFile(Path.Combine(Path.Combine(dungeonFolder.FullName), i.Item2[3]));
                    return 0;
                }).ToArray(); // ToArray to get side effects to compute.

                shuffledCurios.WriteToFile(Path.Combine(curiosFolder.FullName, "curio_type_library.csv"));
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                MessageBoxManager.GetMessageBoxStandardWindow("Folder Not Found", "Please set the Darkest Dungeon game folder.", MessageBox.Avalonia.Enums.ButtonEnum.Ok);
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
                MessageBoxManager.GetMessageBoxStandardWindow("Folder Not Found", "Please set the Darkest Dungeon game folder.", MessageBox.Avalonia.Enums.ButtonEnum.Ok);
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
                MessageBoxManager.GetMessageBoxStandardWindow("Folder Not Found", "Please set the Darkest Dungeon game folder.", MessageBox.Avalonia.Enums.ButtonEnum.Ok);
            }
        }
    }
}
