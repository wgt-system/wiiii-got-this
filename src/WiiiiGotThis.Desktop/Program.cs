using Avalonia;
using WiiiiGotThis.Application;
using WiiiiGotThis.Infrastructure;
using WiiiiGotThis.Integrations.Reference;
using WiiiiGotThis.Integrations.Vocation;
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
        var vocationHttpClient = new HttpClient();
        var vocationSource = new VocationHttpOpportunityOverviewSource(vocationHttpClient);
        var vocationMapSource = new VocationHttpMapProjectionSource(vocationHttpClient);
        var adapters = new StaticIntegrationAdapterCatalog([
            new ReferenceIntegrationAdapter(),
            new VocationIntegrationAdapter(vocationSource, vocationMapSource)]);
        var ensureDevice = new EnsureCurrentDeviceUseCase(deviceStore);
        var register = new RegisterKnownIntegrationsUseCase(adapters, integrationStore);
        var refresh = new RefreshPublicationsUseCase(adapters, publicationStore);
        var list = new ListServiceIntegrationsUseCase(integrationStore, publicationStore);
        var global = new SetGlobalIntegrationEnablementUseCase(integrationStore);
        var deviceOverride = new SetDeviceIntegrationOverrideUseCase(integrationStore);
        var clearOverride = new ClearDeviceIntegrationOverrideUseCase(integrationStore);
        var catalog = new ResolveCapabilityCatalogUseCase(adapters, integrationStore, publicationStore);
        var readVocationOpportunityOverview = new GetVocationOpportunityOverviewUseCase(vocationSource);
        var readVocationMapProjection = new GetVocationMapProjectionUseCase(vocationMapSource);
        var shell = new ShellViewModel(
            ensureDevice,
            register,
            refresh,
            list,
            global,
            deviceOverride,
            clearOverride,
            catalog,
            "Windows PC",
            readVocationOpportunityOverview,
            readVocationMapProjection,
            isOrientationMapSurfaceComposed: IsOrientationMapSurfaceComposed());

        BuildAvaloniaApp(shell).StartWithClassicDesktopLifetime(args);
    }

    private static bool IsOrientationMapSurfaceComposed()
    {
        var configuredPath = Environment.GetEnvironmentVariable("WGT_ORIENTATION_EMBED_PATH");
        var embedPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "orientation-map", "embed.html")
            : configuredPath.Trim();

        try
        {
            return File.Exists(Path.GetFullPath(embedPath));
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static AppBuilder BuildAvaloniaApp(ShellViewModel shell) => AppBuilder.Configure(() => new App(shell)).UsePlatformDetect().LogToTrace();
}
