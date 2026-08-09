using Avalonia;
using WiiiiGotThis.Application;
using WiiiiGotThis.Infrastructure;
using WiiiiGotThis.Integrations.Reference;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WiiiiGotThis");
        Directory.CreateDirectory(dataDirectory);
        var databasePath = Path.Combine(dataDirectory, "wiiii-got-this.db");
        var connectionFactory = new SqliteConnectionFactory($"Data Source={databasePath}");
        new MigrationRunner(connectionFactory).ApplyAsync().GetAwaiter().GetResult();

        var deviceStore = new SqliteLocalDeviceStore(connectionFactory);
        var integrationStore = new SqliteServiceIntegrationStore(connectionFactory);
        var publicationStore = new SqliteIntegrationPublicationStore(connectionFactory);
        var adapters = new StaticIntegrationAdapterCatalog([new ReferenceIntegrationAdapter()]);
        var ensureDevice = new EnsureCurrentDeviceUseCase(deviceStore);
        var register = new RegisterKnownIntegrationsUseCase(adapters, integrationStore);
        var refresh = new RefreshPublicationsUseCase(adapters, publicationStore);
        var list = new ListServiceIntegrationsUseCase(integrationStore, publicationStore);
        var global = new SetGlobalIntegrationEnablementUseCase(integrationStore);
        var deviceOverride = new SetDeviceIntegrationOverrideUseCase(integrationStore);
        var clearOverride = new ClearDeviceIntegrationOverrideUseCase(integrationStore);
        var catalog = new ResolveCapabilityCatalogUseCase(adapters, integrationStore, publicationStore);
        var shell = new ShellViewModel(ensureDevice, register, refresh, list, global, deviceOverride, clearOverride, catalog, "Windows PC");

        BuildAvaloniaApp(shell).StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp(ShellViewModel shell) => AppBuilder.Configure(() => new App(shell)).UsePlatformDetect().LogToTrace();
}
