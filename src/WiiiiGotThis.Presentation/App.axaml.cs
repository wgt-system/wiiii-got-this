using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace WiiiiGotThis.Presentation;

public sealed class App : Avalonia.Application
{
    private ShellViewModel? shellViewModel;
    private IIlluminationProductSurfaceSource? illuminationProductSurfaceSource;

    public App() { }

    public App(
        ShellViewModel shellViewModel,
        IIlluminationProductSurfaceSource? illuminationProductSurfaceSource = null)
    {
        this.shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
        this.illuminationProductSurfaceSource = illuminationProductSurfaceSource;
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
            desktop.MainWindow = window;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MobileShellView { DataContext = shell };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
