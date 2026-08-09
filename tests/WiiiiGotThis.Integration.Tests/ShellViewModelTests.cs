using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Integrations.Reference;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class ShellViewModelTests
{
    [Fact]
    public async Task Initialize_starts_disabled_and_resolves_four_reference_capabilities()
    {
        var shell = CreateShell();
        await shell.EnsureInitializedAsync();
        Assert.NotNull(shell.CurrentDeviceIdentity); Assert.Single(shell.Integrations); Assert.Equal(4, shell.Capabilities.Count);
        Assert.All(shell.Capabilities, capability => Assert.Equal("Integration is disabled on this device.", capability.StatusText));
    }

    [Fact]
    public async Task Enable_and_device_override_commands_reload_capability_state()
    {
        var shell = CreateShell(); await shell.EnsureInitializedAsync();
        await shell.EnableGloballyCommand.ExecuteAsync(null);
        Assert.Equal(["Available", "This capability is not supported in the current client context.", "Provider is currently unavailable.", "This capability version is not supported."], shell.Capabilities.Select(x => x.StatusText));
        await shell.DisableOnThisDeviceCommand.ExecuteAsync(null);
        Assert.All(shell.Capabilities, capability => Assert.Equal("Integration is disabled on this device.", capability.StatusText));
        await shell.InheritGlobalSettingCommand.ExecuteAsync(null);
        Assert.Equal(["Available", "This capability is not supported in the current client context.", "Provider is currently unavailable.", "This capability version is not supported."], shell.Capabilities.Select(x => x.StatusText));
    }

    [Fact]
    public async Task Only_available_reference_capability_can_open()
    {
        var shell = CreateShell(); await shell.EnsureInitializedAsync(); await shell.EnableGloballyCommand.ExecuteAsync(null);
        Assert.False(shell.IsReferenceCapabilityOpen); Assert.True(shell.IsCapabilityDetailsVisible);
        var available = shell.Capabilities[0]; shell.SelectedCapability = available;
        Assert.True(available.CanOpen); await shell.OpenCapabilityCommand.ExecuteAsync(null); Assert.Same(available, shell.OpenedReferenceCapability); Assert.True(shell.IsReferenceCapabilityOpen); Assert.False(shell.IsCapabilityDetailsVisible);
        shell.BackToCatalogCommand.Execute(null); Assert.Null(shell.OpenedReferenceCapability); Assert.False(shell.IsReferenceCapabilityOpen); Assert.True(shell.IsCapabilityDetailsVisible);
        shell.SelectedCapability = shell.Capabilities[1]; Assert.False(shell.SelectedCapability.CanOpen); Assert.False(shell.OpenCapabilityCommand.CanExecute(null));
    }

    [Fact]
    public void Unavailable_status_copy_is_user_facing_and_distinct()
    {
        var identities = new[] { AvailabilityReason.Disabled, AvailabilityReason.Unknown, AvailabilityReason.Unreachable, AvailabilityReason.Incompatible, AvailabilityReason.Unsupported, AvailabilityReason.MissingPrerequisite };
        var texts = identities.Select(reason => Status(reason)).ToArray();
        Assert.All(texts, text => Assert.False(string.IsNullOrWhiteSpace(text)));
        Assert.Equal(texts.Length, texts.Distinct().Count());
    }

    private static string Status(AvailabilityReason reason)
    {
        var service = new ServiceIdentity("status"); var publication = new ServicePublication(service, "Status", [new(new("cap"), "Capability", new(1, 0))], DateTimeOffset.UtcNow);
        var facts = reason switch
        {
            AvailabilityReason.Unreachable => new CapabilityResolutionFacts(ProviderReachability.Unreachable, ContractCompatibility.Compatible, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported),
            AvailabilityReason.Incompatible => new CapabilityResolutionFacts(ProviderReachability.Reachable, ContractCompatibility.Incompatible, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported),
            AvailabilityReason.Unsupported => new CapabilityResolutionFacts(ProviderReachability.Reachable, ContractCompatibility.Compatible, CurrentContextSupport.Unsupported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported),
            AvailabilityReason.MissingPrerequisite => new CapabilityResolutionFacts(ProviderReachability.Reachable, ContractCompatibility.Compatible, CurrentContextSupport.Supported, PrerequisiteState.Missing, PresentationInvocationSupport.Supported),
            AvailabilityReason.Unknown => new CapabilityResolutionFacts(),
            _ => new CapabilityResolutionFacts()
        };
        var integration = new ServiceIntegration(service); if (reason != AvailabilityReason.Disabled) integration.EnableGlobally();
        return new CapabilityPresentationViewModel(new CapabilityCatalogEntry(service, publication.DisplayName, publication.Capabilities[0].Id, "Capability", new(1, 0), CapabilityResolver.Resolve(integration, DeviceIdentity.New(), new(service, publication.Capabilities[0].Id), facts))).StatusText;
    }

    private static ShellViewModel CreateShell()
    {
        var adapter = new ReferenceIntegrationAdapter(); var adapters = new StaticIntegrationAdapterCatalog([adapter]);
        var integrations = new MemoryIntegrationStore(); var publications = new MemoryPublicationStore();
        return new ShellViewModel(
            new EnsureCurrentDeviceUseCase(new MemoryDeviceStore()),
            new RegisterKnownIntegrationsUseCase(adapters, integrations),
            new RefreshPublicationsUseCase(adapters, publications),
            new ListServiceIntegrationsUseCase(integrations, publications),
            new SetGlobalIntegrationEnablementUseCase(integrations),
            new SetDeviceIntegrationOverrideUseCase(integrations),
            new ClearDeviceIntegrationOverrideUseCase(integrations),
            new ResolveCapabilityCatalogUseCase(adapters, integrations, publications), "Windows PC");
    }

    private sealed class MemoryDeviceStore : ILocalDeviceStore
    {
        private LocalDeviceConfiguration? value;
        public ValueTask<LocalDeviceConfiguration?> LoadAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(value);
        public ValueTask SaveAsync(LocalDeviceConfiguration configuration, CancellationToken cancellationToken = default) { value = configuration; return ValueTask.CompletedTask; }
    }
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
}
