using Avalonia;

namespace WiiiiGotThis.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    private static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<WiiiiGotThis.Presentation.App>().UsePlatformDetect().LogToTrace();
}
