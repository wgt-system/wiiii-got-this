using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class IntegrationSettingsUxTests
{
    [Fact]
    public void Presentation_exposes_only_contextually_relevant_enablement_actions_and_user_facing_health()
    {
        var service = new ServiceIdentity("reference-service");
        var pending = new ServiceIntegrationPresentationViewModel(new ServiceIntegrationListItem(
            service,
            "Reference Integration",
            false,
            null,
            false,
            false,
            false,
            null,
            null,
            null));

        Assert.True(pending.IsReferenceIntegration);
        Assert.True(pending.ShowEnableGloballyAction);
        Assert.False(pending.ShowDisableGloballyAction);
        Assert.True(pending.ShowEnableOnDeviceAction);
        Assert.True(pending.ShowDisableOnDeviceAction);
        Assert.False(pending.HasDeviceOverride);
        Assert.Equal("Connection not checked yet", pending.ConnectionHealthTitle);

        var stale = new ServiceIntegrationPresentationViewModel(new ServiceIntegrationListItem(
            new ServiceIdentity("vocation"),
            "Vocation",
            true,
            true,
            true,
            true,
            true,
            IntegrationRefreshStatus.AdapterFailed,
            new DateTimeOffset(2026, 8, 17, 6, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 17, 5, 0, 0, TimeSpan.Zero)));

        Assert.False(stale.IsReferenceIntegration);
        Assert.False(stale.ShowEnableGloballyAction);
        Assert.True(stale.ShowDisableGloballyAction);
        Assert.False(stale.ShowEnableOnDeviceAction);
        Assert.True(stale.ShowDisableOnDeviceAction);
        Assert.True(stale.HasDeviceOverride);
        Assert.Equal("Using last known data", stale.ConnectionHealthTitle);
        Assert.Contains("last valid", stale.ConnectionHealthDescription, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Settings_capabilities_follow_the_selected_integration_and_enablement_changes_remain_reversible()
    {
        var serviceA = new ServiceIdentity("service-a");
        var serviceB = new ServiceIdentity("service-b");
        var adapters = new StaticIntegrationAdapterCatalog([
            new TestAdapter(serviceA, "Service A", "service-a.first", "service-a.second"),
            new TestAdapter(serviceB, "Service B", "service-b.only")
        ]);
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
            "test-device");

        await shell.EnsureInitializedAsync();

        shell.SelectedIntegration = shell.Integrations.Single(item => item.ServiceIdentity == serviceA);
        Assert.Equal(["service-a.first", "service-a.second"], shell.SelectedIntegrationCapabilities.Select(item => item.CapabilityIdentity.Value));
        Assert.All(shell.SelectedIntegrationCapabilities, item => Assert.Equal(serviceA, item.ServiceIdentity));

        shell.SelectedIntegration = shell.Integrations.Single(item => item.ServiceIdentity == serviceB);
        Assert.Equal(["service-b.only"], shell.SelectedIntegrationCapabilities.Select(item => item.CapabilityIdentity.Value));
        Assert.All(shell.SelectedIntegrationCapabilities, item => Assert.Equal(serviceB, item.ServiceIdentity));

        Assert.True(shell.SelectedIntegration.ShowEnableGloballyAction);
        await shell.EnableGloballyCommand.ExecuteAsync(null);
        Assert.True(shell.SelectedIntegration!.IsGloballyEnabled);
        Assert.True(shell.SelectedIntegration.ShowDisableGloballyAction);

        await shell.DisableOnThisDeviceCommand.ExecuteAsync(null);
        Assert.Equal(false, shell.SelectedIntegration!.CurrentDeviceOverride);
        Assert.False(shell.SelectedIntegration.IsEffectivelyEnabled);
        Assert.True(shell.SelectedIntegration.HasDeviceOverride);

        await shell.InheritGlobalSettingCommand.ExecuteAsync(null);
        Assert.Null(shell.SelectedIntegration!.CurrentDeviceOverride);
        Assert.True(shell.SelectedIntegration.IsEffectivelyEnabled);
    }

    private sealed class TestAdapter : IIntegrationAdapter
    {
        private readonly ServicePublication publication;

        public TestAdapter(ServiceIdentity serviceId, string displayName, params string[] capabilityIds)
        {
            ServiceId = serviceId;
            publication = new ServicePublication(
                serviceId,
                displayName,
                capabilityIds.Select(id => new CapabilityPublication(new CapabilityIdentity(id), id, new Version(1, 0))).ToArray(),
                new DateTimeOffset(2026, 8, 17, 6, 0, 0, TimeSpan.Zero));
        }

        public ServiceIdentity ServiceId { get; }

        public ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(publication);
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
