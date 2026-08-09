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
    public async Task Registration_creates_disabled_integrations_without_contacting_providers()
    {
        var failing = new TestAdapter(new("service-a"), publicationException: new InvalidOperationException());
        var healthy = new TestAdapter(new("service-b"));
        var integrations = new MemoryIntegrationStore();
        await new RegisterKnownIntegrationsUseCase(new StaticIntegrationAdapterCatalog([failing, healthy]), integrations).RegisterAsync();

        Assert.Equal(Enablement.Disabled, (await integrations.LoadAsync(failing.ServiceId))!.GlobalEnablement);
        Assert.Equal(Enablement.Disabled, (await integrations.LoadAsync(healthy.ServiceId))!.GlobalEnablement);
        Assert.Equal(0, failing.PublicationCalls + healthy.PublicationCalls);
        Assert.Equal(0, failing.ObservationCalls + healthy.ObservationCalls);

        var refreshResults = await new RefreshPublicationsUseCase(
            new StaticIntegrationAdapterCatalog([failing, healthy]),
            new MemoryPublicationStore()).RefreshAsync();
        Assert.Equal([IntegrationRefreshStatus.AdapterFailed, IntegrationRefreshStatus.Refreshed], refreshResults.Select(result => result.Status));
        Assert.NotNull(await integrations.LoadAsync(failing.ServiceId));
        Assert.NotNull(await integrations.LoadAsync(healthy.ServiceId));
    }

    [Fact]
    public async Task Registration_is_idempotent_and_preserves_enablement_and_device_overrides()
    {
        var adapter = new TestAdapter(new("configured-service"));
        var device = DeviceIdentity.New();
        var integration = new ServiceIntegration(adapter.ServiceId);
        integration.EnableGlobally(); integration.SetDeviceOverride(device, Enablement.Disabled);
        var integrations = new MemoryIntegrationStore(); await integrations.SaveAsync(integration);

        var registration = new RegisterKnownIntegrationsUseCase(new StaticIntegrationAdapterCatalog([adapter]), integrations);
        await registration.RegisterAsync(); await registration.RegisterAsync();
        var preserved = await integrations.LoadAsync(adapter.ServiceId);
        Assert.Equal(Enablement.Enabled, preserved!.GlobalEnablement);
        Assert.Equal(Enablement.Disabled, preserved.GetEffectiveEnablement(device));
        Assert.Equal(0, adapter.PublicationCalls);
    }

    [Fact]
    public async Task Publication_refresh_alone_does_not_register_an_integration()
    {
        var adapter = new TestAdapter(new("refresh-only-service"));
        var integrations = new MemoryIntegrationStore(); var publications = new MemoryPublicationStore();
        Assert.Equal(IntegrationRefreshStatus.Refreshed, (await new RefreshPublicationsUseCase(new StaticIntegrationAdapterCatalog([adapter]), publications).RefreshAsync()).Single().Status);
        Assert.Null(await integrations.LoadAsync(adapter.ServiceId));
    }

    [Fact]
    public async Task Registration_cancellation_is_propagated()
    {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => new RegisterKnownIntegrationsUseCase(
            new StaticIntegrationAdapterCatalog([new TestAdapter(new("cancelled-service"))]),
            new MemoryIntegrationStore()).RegisterAsync(cancellation.Token).AsTask());
    }

    [Fact]
    public async Task Refresh_preserves_existing_configuration_without_registering()
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
        Assert.Equal(4, (await publications.LoadAsync(adapter.ServiceId)).Publication!.Capabilities.Count);
    }

    [Fact]
    public async Task Invalid_publication_and_adapter_failure_are_isolated()
    {
        var invalid = new TestAdapter(new("service-a"), new ServicePublication(new("service-b"), "Wrong", [], DateTimeOffset.UtcNow));
        var failing = new TestAdapter(new("service-failing"), publicationException: new InvalidOperationException());
        var healthy = new ReferenceIntegrationAdapter();
        var integrations = new MemoryIntegrationStore(); var publications = new MemoryPublicationStore();
        var results = await new RefreshPublicationsUseCase(new StaticIntegrationAdapterCatalog([invalid, failing, healthy]), publications).RefreshAsync();
        Assert.Equal([IntegrationRefreshStatus.InvalidPublication, IntegrationRefreshStatus.AdapterFailed, IntegrationRefreshStatus.Refreshed], results.Select(x => x.Status));
        Assert.Null((await publications.LoadAsync(invalid.ServiceId)).Publication);
        Assert.NotNull((await publications.LoadAsync(healthy.ServiceId)).Publication);
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
        Assert.Equal("First", (await publications.LoadAsync(identity)).Publication!.DisplayName);
    }

    [Fact]
    public async Task First_success_persists_snapshot_and_refresh_observation()
    {
        var service = new ServiceIdentity("successful-service");
        var at = new DateTimeOffset(2026, 8, 9, 10, 0, 0, TimeSpan.Zero);
        var publication = new ServicePublication(service, "Published", [], at.AddMinutes(-1));
        var store = new MemoryPublicationStore();
        var result = await new RefreshPublicationsUseCase(new StaticIntegrationAdapterCatalog([new TestAdapter(service, publication)]), store, new FixedTimeProvider(at)).RefreshAsync();
        var state = await store.LoadAsync(service);
        Assert.Equal(IntegrationRefreshStatus.Refreshed, result.Single().Status);
        Assert.Equal("Published", state.Publication!.DisplayName);
        Assert.Equal(at, state.RefreshObservation.LastAttemptedAtUtc);
        Assert.Equal(at, state.RefreshObservation.LastSuccessfulRefreshAtUtc);
    }

    [Fact]
    public async Task First_failures_persist_bounded_metadata_without_publication()
    {
        var service = new ServiceIdentity("failed-service"); var at = new DateTimeOffset(2026, 8, 9, 11, 0, 0, TimeSpan.Zero); var store = new MemoryPublicationStore();
        var adapter = new TestAdapter(service, publicationException: new InvalidOperationException());
        var useCase = new RefreshPublicationsUseCase(new StaticIntegrationAdapterCatalog([adapter]), store, new FixedTimeProvider(at));
        Assert.Equal(IntegrationRefreshStatus.AdapterFailed, (await useCase.RefreshAsync()).Single().Status);
        var state = await store.LoadAsync(service); Assert.Null(state.Publication); Assert.True(state.RefreshObservation.HasAttempted); Assert.Equal(IntegrationRefreshStatus.AdapterFailed, state.RefreshObservation.LatestResult); Assert.Null(state.RefreshObservation.LastSuccessfulRefreshAtUtc);

        var invalidService = new ServiceIdentity("invalid-service"); var invalidStore = new MemoryPublicationStore();
        var invalid = new TestAdapter(invalidService, new ServicePublication(new("other"), "Invalid", [], at));
        var invalidUseCase = new RefreshPublicationsUseCase(new StaticIntegrationAdapterCatalog([invalid]), invalidStore, new FixedTimeProvider(at));
        Assert.Equal(IntegrationRefreshStatus.InvalidPublication, (await invalidUseCase.RefreshAsync()).Single().Status);
        var invalidState = await invalidStore.LoadAsync(invalidService); Assert.Null(invalidState.Publication); Assert.Equal(IntegrationRefreshStatus.InvalidPublication, invalidState.RefreshObservation.LatestResult);
    }

    [Fact]
    public async Task Failed_and_invalid_refreshes_retain_snapshot_and_last_success_time()
    {
        var service = new ServiceIdentity("retained-service"); var firstAt = new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero); var failedAt = firstAt.AddHours(1); var store = new MemoryPublicationStore();
        var capability = new CapabilityPublication(new("capability"), "Original", new(1, 0)); var adapter = new TestAdapter(service, new ServicePublication(service, "First", [capability], firstAt));
        var clock = new FixedTimeProvider(firstAt, failedAt, failedAt.AddHours(1)); var useCase = new RefreshPublicationsUseCase(new StaticIntegrationAdapterCatalog([adapter]), store, clock);
        await useCase.RefreshAsync(); adapter.PublicationException = new InvalidOperationException(); await useCase.RefreshAsync();
        var afterFailure = await store.LoadAsync(service); Assert.Equal("First", afterFailure.Publication!.DisplayName); Assert.Equal("capability", afterFailure.Publication.Capabilities.Single().Id.Value); Assert.Equal(firstAt, afterFailure.RefreshObservation.LastSuccessfulRefreshAtUtc);
        adapter.PublicationException = null; adapter.Publication = new ServicePublication(new("other"), "Invalid", [], failedAt); await useCase.RefreshAsync();
        var afterInvalid = await store.LoadAsync(service); Assert.Equal("First", afterInvalid.Publication!.DisplayName); Assert.Equal(firstAt, afterInvalid.RefreshObservation.LastSuccessfulRefreshAtUtc);
    }

    [Fact]
    public async Task Successful_refresh_reconciles_added_removed_title_and_version_changes()
    {
        var service = new ServiceIdentity("reconciled-service"); var firstAt = new DateTimeOffset(2026, 8, 9, 13, 0, 0, TimeSpan.Zero); var secondAt = firstAt.AddHours(1); var store = new MemoryPublicationStore();
        var adapter = new TestAdapter(service, new ServicePublication(service, "First", [new(new("a"), "A", new(1, 0)), new(new("removed"), "Removed", new(1, 0))], firstAt));
        var useCase = new RefreshPublicationsUseCase(new StaticIntegrationAdapterCatalog([adapter]), store, new FixedTimeProvider(firstAt, secondAt)); await useCase.RefreshAsync();
        adapter.Publication = new ServicePublication(service, "Second", [new(new("a"), "Changed", new(2, 0)), new(new("b"), "Added", new(1, 0))], secondAt); await useCase.RefreshAsync();
        var publication = (await store.LoadAsync(service)).Publication!; Assert.Equal(["a", "b"], publication.Capabilities.Select(x => x.Id.Value)); Assert.Equal("Changed", publication.Capabilities[0].Title); Assert.Equal(new Version(2, 0), publication.Capabilities[0].ContractVersion); Assert.Equal(secondAt, (await store.LoadAsync(service)).RefreshObservation.LastSuccessfulRefreshAtUtc);
    }

    [Fact]
    public async Task Refresh_failure_for_one_service_does_not_affect_another()
    {
        var first = new ServiceIdentity("first-service"); var second = new ServiceIdentity("second-service"); var at = new DateTimeOffset(2026, 8, 9, 14, 0, 0, TimeSpan.Zero); var store = new MemoryPublicationStore();
        var useCase = new RefreshPublicationsUseCase(new StaticIntegrationAdapterCatalog([new TestAdapter(first, publicationException: new InvalidOperationException()), new TestAdapter(second, new ServicePublication(second, "Healthy", [], at))]), store, new FixedTimeProvider(at, at));
        var results = await useCase.RefreshAsync(); Assert.Equal([IntegrationRefreshStatus.AdapterFailed, IntegrationRefreshStatus.Refreshed], results.Select(x => x.Status)); Assert.Equal("Healthy", (await store.LoadAsync(second)).Publication!.DisplayName); Assert.Equal(IntegrationRefreshStatus.AdapterFailed, (await store.LoadAsync(first)).RefreshObservation.LatestResult);
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
        await Register(adapter, integrations);
        await Refresh(adapter, integrations, publications);
        var entries = await new ResolveCapabilityCatalogUseCase(new StaticIntegrationAdapterCatalog([adapter]), integrations, publications).ResolveAsync(DeviceIdentity.New());
        Assert.Equal(4, entries.Count); Assert.Equal(0, adapter.ObservationCount); Assert.All(entries, x => Assert.Equal(AvailabilityReason.Disabled, x.Resolution.Availability.Reason));
        var integration = await integrations.LoadAsync(adapter.ServiceId); integration!.EnableGlobally(); await integrations.SaveAsync(integration);
        entries = await new ResolveCapabilityCatalogUseCase(new StaticIntegrationAdapterCatalog([adapter]), integrations, publications).ResolveAsync(DeviceIdentity.New());
        Assert.Equal([null, AvailabilityReason.Unsupported, AvailabilityReason.Unreachable, AvailabilityReason.Incompatible], entries.Select(x => x.Resolution.Availability.Reason));
    }

    [Fact]
    public async Task Catalog_respects_device_override_and_observes_only_the_enabled_device()
    {
        var adapter = new CountingAdapter(new ReferenceIntegrationAdapter());
        var integrations = new MemoryIntegrationStore(); var publications = new MemoryPublicationStore();
        var deviceA = DeviceIdentity.New(); var deviceB = DeviceIdentity.New();
        var integration = new ServiceIntegration(adapter.ServiceId);
        integration.EnableGlobally(); integration.SetDeviceOverride(deviceA, Enablement.Disabled);
        await integrations.SaveAsync(integration);
        await Refresh(adapter, integrations, publications);
        var catalog = new ResolveCapabilityCatalogUseCase(new StaticIntegrationAdapterCatalog([adapter]), integrations, publications);

        var deviceAEntries = await catalog.ResolveAsync(deviceA);
        Assert.Equal(4, deviceAEntries.Count);
        Assert.All(deviceAEntries, entry => Assert.Equal(AvailabilityReason.Disabled, entry.Resolution.Availability.Reason));
        Assert.Equal(0, adapter.ObservationCount);

        var deviceBEntries = await catalog.ResolveAsync(deviceB);
        Assert.Equal(["reference.available", "reference.unsupported", "reference.unavailable", "reference.version-mismatch"], deviceBEntries.Select(entry => entry.CapabilityIdentity.Value));
        Assert.True(deviceBEntries[0].Resolution.Availability.IsAvailable);
        Assert.Equal([AvailabilityReason.Unsupported, AvailabilityReason.Unreachable, AvailabilityReason.Incompatible], deviceBEntries.Skip(1).Select(entry => entry.Resolution.Availability.Reason));
        Assert.Equal(4, adapter.ObservationCount);
    }

    [Fact]
    public async Task Successful_refresh_replaces_publication_without_changing_integration_configuration()
    {
        var service = new ServiceIdentity("replaceable-service");
        var deviceA = DeviceIdentity.New();
        var integration = new ServiceIntegration(service);
        integration.EnableGlobally(); integration.SetDeviceOverride(deviceA, Enablement.Disabled);
        var integrations = new MemoryIntegrationStore(); var publications = new MemoryPublicationStore();
        await integrations.SaveAsync(integration);
        var firstCapability = new CapabilityPublication(new("capability-a"), "Capability A", new(1, 0));
        var secondCapability = new CapabilityPublication(new("capability-b"), "Capability B", new(1, 0));
        var adapter = new TestAdapter(service, new ServicePublication(service, "First", [firstCapability], DateTimeOffset.UtcNow));
        Assert.Equal(IntegrationRefreshStatus.Refreshed, (await Refresh(adapter, integrations, publications)).Single().Status);

        adapter.Publication = new ServicePublication(service, "Second", [secondCapability], DateTimeOffset.UtcNow.AddMinutes(1));
        var secondResult = (await Refresh(adapter, integrations, publications)).Single();
        var snapshot = (await publications.LoadAsync(service)).Publication;
        var preservedIntegration = await integrations.LoadAsync(service);
        Assert.Equal(IntegrationRefreshStatus.Refreshed, secondResult.Status);
        Assert.Equal("Second", snapshot!.DisplayName);
        Assert.DoesNotContain(snapshot.Capabilities, capability => capability.Id == firstCapability.Id);
        Assert.Contains(snapshot.Capabilities, capability => capability.Id == secondCapability.Id);
        Assert.Equal(Enablement.Enabled, preservedIntegration!.GlobalEnablement);
        Assert.Equal(Enablement.Disabled, preservedIntegration.GetEffectiveEnablement(deviceA));
    }

    [Fact]
    public async Task Observation_failure_becomes_unknown_without_blocking_other_adapter()
    {
        var failing = new CountingAdapter(new ReferenceIntegrationAdapter()) { ThrowOnObservation = true };
        var healthyIdentity = new ServiceIdentity("healthy-service");
        var healthy = new TestAdapter(healthyIdentity, new ServicePublication(healthyIdentity, "Healthy", [new(new("healthy-capability"), "Healthy", new(1, 0))], DateTimeOffset.UtcNow));
        var integrations = new MemoryIntegrationStore(); var publications = new MemoryPublicationStore();
        await Register(failing, integrations); await Register(healthy, integrations);
        await Refresh(failing, integrations, publications); await Refresh(healthy, integrations, publications);
        foreach (var integration in await integrations.LoadAllAsync()) { integration.EnableGlobally(); await integrations.SaveAsync(integration); }
        var entries = await new ResolveCapabilityCatalogUseCase(new StaticIntegrationAdapterCatalog([failing, healthy]), integrations, publications).ResolveAsync(DeviceIdentity.New());
        Assert.All(entries.Take(4), x => Assert.Equal(AvailabilityReason.Unknown, x.Resolution.Availability.Reason));
        Assert.Null(entries[4].Resolution.Availability.Reason);
    }

    private static ValueTask<IReadOnlyList<IntegrationRefreshResult>> Refresh(IIntegrationAdapter adapter, MemoryIntegrationStore integrations, MemoryPublicationStore publications, CancellationToken token = default) => new RefreshPublicationsUseCase(new StaticIntegrationAdapterCatalog([adapter]), publications).RefreshAsync(token);
    private static ValueTask Register(IIntegrationAdapter adapter, MemoryIntegrationStore integrations, CancellationToken token = default) => new RegisterKnownIntegrationsUseCase(new StaticIntegrationAdapterCatalog([adapter]), integrations).RegisterAsync(token);

    private sealed class MemoryIntegrationStore : IServiceIntegrationStore
    {
        private readonly Dictionary<ServiceIdentity, ServiceIntegration> values = [];
        public ValueTask<ServiceIntegration?> LoadAsync(ServiceIdentity id, CancellationToken cancellationToken = default) => ValueTask.FromResult(values.GetValueOrDefault(id));
        public ValueTask<IReadOnlyList<ServiceIntegration>> LoadAllAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<IReadOnlyList<ServiceIntegration>>(values.Values.ToArray());
        public ValueTask SaveAsync(ServiceIntegration integration, CancellationToken cancellationToken = default) { values[integration.ServiceIdentity] = integration; return ValueTask.CompletedTask; }
    }
    private sealed class MemoryPublicationStore : IIntegrationPublicationStore
    {
        private readonly Dictionary<ServiceIdentity, IntegrationPublicationState> values = [];
        public ValueTask SaveAsync(IntegrationPublicationState state, CancellationToken cancellationToken = default) { values[state.ServiceIdentity] = state; return ValueTask.CompletedTask; }
        public ValueTask<IntegrationPublicationState> LoadAsync(ServiceIdentity id, CancellationToken cancellationToken = default) => ValueTask.FromResult(values.GetValueOrDefault(id) ?? new IntegrationPublicationState(id, null, PublicationRefreshObservation.NotAttempted));
    }
    private sealed class TestAdapter(ServiceIdentity serviceId, ServicePublication? publication = null, Exception? publicationException = null) : IIntegrationAdapter
    {
        public ServiceIdentity ServiceId { get; } = serviceId; public ServicePublication? Publication { get; set; } = publication; public Exception? PublicationException { get; set; } = publicationException;
        public int PublicationCalls { get; private set; } public int ObservationCalls { get; private set; }
        public ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default) { PublicationCalls++; cancellationToken.ThrowIfCancellationRequested(); if (PublicationException is not null) throw PublicationException; return ValueTask.FromResult(Publication ?? new ServicePublication(ServiceId, "Test", [], DateTimeOffset.UtcNow)); }
        public ValueTask<CapabilityResolutionFacts> ObserveCapabilityAsync(CapabilityPublication capability, CancellationToken cancellationToken = default) { ObservationCalls++; return ValueTask.FromResult(new CapabilityResolutionFacts(ProviderReachability.Reachable, ContractCompatibility.Compatible, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported)); }
    }
    private sealed class CountingAdapter(IIntegrationAdapter inner) : IIntegrationAdapter
    {
        public ServiceIdentity ServiceId => inner.ServiceId; public int ObservationCount { get; private set; } public bool ThrowOnObservation { get; set; }
        public ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default) => inner.GetPublicationAsync(cancellationToken);
        public ValueTask<CapabilityResolutionFacts> ObserveCapabilityAsync(CapabilityPublication capability, CancellationToken cancellationToken = default) { ObservationCount++; if (ThrowOnObservation) throw new InvalidOperationException(); return inner.ObserveCapabilityAsync(capability, cancellationToken); }
    }

    private sealed class FixedTimeProvider(params DateTimeOffset[] times) : TimeProvider
    {
        private int index;
        public override DateTimeOffset GetUtcNow() => times[Math.Min(index++, times.Length - 1)];
    }
}
