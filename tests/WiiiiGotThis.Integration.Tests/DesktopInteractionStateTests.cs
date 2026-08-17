using System.Numerics;
using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Integrations.Vocation;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class DesktopInteractionStateTests
{
    [Fact]
    public async Task Repeated_product_navigation_remains_mutually_exclusive_and_ready_state_is_quiet()
    {
        var overviewSource = new OverviewSource(new VocationOpportunityOverview(
            "overview-publication",
            new("2026-08-17T06:00:00Z", new DateTimeOffset(2026, 8, 17, 6, 0, 0, TimeSpan.Zero)),
            [new("opportunity", "Platform Engineer", new("company", "Company"), [new("Hamburg", "Hamburg", null, "DE", "city")], BigInteger.One)]));
        var mapSource = new MapSource(new VocationMapProjection(
            "map-publication",
            new("2026-08-17T06:00:00Z", new DateTimeOffset(2026, 8, 17, 6, 0, 0, TimeSpan.Zero)),
            [new("feature", "opportunity", "Platform Engineer", new("company", "Company"), new("Hamburg", "city"), new(53.55, 10.0))]));
        var adapter = new VocationIntegrationAdapter(overviewSource, mapSource);
        var adapters = new StaticIntegrationAdapterCatalog([adapter]);
        var integrations = new MemoryIntegrationStore();
        var publications = new MemoryPublicationStore();
        var shell = new ShellViewModel(
            new EnsureCurrentDeviceUseCase(new MemoryDeviceStore()),
            new RegisterKnownIntegrationsUseCase(adapters, integrations),
            new RefreshPublicationsUseCase(adapters, publications),
            new ListServiceIntegrationsUseCase(integrations, publications),
            new SetGlobalIntegrationEnablementUseCase(integrations),
            new SetDeviceIntegrationOverrideUseCase(integrations),
            new ClearDeviceIntegrationOverrideUseCase(integrations),
            new ResolveCapabilityCatalogUseCase(adapters, integrations, publications),
            "Desktop",
            new GetVocationOpportunityOverviewUseCase(overviewSource),
            new GetVocationMapProjectionUseCase(mapSource));

        await shell.EnsureInitializedAsync();
        Assert.Equal("Ready", shell.StatusText);
        Assert.False(shell.HasStatusMessage);
        AssertSurface(shell, ShellSurface.Home);

        shell.SelectedIntegration = shell.Integrations.Single(item => item.ServiceIdentity.Value == "vocation");
        await shell.EnableGloballyCommand.ExecuteAsync(null);
        Assert.True(shell.IsJobsAvailable);
        Assert.True(shell.IsMapAvailable);

        await shell.ShowJobsCommand.ExecuteAsync(null);
        AssertSurface(shell, ShellSurface.Jobs);
        Assert.Equal("Platform Engineer", shell.OpenedVocationOpportunityOverview!.VisibleOpportunities.Single().Title);

        await shell.ShowMapCommand.ExecuteAsync(null);
        AssertSurface(shell, ShellSurface.Map);
        Assert.Equal("Platform Engineer", shell.OpenedVocationMapProjection!.Features.Single().Title);

        shell.ShowSettingsCommand.Execute(null);
        AssertSurface(shell, ShellSurface.Settings);

        shell.ShowHomeCommand.Execute(null);
        AssertSurface(shell, ShellSurface.Home);

        await shell.ShowJobsCommand.ExecuteAsync(null);
        AssertSurface(shell, ShellSurface.Jobs);
    }

    private static void AssertSurface(ShellViewModel shell, ShellSurface expected)
    {
        Assert.Equal(expected, shell.CurrentSurface);
        Assert.Equal(expected == ShellSurface.Home, shell.IsHomeVisible);
        Assert.Equal(expected == ShellSurface.Jobs, shell.IsJobsVisible);
        Assert.Equal(expected == ShellSurface.Map, shell.IsMapVisible);
        Assert.Equal(expected == ShellSurface.Settings, shell.IsSettingsVisible);
    }

    private sealed class OverviewSource(VocationOpportunityOverview snapshot) : IVocationOpportunityOverviewSource
    {
        public ValueTask<VocationOpportunityOverview> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(snapshot);
        }
    }

    private sealed class MapSource(VocationMapProjection snapshot) : IVocationMapProjectionSource
    {
        public ValueTask<VocationMapProjection> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(snapshot);
        }
    }

    private sealed class MemoryDeviceStore : ILocalDeviceStore
    {
        private LocalDeviceConfiguration? value;

        public ValueTask<LocalDeviceConfiguration?> LoadAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(value);

        public ValueTask SaveAsync(LocalDeviceConfiguration configuration, CancellationToken cancellationToken = default)
        {
            value = configuration;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MemoryIntegrationStore : IServiceIntegrationStore
    {
        private readonly Dictionary<ServiceIdentity, ServiceIntegration> values = [];

        public ValueTask<ServiceIntegration?> LoadAsync(ServiceIdentity id, CancellationToken cancellationToken = default) => ValueTask.FromResult(values.GetValueOrDefault(id));

        public ValueTask<IReadOnlyList<ServiceIntegration>> LoadAllAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<IReadOnlyList<ServiceIntegration>>(values.Values.ToArray());

        public ValueTask SaveAsync(ServiceIntegration integration, CancellationToken cancellationToken = default)
        {
            values[integration.ServiceIdentity] = integration;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MemoryPublicationStore : IIntegrationPublicationStore
    {
        private readonly Dictionary<ServiceIdentity, IntegrationPublicationState> values = [];

        public ValueTask SaveAsync(IntegrationPublicationState state, CancellationToken cancellationToken = default)
        {
            values[state.ServiceIdentity] = state;
            return ValueTask.CompletedTask;
        }

        public ValueTask<IntegrationPublicationState> LoadAsync(ServiceIdentity id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(values.GetValueOrDefault(id) ?? new IntegrationPublicationState(id, null, PublicationRefreshObservation.NotAttempted));
    }
}
