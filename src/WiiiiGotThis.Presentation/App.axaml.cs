using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace WiiiiGotThis.Presentation;

public sealed class App : Avalonia.Application
{
    private ShellViewModel? shellViewModel;

    public App() { }

    public App(ShellViewModel shellViewModel)
    {
        this.shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
    }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var shell = shellViewModel ?? throw new InvalidOperationException("Wiiii Got This Presentation App was not configured by its composition root.");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = shell };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MobileShellView { DataContext = shell };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
