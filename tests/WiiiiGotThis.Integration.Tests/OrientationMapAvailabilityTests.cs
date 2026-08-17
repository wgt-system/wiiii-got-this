using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class OrientationMapAvailabilityTests
{
    [Fact]
    public async Task Map_destination_requires_both_vocation_read_seam_and_orientation_surface()
    {
        var withoutOrientation = CreateShell(isOrientationMapSurfaceComposed: false);
        await withoutOrientation.EnsureInitializedAsync();
        await withoutOrientation.EnableGloballyCommand.ExecuteAsync(null);

        Assert.False(withoutOrientation.IsMapAvailable);
        Assert.False(withoutOrientation.ShowMapCommand.CanExecute(null));
        Assert.Equal(ShellSurface.Home, withoutOrientation.CurrentSurface);

        var withOrientation = CreateShell(isOrientationMapSurfaceComposed: true);
        await withOrientation.EnsureInitializedAsync();
        await withOrientation.EnableGloballyCommand.ExecuteAsync(null);

        Assert.True(withOrientation.IsMapAvailable);
        Assert.True(withOrientation.ShowMapCommand.CanExecute(null));
        await withOrientation.ShowMapCommand.ExecuteAsync(null);
        Assert.Equal(ShellSurface.Map, withOrientation.CurrentSurface);
    }

    private static ShellViewModel CreateShell(bool isOrientationMapSurfaceComposed)
    {
        var adapter = new VocationMapAdapter();
        var adapters = new StaticIntegrationAdapterCatalog([adapter]);
        var integrations = new MemoryIntegrationStore();
        var publications = new MemoryPublicationStore();
        var mapRead = new GetVocationMapProjectionUseCase(new EmptyMapSource());

        return new ShellViewModel(
            new EnsureCurrentDeviceUseCase(new MemoryDeviceStore()),
            new RegisterKnownIntegrationsUseCase(adapters, integrations),
            new RefreshPublicationsUseCase(adapters, publications),
            new ListServiceIntegrationsUseCase(integrations, publications),
            new SetGlobalIntegrationEnablementUseCase(integrations),
            new SetDeviceIntegrationOverrideUseCase(integrations),
            new ClearDeviceIntegrationOverrideUseCase(integrations),
            new ResolveCapabilityCatalogUseCase(adapters, integrations, publications),
            "test-device",
            readVocationMapProjection: mapRead,
            isOrientationMapSurfaceComposed: isOrientationMapSurfaceComposed);
    }

    private sealed class EmptyMapSource : IVocationMapProjectionSource
    {
        public ValueTask<VocationMapProjection> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new VocationMapProjection(
                "publication-test",
                new VocationMapGeneratedAt("2026-08-17T00:00:00Z", DateTimeOffset.Parse("2026-08-17T00:00:00Z")),
                []));
        }
    }

    private sealed class VocationMapAdapter : IIntegrationAdapter
    {
        public ServiceIdentity ServiceId { get; } = new("vocation");

        public ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new ServicePublication(
                ServiceId,
                "Vocation",
                [new CapabilityPublication(new("vocation.map_projection"), "Map Projection", new Version(1, 0))],
                DateTimeOffset.UtcNow));
        }

        public ValueTask<CapabilityResolutionFacts> ObserveCapabilityAsync(
            CapabilityPublication capability,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new CapabilityResolutionFacts(
                ProviderReachability.Reachable,
                ContractCompatibility.Compatible,
                CurrentContextSupport.Supported,
                PrerequisiteState.Satisfied,
                PresentationInvocationSupport.Supported));
        }
    }

    private sealed class MemoryDeviceStore : ILocalDeviceStore
    {
        private LocalDeviceConfiguration? value;
        public ValueTask<LocalDeviceConfiguration?> LoadAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(value);
        public ValueTask SaveAsync(LocalDeviceConfiguration configuration, CancellationToken cancellationToken = default)
        {
            value = configuration;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MemoryIntegrationStore : IServiceIntegrationStore
    {
        private readonly Dictionary<ServiceIdentity, ServiceIntegration> values = [];
        public ValueTask<ServiceIntegration?> LoadAsync(ServiceIdentity id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(values.GetValueOrDefault(id));
        public ValueTask<IReadOnlyList<ServiceIntegration>> LoadAllAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<ServiceIntegration>>(values.Values.ToArray());
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
            ValueTask.FromResult(values.GetValueOrDefault(id)
                ?? new IntegrationPublicationState(id, null, PublicationRefreshObservation.NotAttempted));
    }
}
