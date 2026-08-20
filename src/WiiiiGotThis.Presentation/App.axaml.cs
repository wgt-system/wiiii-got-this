using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace WiiiiGotThis.Presentation;

public sealed class App : Avalonia.Application
{
    private ShellViewModel? shellViewModel;
    private IIlluminationProductSurfaceSource? illuminationProductSurfaceSource;
    private IVocationProductRuntime? vocationProductRuntime;
    private IOrientationProductRuntime? orientationProductRuntime;

    public App() { }

    public App(
        ShellViewModel shellViewModel,
        IIlluminationProductSurfaceSource? illuminationProductSurfaceSource = null,
        IVocationProductRuntime? vocationProductRuntime = null,
        IOrientationProductRuntime? orientationProductRuntime = null)
    {
        this.shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
        this.illuminationProductSurfaceSource = illuminationProductSurfaceSource;
        this.vocationProductRuntime = vocationProductRuntime;
        this.orientationProductRuntime = orientationProductRuntime;
    }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var shell = shellViewModel ?? throw new InvalidOperationException("Wiiii Got This Presentation App was not configured by its composition root.");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow { DataContext = shell };
            if (illuminationProductSurfaceSource is not null)
                window.ConfigureIlluminationProductSurfaceSource(illuminationProductSurfaceSource);
            if (vocationProductRuntime is not null && orientationProductRuntime is not null)
                window.ConfigureProductRuntimes(vocationProductRuntime, orientationProductRuntime);
            desktop.MainWindow = window;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MobileShellView { DataContext = shell };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
