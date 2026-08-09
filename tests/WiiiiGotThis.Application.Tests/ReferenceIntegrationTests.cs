using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Integrations.Reference;

namespace WiiiiGotThis.Application.Tests;

public sealed class ReferenceIntegrationTests
{
    [Fact]
    public void Local_device_configuration_validates_and_trims_only_its_display_name()
    {
        var identity = DeviceIdentity.New();
        var configuration = new LocalDeviceConfiguration(identity, "  Windows PC  ");
        Assert.Equal(identity, configuration.DeviceIdentity);
        Assert.Equal("Windows PC", configuration.DisplayName);
        Assert.Throws<ArgumentException>(() => new LocalDeviceConfiguration(identity, " \t "));
        Assert.Throws<ArgumentNullException>(() => new LocalDeviceConfiguration(null!, "name"));
    }

    [Fact]
    public async Task Reference_publication_is_stable_and_ordered()
    {
        var publication = await new ReferenceIntegrationAdapter().GetPublicationAsync();
        Assert.Equal(ReferenceIntegrationAdapter.StableServiceIdentity, publication.ServiceId);
        Assert.Equal(4, publication.Capabilities.Count);
        Assert.Equal(["reference.available", "reference.unsupported", "reference.unavailable", "reference.version-mismatch"], publication.Capabilities.Select(x => x.Id.Value));
        Assert.Equal(new Version(99, 0), publication.Capabilities[3].ContractVersion);
        Assert.All(publication.Capabilities.Take(3), x => Assert.Equal(new Version(1, 0), x.ContractVersion));
        Assert.Equal(4, publication.Capabilities.Select(x => x.Id).Distinct().Count());
    }

    [Fact]
    public async Task Reference_observations_resolve_to_the_four_expected_states()
    {
        var adapter = new ReferenceIntegrationAdapter();
        var publication = await adapter.GetPublicationAsync();
        var integration = new ServiceIntegration(adapter.ServiceId); integration.EnableGlobally();
        var device = DeviceIdentity.New();
        var reasons = new List<AvailabilityReason?>();
        foreach (var capability in publication.Capabilities)
        {
            var facts = await adapter.ObserveCapabilityAsync(capability);
            reasons.Add(CapabilityResolver.Resolve(integration, device, new(adapter.ServiceId, capability.Id), facts).Availability.Reason);
        }
        Assert.Equal([null, AvailabilityReason.Unsupported, AvailabilityReason.Unreachable, AvailabilityReason.Incompatible], reasons);
    }

    [Fact]
    public async Task Reference_version_compatibility_depends_on_the_published_version()
    {
        var adapter = new ReferenceIntegrationAdapter();
        var normal = new CapabilityPublication(new("reference.available"), "Available", new(1, 0));
        var mismatch = normal with { ContractVersion = new Version(2, 0) };
        Assert.Equal(ContractCompatibility.Compatible, (await adapter.ObserveCapabilityAsync(normal)).ContractCompatibility);
        Assert.Equal(ContractCompatibility.Incompatible, (await adapter.ObserveCapabilityAsync(mismatch)).ContractCompatibility);
    }

    [Fact]
    public void Static_catalog_rejects_null_and_duplicate_adapters_deterministically()
    {
        var adapter = new ReferenceIntegrationAdapter();
        Assert.Throws<ArgumentNullException>(() => new StaticIntegrationAdapterCatalog([null!]));
        Assert.Throws<ArgumentException>(() => new StaticIntegrationAdapterCatalog([adapter, adapter]));
        Assert.Same(adapter, new StaticIntegrationAdapterCatalog([adapter]).Adapters[0]);
    }

    [Fact]
    public async Task Refresh_registers_without_enabling_and_preserves_configuration()
    {
        var adapter = new ReferenceIntegrationAdapter();
        var integrations = new MemoryIntegrationStore();
        var publications = new MemoryPublicationStore();
        var device = DeviceIdentity.New();
        var configured = new ServiceIntegration(adapter.ServiceId); configured.EnableGlobally(); configured.SetDeviceOverride(device, Enablement.Disabled);
        await integrations.SaveAsync(configured);
        var result = await Refresh(adapter, integrations, publications);
        Assert.Equal(IntegrationRefreshStatus.Refreshed, result.Single().Status);
        var loaded = await integrations.LoadAsync(adapter.ServiceId);
        Assert.Equal(Enablement.Enabled, loaded!.GlobalEnablement);
        Assert.Equal(Enablement.Disabled, loaded.GetEffectiveEnablement(device));
        Assert.Equal(4, (await publications.LoadAsync(adapter.ServiceId))!.Capabilities.Count);
    }

    [Fact]
    public async Task Invalid_publication_and_adapter_failure_are_isolated()
    {
        var invalid = new TestAdapter(new("service-a"), new ServicePublication(new("service-b"), "Wrong", [], DateTimeOffset.UtcNow));
        var failing = new TestAdapter(new("service-failing"), publicationException: new InvalidOperationException());
        var healthy = new ReferenceIntegrationAdapter();
        var integrations = new MemoryIntegrationStore(); var publications = new MemoryPublicationStore();
        var results = await new RefreshPublicationsUseCase(new StaticIntegrationAdapterCatalog([invalid, failing, healthy]), integrations, publications).RefreshAsync();
        Assert.Equal([IntegrationRefreshStatus.InvalidPublication, IntegrationRefreshStatus.AdapterFailed, IntegrationRefreshStatus.Refreshed], results.Select(x => x.Status));
        Assert.Null(await publications.LoadAsync(invalid.ServiceId));
        Assert.NotNull(await publications.LoadAsync(healthy.ServiceId));
    }

    [Fact]
    public async Task Failed_refresh_does_not_delete_last_successful_snapshot()
    {
        var identity = new ServiceIdentity("service-a");
        var publications = new MemoryPublicationStore(); var integrations = new MemoryIntegrationStore();
        var adapter = new TestAdapter(identity, new ServicePublication(identity, "First", [], DateTimeOffset.UtcNow));
        await Refresh(adapter, integrations, publications);
        adapter.PublicationException = new InvalidOperationException();
        Assert.Equal(IntegrationRefreshStatus.AdapterFailed, (await Refresh(adapter, integrations, publications)).Single().Status);
        Assert.Equal("First", (await publications.LoadAsync(identity))!.DisplayName);
    }

    [Fact]
    public async Task Cancellation_is_propagated()
    {
        var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => Refresh(new ReferenceIntegrationAdapter(), new MemoryIntegrationStore(), new MemoryPublicationStore(), cts.Token).AsTask());
    }

    [Fact]
    public async Task Catalog_resolves_in_registration_and_publication_order_and_skips_disabled_observation()
    {
        var adapter = new CountingAdapter(new ReferenceIntegrationAdapter());
        var integrations = new MemoryIntegrationStore(); var publications = new MemoryPublicationStore();
        await Refresh(adapter, integrations, publications);
        var entries = await new ResolveCapabilityCatalogUseCase(new StaticIntegrationAdapterCatalog([adapter]), integrations, publications).ResolveAsync(DeviceIdentity.New());
        Assert.Equal(4, entries.Count); Assert.Equal(0, adapter.ObservationCount); Assert.All(entries, x => Assert.Equal(AvailabilityReason.Disabled, x.Resolution.Availability.Reason));
        var integration = await integrations.LoadAsync(adapter.ServiceId); integration!.EnableGlobally(); await integrations.SaveAsync(integration);
        entries = await new ResolveCapabilityCatalogUseCase(new StaticIntegrationAdapterCatalog([adapter]), integrations, publications).ResolveAsync(DeviceIdentity.New());
        Assert.Equal([null, AvailabilityReason.Unsupported, AvailabilityReason.Unreachable, AvailabilityReason.Incompatible], entries.Select(x => x.Resolution.Availability.Reason));
    }

    [Fact]
    public async Task Observation_failure_becomes_unknown_without_blocking_other_adapter()
    {
        var failing = new CountingAdapter(new ReferenceIntegrationAdapter()) { ThrowOnObservation = true };
        var healthyIdentity = new ServiceIdentity("healthy-service");
        var healthy = new TestAdapter(healthyIdentity, new ServicePublication(healthyIdentity, "Healthy", [new(new("healthy-capability"), "Healthy", new(1, 0))], DateTimeOffset.UtcNow));
        var integrations = new MemoryIntegrationStore(); var publications = new MemoryPublicationStore();
        await Refresh(failing, integrations, publications); await Refresh(healthy, integrations, publications);
        foreach (var integration in await integrations.LoadAllAsync()) { integration.EnableGlobally(); await integrations.SaveAsync(integration); }
        var entries = await new ResolveCapabilityCatalogUseCase(new StaticIntegrationAdapterCatalog([failing, healthy]), integrations, publications).ResolveAsync(DeviceIdentity.New());
        Assert.All(entries.Take(4), x => Assert.Equal(AvailabilityReason.Unknown, x.Resolution.Availability.Reason));
        Assert.Null(entries[4].Resolution.Availability.Reason);
    }

    private static ValueTask<IReadOnlyList<IntegrationRefreshResult>> Refresh(IIntegrationAdapter adapter, MemoryIntegrationStore integrations, MemoryPublicationStore publications, CancellationToken token = default) => new RefreshPublicationsUseCase(new StaticIntegrationAdapterCatalog([adapter]), integrations, publications).RefreshAsync(token);

    private sealed class MemoryIntegrationStore : IServiceIntegrationStore
    {
        private readonly Dictionary<ServiceIdentity, ServiceIntegration> values = [];
        public ValueTask<ServiceIntegration?> LoadAsync(ServiceIdentity id, CancellationToken cancellationToken = default) => ValueTask.FromResult(values.GetValueOrDefault(id));
        public ValueTask<IReadOnlyList<ServiceIntegration>> LoadAllAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<IReadOnlyList<ServiceIntegration>>(values.Values.ToArray());
        public ValueTask SaveAsync(ServiceIntegration integration, CancellationToken cancellationToken = default) { values[integration.ServiceIdentity] = integration; return ValueTask.CompletedTask; }
    }
    private sealed class MemoryPublicationStore : IIntegrationPublicationStore
    {
        private readonly Dictionary<ServiceIdentity, ServicePublication> values = [];
        public ValueTask SaveAsync(ServicePublication publication, CancellationToken cancellationToken = default) { values[publication.ServiceId] = publication; return ValueTask.CompletedTask; }
        public ValueTask<ServicePublication?> LoadAsync(ServiceIdentity id, CancellationToken cancellationToken = default) => ValueTask.FromResult(values.GetValueOrDefault(id));
    }
    private sealed class TestAdapter(ServiceIdentity serviceId, ServicePublication? publication = null, Exception? publicationException = null) : IIntegrationAdapter
    {
        public ServiceIdentity ServiceId { get; } = serviceId; public ServicePublication? Publication { get; } = publication; public Exception? PublicationException { get; set; } = publicationException;
        public ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default) { cancellationToken.ThrowIfCancellationRequested(); if (PublicationException is not null) throw PublicationException; return ValueTask.FromResult(Publication ?? new ServicePublication(ServiceId, "Test", [], DateTimeOffset.UtcNow)); }
        public ValueTask<CapabilityResolutionFacts> ObserveCapabilityAsync(CapabilityPublication capability, CancellationToken cancellationToken = default) => ValueTask.FromResult(new CapabilityResolutionFacts(ProviderReachability.Reachable, ContractCompatibility.Compatible, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported));
    }
    private sealed class CountingAdapter(IIntegrationAdapter inner) : IIntegrationAdapter
    {
        public ServiceIdentity ServiceId => inner.ServiceId; public int ObservationCount { get; private set; } public bool ThrowOnObservation { get; set; }
        public ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default) => inner.GetPublicationAsync(cancellationToken);
        public ValueTask<CapabilityResolutionFacts> ObserveCapabilityAsync(CapabilityPublication capability, CancellationToken cancellationToken = default) { ObservationCount++; if (ThrowOnObservation) throw new InvalidOperationException(); return inner.ObserveCapabilityAsync(capability, cancellationToken); }
    }
}
