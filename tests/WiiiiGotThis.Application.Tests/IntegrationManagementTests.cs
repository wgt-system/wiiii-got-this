using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Application.Tests;

public sealed class IntegrationManagementTests
{
    [Fact]
    public async Task Ensure_current_device_creates_once_and_preserves_identity_and_name()
    {
        var store = new MemoryDeviceStore();
        var useCase = new EnsureCurrentDeviceUseCase(store);
        var first = await useCase.GetOrCreateAsync("  Windows PC  ");
        var second = await useCase.GetOrCreateAsync("New name");
        Assert.Equal(first.DeviceIdentity, second.DeviceIdentity);
        Assert.Equal("Windows PC", second.DisplayName);
    }

    [Fact]
    public async Task Integration_list_maps_global_override_effective_state_and_fallback_name()
    {
        var device = DeviceIdentity.New();
        var service = new ServiceIdentity("service");
        var integration = new ServiceIntegration(service); integration.EnableGlobally(); integration.SetDeviceOverride(device, Enablement.Disabled);
        var integrations = new MemoryIntegrationStore(integration);
        var publications = new MemoryPublicationStore(new IntegrationPublicationState(service, new ServicePublication(service, "Published name", [], DateTimeOffset.UtcNow), PublicationRefreshObservation.NotAttempted));
        var item = (await new ListServiceIntegrationsUseCase(integrations, publications).ListAsync(device)).Single();
        Assert.True(item.IsGloballyEnabled); Assert.False(item.CurrentDeviceOverride); Assert.False(item.IsEffectivelyEnabled); Assert.Equal("Published name", item.DisplayName);

        var fallback = new ServiceIdentity("fallback"); await integrations.SaveAsync(new ServiceIntegration(fallback));
        var fallbackItem = (await new ListServiceIntegrationsUseCase(integrations, publications).ListAsync(device)).Single(x => x.ServiceIdentity == fallback);
        Assert.Equal("fallback", fallbackItem.DisplayName); Assert.Null(fallbackItem.CurrentDeviceOverride);
    }

    [Fact]
    public async Task Global_commands_preserve_device_overrides_and_unknown_services_are_rejected()
    {
        var service = new ServiceIdentity("service"); var device = DeviceIdentity.New(); var integration = new ServiceIntegration(service); integration.SetDeviceOverride(device, Enablement.Disabled);
        var store = new MemoryIntegrationStore(integration); var useCase = new SetGlobalIntegrationEnablementUseCase(store);
        await useCase.EnableAsync(service); Assert.Equal(Enablement.Disabled, (await store.LoadAsync(service))!.GetEffectiveEnablement(device));
        await useCase.DisableAsync(service); Assert.Equal(Enablement.Disabled, (await store.LoadAsync(service))!.GetEffectiveEnablement(device));
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EnableAsync(new ServiceIdentity("unknown")).AsTask());
    }

    [Fact]
    public async Task Device_override_set_and_clear_restore_global_state()
    {
        var service = new ServiceIdentity("service"); var device = DeviceIdentity.New(); var integration = new ServiceIntegration(service); integration.EnableGlobally();
        var store = new MemoryIntegrationStore(integration);
        await new SetDeviceIntegrationOverrideUseCase(store).SetAsync(service, device, false);
        Assert.Equal(Enablement.Disabled, (await store.LoadAsync(service))!.GetEffectiveEnablement(device));
        await new ClearDeviceIntegrationOverrideUseCase(store).ClearAsync(service, device);
        Assert.Equal(Enablement.Enabled, (await store.LoadAsync(service))!.GetEffectiveEnablement(device));
    }

    private sealed class MemoryDeviceStore : ILocalDeviceStore
    {
        private LocalDeviceConfiguration? value;
        public ValueTask<LocalDeviceConfiguration?> LoadAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(value);
        public ValueTask SaveAsync(LocalDeviceConfiguration configuration, CancellationToken cancellationToken = default) { value = configuration; return ValueTask.CompletedTask; }
    }

    private sealed class MemoryIntegrationStore(params ServiceIntegration[] initial) : IServiceIntegrationStore
    {
        private readonly Dictionary<ServiceIdentity, ServiceIntegration> values = initial.ToDictionary(x => x.ServiceIdentity);
        public ValueTask<ServiceIntegration?> LoadAsync(ServiceIdentity serviceIdentity, CancellationToken cancellationToken = default) => ValueTask.FromResult(values.GetValueOrDefault(serviceIdentity));
        public ValueTask<IReadOnlyList<ServiceIntegration>> LoadAllAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<IReadOnlyList<ServiceIntegration>>(values.Values.ToArray());
        public ValueTask SaveAsync(ServiceIntegration integration, CancellationToken cancellationToken = default) { values[integration.ServiceIdentity] = integration; return ValueTask.CompletedTask; }
    }

    private sealed class MemoryPublicationStore(params IntegrationPublicationState[] initial) : IIntegrationPublicationStore
    {
        private readonly Dictionary<ServiceIdentity, IntegrationPublicationState> values = initial.ToDictionary(x => x.ServiceIdentity);
        public ValueTask SaveAsync(IntegrationPublicationState state, CancellationToken cancellationToken = default) { values[state.ServiceIdentity] = state; return ValueTask.CompletedTask; }
        public ValueTask<IntegrationPublicationState> LoadAsync(ServiceIdentity serviceIdentity, CancellationToken cancellationToken = default) => ValueTask.FromResult(values.GetValueOrDefault(serviceIdentity) ?? new IntegrationPublicationState(serviceIdentity, null, PublicationRefreshObservation.NotAttempted));
    }
}
