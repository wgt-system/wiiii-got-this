#if WGT_IOS
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.iOS;
using Foundation;
using UIKit;
using WiiiiGotThis.Application;
using WiiiiGotThis.Infrastructure;
using WiiiiGotThis.Integrations.Reference;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.iOS;

[Register("AppDelegate")]
internal sealed class AppDelegate : UIApplicationDelegate, IAvaloniaAppDelegate
{
    public event EventHandler<ActivatedEventArgs>? Activated;
    public event EventHandler<ActivatedEventArgs>? Deactivated;

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        var shell = CreateShell();
        BuildAvaloniaApp(shell, this).SetupWithoutStarting();
        return true;
    }

    public override void OnActivated(UIApplication application) =>
        Activated?.Invoke(this, new ActivatedEventArgs(ActivationKind.Unspecified));

    public override void WillResignActive(UIApplication application) =>
        Deactivated?.Invoke(this, new ActivatedEventArgs(ActivationKind.Unspecified));

    private static ShellViewModel CreateShell()
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WiiiiGotThis");
        Directory.CreateDirectory(dataDirectory);

        var databasePath = Path.Combine(dataDirectory, "wiiii-got-this.db");
        var connectionFactory = new SqliteConnectionFactory($"Data Source={databasePath}");
        new MigrationRunner(connectionFactory).ApplyAsync().GetAwaiter().GetResult();

        var deviceStore = new SqliteLocalDeviceStore(connectionFactory);
        var integrationStore = new SqliteServiceIntegrationStore(connectionFactory);
        var publicationStore = new SqliteIntegrationPublicationStore(connectionFactory);
        var adapters = new StaticIntegrationAdapterCatalog([new ReferenceIntegrationAdapter()]);
        var ensureDevice = new EnsureCurrentDeviceUseCase(deviceStore);
        var refresh = new RefreshPublicationsUseCase(adapters, integrationStore, publicationStore);
        var list = new ListServiceIntegrationsUseCase(integrationStore, publicationStore);
        var global = new SetGlobalIntegrationEnablementUseCase(integrationStore);
        var deviceOverride = new SetDeviceIntegrationOverrideUseCase(integrationStore);
        var clearOverride = new ClearDeviceIntegrationOverrideUseCase(integrationStore);
        var catalog = new ResolveCapabilityCatalogUseCase(adapters, integrationStore, publicationStore);

        return new ShellViewModel(
            ensureDevice,
            refresh,
            list,
            global,
            deviceOverride,
            clearOverride,
            catalog,
            "iPhone");
    }

    private static AppBuilder BuildAvaloniaApp(ShellViewModel shell, IAvaloniaAppDelegate appDelegate) =>
        AppBuilder.Configure(() => new App(shell))
            .UseiOS(appDelegate)
            .LogToTrace();
}
#endif
