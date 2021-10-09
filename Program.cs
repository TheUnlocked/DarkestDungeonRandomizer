using Avalonia;
using System.Globalization;

namespace DarkestDungeonRandomizer;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static void Main(string[] args)
    {
        // Invariant culture to make sure we don't create DD files wrong.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        // Avalonia boilerplate
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
