using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Presentation;

public enum ShellSurface
{
    // Retained as the migration-compatible root value while Desktop renders the Atlas.
    Home,
    Jobs,
    Map,
    Settings
}

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly EnsureCurrentDeviceUseCase ensureCurrentDevice;
    private readonly RegisterKnownIntegrationsUseCase registerKnownIntegrations;
    private readonly RefreshPublicationsUseCase refreshPublications;
    private readonly ListServiceIntegrationsUseCase listServiceIntegrations;
    private readonly SetGlobalIntegrationEnablementUseCase setGlobalEnablement;
    private readonly SetDeviceIntegrationOverrideUseCase setDeviceOverride;
    private readonly ClearDeviceIntegrationOverrideUseCase clearDeviceOverride;
    private readonly ResolveCapabilityCatalogUseCase resolveCapabilityCatalog;
    private readonly GetVocationOpportunityOverviewUseCase? readVocationOpportunityOverview;
    private readonly GetVocationMapProjectionUseCase? readVocationMapProjection;
    private readonly BuildAtlasProjectionUseCase buildAtlasProjection = new();
    private readonly string suggestedDeviceName;
    private readonly object initializationGate = new();
    private Task? initializationTask;

    [ObservableProperty] private string currentDeviceName = "Not initialized";
    [ObservableProperty] private DeviceIdentity? currentDeviceIdentity;
    [ObservableProperty] private ServiceIntegrationPresentationViewModel? selectedIntegration;
    [ObservableProperty] private CapabilityPresentationViewModel? selectedCapability;
    [ObservableProperty] private CapabilityPresentationViewModel? openedReferenceCapability;
    [ObservableProperty] private VocationOpportunityOverviewViewModel? openedVocationOpportunityOverview;
    [ObservableProperty] private VocationMapProjectionViewModel? openedVocationMapProjection;
    [ObservableProperty] private AtlasNodePresentationViewModel? selectedAtlasNode;
    [ObservableProperty] private string atlasSearchText = string.Empty;
    [ObservableProperty] private bool atlasSettingsExpanded;
    [ObservableProperty] private string statusText = "Starting…";
    [ObservableProperty] private ShellSurface currentSurface = ShellSurface.Home;

    public ShellViewModel(
        EnsureCurrentDeviceUseCase ensureCurrentDevice,
        RegisterKnownIntegrationsUseCase registerKnownIntegrations,
        RefreshPublicationsUseCase refreshPublications,
        ListServiceIntegrationsUseCase listServiceIntegrations,
        SetGlobalIntegrationEnablementUseCase setGlobalEnablement,
        SetDeviceIntegrationOverrideUseCase setDeviceOverride,
        ClearDeviceIntegrationOverrideUseCase clearDeviceOverride,
        ResolveCapabilityCatalogUseCase resolveCapabilityCatalog,
        string suggestedDeviceName,
        GetVocationOpportunityOverviewUseCase? readVocationOpportunityOverview = null,
        GetVocationMapProjectionUseCase? readVocationMapProjection = null)
    {
        this.ensureCurrentDevice = ensureCurrentDevice;
        this.registerKnownIntegrations = registerKnownIntegrations;
        this.refreshPublications = refreshPublications;
        this.listServiceIntegrations = listServiceIntegrations;
        this.setGlobalEnablement = setGlobalEnablement;
        this.setDeviceOverride = setDeviceOverride;
        this.clearDeviceOverride = clearDeviceOverride;
        this.resolveCapabilityCatalog = resolveCapabilityCatalog;
        this.readVocationOpportunityOverview = readVocationOpportunityOverview;
        this.readVocationMapProjection = readVocationMapProjection;
        this.suggestedDeviceName = suggestedDeviceName;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        EnableGloballyCommand = new AsyncRelayCommand(EnableGloballyAsync, CanManageSelectedIntegration);
        DisableGloballyCommand = new AsyncRelayCommand(DisableGloballyAsync, CanManageSelectedIntegration);
        EnableOnThisDeviceCommand = new AsyncRelayCommand(EnableOnThisDeviceAsync, CanManageSelectedIntegration);
        DisableOnThisDeviceCommand = new AsyncRelayCommand(DisableOnThisDeviceAsync, CanManageSelectedIntegration);
        InheritGlobalSettingCommand = new AsyncRelayCommand(InheritGlobalSettingAsync, CanManageSelectedIntegration);
        OpenCapabilityCommand = new AsyncRelayCommand(OpenCapabilityAsync, CanOpenSelectedCapability);
        BackToCatalogCommand = new RelayCommand(() => OpenedReferenceCapability = null);
        ShowHomeCommand = new RelayCommand(ReturnToAtlas);
        ReturnToAtlasCommand = new RelayCommand(ReturnToAtlas);
        ShowJobsCommand = new AsyncRelayCommand(ShowJobsAsync, CanShowJobs);
        ShowMapCommand = new AsyncRelayCommand(ShowMapAsync, CanShowMap);
        ShowSettingsCommand = new RelayCommand(() => CurrentSurface = ShellSurface.Settings);
        SelectAtlasNodeCommand = new RelayCommand<AtlasNodePresentationViewModel?>(SelectAtlasNode);
        SearchAtlasCommand = new RelayCommand(SearchAtlas);
        ToggleAtlasSettingsCommand = new RelayCommand(() => AtlasSettingsExpanded = !AtlasSettingsExpanded);
    }

    public ObservableCollection<ServiceIntegrationPresentationViewModel> Integrations { get; } = [];
    public ObservableCollection<CapabilityPresentationViewModel> Capabilities { get; } = [];
    public ObservableCollection<CapabilityPresentationViewModel> SelectedIntegrationCapabilities { get; } = [];
    public ObservableCollection<AtlasNodePresentationViewModel> AtlasNodes { get; } = [];
    public ObservableCollection<AtlasConnectionPresentationViewModel> AtlasConnections { get; } = [];
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand EnableGloballyCommand { get; }
    public IAsyncRelayCommand DisableGloballyCommand { get; }
    public IAsyncRelayCommand EnableOnThisDeviceCommand { get; }
    public IAsyncRelayCommand DisableOnThisDeviceCommand { get; }
    public IAsyncRelayCommand InheritGlobalSettingCommand { get; }
    public IAsyncRelayCommand OpenCapabilityCommand { get; }
    public IRelayCommand BackToCatalogCommand { get; }
    public IRelayCommand ShowHomeCommand { get; }
    public IRelayCommand ReturnToAtlasCommand { get; }
    public IAsyncRelayCommand ShowJobsCommand { get; }
    public IAsyncRelayCommand ShowMapCommand { get; }
    public IRelayCommand ShowSettingsCommand { get; }
    public IRelayCommand<AtlasNodePresentationViewModel?> SelectAtlasNodeCommand { get; }
    public IRelayCommand SearchAtlasCommand { get; }
    public IRelayCommand ToggleAtlasSettingsCommand { get; }
    public bool IsHomeVisible => CurrentSurface == ShellSurface.Home;
    public bool IsAtlasVisible => CurrentSurface == ShellSurface.Home;
    public bool IsJobsVisible => CurrentSurface == ShellSurface.Jobs;
    public bool IsMapVisible => CurrentSurface == ShellSurface.Map;
    public bool IsSettingsVisible => CurrentSurface == ShellSurface.Settings;
    public bool IsHomeActive => IsHomeVisible;
    public bool IsJobsActive => IsJobsVisible;
    public bool IsMapActive => IsMapVisible;
    public bool IsSettingsActive => IsSettingsVisible;
    public bool IsJobsAvailable => CanShowJobs();
    public bool IsMapAvailable => CanShowMap();
    public bool IsReferenceCapabilityOpen => OpenedReferenceCapability is not null;
    public bool IsVocationOpportunityOverviewOpen => OpenedVocationOpportunityOverview is not null;
    public bool IsMapProjectionOpen => OpenedVocationMapProjection is not null;
    public bool IsCapabilityDetailsVisible => !IsReferenceCapabilityOpen && !IsVocationOpportunityOverviewOpen;
    public bool IsDesktopCapabilityDetailsVisible => !IsReferenceCapabilityOpen;
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusText);
    public bool HasSelectedAtlasNode => SelectedAtlasNode is not null;
    public bool IsSelectedAtlasCore => SelectedAtlasNode?.IsCore == true;
    public bool IsSelectedAtlasService => SelectedAtlasNode?.IsService == true;
    public bool IsSelectedAtlasCapability => SelectedAtlasNode?.IsCapability == true;

    public Task EnsureInitializedAsync()
    {
        lock (initializationGate)
            return initializationTask ??= InitializeCoreAsync();
    }

    partial void OnSelectedIntegrationChanged(ServiceIntegrationPresentationViewModel? value)
    {
        RebuildSelectedIntegrationCapabilities();
        OpenedReferenceCapability = null;
        RefreshCommandStates();
    }

    partial void OnSelectedCapabilityChanged(CapabilityPresentationViewModel? value)
    {
        OpenedReferenceCapability = null;
        RefreshCommandStates();
    }

    partial void OnSelectedAtlasNodeChanged(AtlasNodePresentationViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedAtlasNode));
        OnPropertyChanged(nameof(IsSelectedAtlasCore));
        OnPropertyChanged(nameof(IsSelectedAtlasService));
        OnPropertyChanged(nameof(IsSelectedAtlasCapability));

        if (value?.ServiceIdentity is { } serviceIdentity)
            SelectedIntegration = Integrations.FirstOrDefault(integration => integration.ServiceIdentity == serviceIdentity);
        if (value?.CapabilityIdentity is { } capabilityIdentity)
            SelectedCapability = SelectedIntegrationCapabilities.FirstOrDefault(capability => capability.CapabilityIdentity == capabilityIdentity);
    }

    partial void OnOpenedReferenceCapabilityChanged(CapabilityPresentationViewModel? value)
    {
        OnPropertyChanged(nameof(IsReferenceCapabilityOpen));
        OnPropertyChanged(nameof(IsCapabilityDetailsVisible));
        OnPropertyChanged(nameof(IsDesktopCapabilityDetailsVisible));
    }

    partial void OnOpenedVocationOpportunityOverviewChanged(VocationOpportunityOverviewViewModel? value)
    {
        OnPropertyChanged(nameof(IsVocationOpportunityOverviewOpen));
        OnPropertyChanged(nameof(IsCapabilityDetailsVisible));
    }

    partial void OnOpenedVocationMapProjectionChanged(VocationMapProjectionViewModel? value)
    {
        OnPropertyChanged(nameof(IsMapProjectionOpen));
    }

    partial void OnStatusTextChanged(string value) => OnPropertyChanged(nameof(HasStatusMessage));

    partial void OnCurrentSurfaceChanged(ShellSurface value)
    {
        OnPropertyChanged(nameof(IsHomeVisible));
        OnPropertyChanged(nameof(IsAtlasVisible));
        OnPropertyChanged(nameof(IsJobsVisible));
        OnPropertyChanged(nameof(IsMapVisible));
        OnPropertyChanged(nameof(IsSettingsVisible));
        OnPropertyChanged(nameof(IsHomeActive));
        OnPropertyChanged(nameof(IsJobsActive));
        OnPropertyChanged(nameof(IsMapActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }

    private async Task InitializeCoreAsync()
    {
        try
        {
            var device = await ensureCurrentDevice.GetOrCreateAsync(suggestedDeviceName);
            CurrentDeviceIdentity = device.DeviceIdentity;
            CurrentDeviceName = device.DisplayName;
            await registerKnownIntegrations.RegisterAsync();
            await RefreshCoreAsync();
        }
        catch
        {
            StatusText = "WGT could not load its local state.";
            throw;
        }
    }

    private async Task RefreshAsync()
    {
        await EnsureInitializedAsync();
        await RefreshCoreAsync();
    }

    private async Task RefreshCoreAsync()
    {
        if (CurrentDeviceIdentity is null) return;
        try
        {
            var results = await refreshPublications.RefreshAsync();
            var failures = results.Where(x => x.Status != IntegrationRefreshStatus.Refreshed).ToArray();
            StatusText = failures.Length == 0
                ? string.Empty
                : "Some integration publications could not be refreshed.";
            await ReloadStateAsync();
        }
        catch
        {
            StatusText = "WGT could not refresh its local state.";
            throw;
        }
    }

    private async Task ReloadStateAsync()
    {
        if (CurrentDeviceIdentity is null) return;
        var selectedServiceIdentity = SelectedIntegration?.ServiceIdentity;
        var selectedCapabilityIdentity = SelectedCapability?.CapabilityIdentity;
        var selectedAtlasNodeId = SelectedAtlasNode?.NodeId;
        var integrations = await listServiceIntegrations.ListAsync(CurrentDeviceIdentity);
        Replace(Integrations, integrations.Select(item => new ServiceIntegrationPresentationViewModel(item)));
        SelectedIntegration = selectedServiceIdentity is not null
            ? Integrations.FirstOrDefault(x => x.ServiceIdentity == selectedServiceIdentity) ?? Integrations.FirstOrDefault()
            : Integrations.FirstOrDefault();

        var entries = await resolveCapabilityCatalog.ResolveAsync(CurrentDeviceIdentity);
        Replace(Capabilities, entries.Select(x => new CapabilityPresentationViewModel(x)));
        RebuildSelectedIntegrationCapabilities(selectedCapabilityIdentity);
        RebuildAtlas(integrations, entries, selectedAtlasNodeId);
        if (OpenedReferenceCapability is not null)
            OpenedReferenceCapability = SelectedIntegrationCapabilities.FirstOrDefault(x => x.CapabilityIdentity == OpenedReferenceCapability.CapabilityIdentity && x.CanOpen);
        RefreshCommandStates();
    }

    private async Task EnableGloballyAsync() { if (SelectedIntegration is { } selected) { await setGlobalEnablement.EnableAsync(selected.ServiceIdentity); await ReloadStateAsync(); } }
    private async Task DisableGloballyAsync() { if (SelectedIntegration is { } selected) { await setGlobalEnablement.DisableAsync(selected.ServiceIdentity); await ReloadStateAsync(); } }
    private async Task EnableOnThisDeviceAsync() { if (SelectedIntegration is { } selected && CurrentDeviceIdentity is { } device) { await setDeviceOverride.SetAsync(selected.ServiceIdentity, device, true); await ReloadStateAsync(); } }
    private async Task DisableOnThisDeviceAsync() { if (SelectedIntegration is { } selected && CurrentDeviceIdentity is { } device) { await setDeviceOverride.SetAsync(selected.ServiceIdentity, device, false); await ReloadStateAsync(); } }
    private async Task InheritGlobalSettingAsync() { if (SelectedIntegration is { } selected && CurrentDeviceIdentity is { } device) { await clearDeviceOverride.ClearAsync(selected.ServiceIdentity, device); await ReloadStateAsync(); } }

    private async Task OpenCapabilityAsync()
    {
        if (SelectedCapability is not { CanOpen: true } capability) return;
        if (string.Equals(capability.CapabilityIdentity.Value, "reference.available", StringComparison.Ordinal))
        {
            OpenedReferenceCapability = capability;
            return;
        }

        if (string.Equals(capability.CapabilityIdentity.Value, "vocation.opportunity_overview", StringComparison.Ordinal))
        {
            await ShowJobsAsync();
            return;
        }

        if (string.Equals(capability.CapabilityIdentity.Value, "vocation.map_projection", StringComparison.Ordinal))
            await ShowMapAsync();
    }

    private async Task ShowJobsAsync()
    {
        if (!CanShowJobs()) return;
        await LoadVocationOverviewAsync();
        CurrentSurface = ShellSurface.Jobs;
    }

    private async Task ShowMapAsync()
    {
        if (!CanShowMap()) return;
        await LoadMapProjectionAsync();
        CurrentSurface = ShellSurface.Map;
    }

    private async Task LoadVocationOverviewAsync()
    {
        if (readVocationOpportunityOverview is null) return;
        OpenedReferenceCapability = null;
        OpenedVocationOpportunityOverview ??= new VocationOpportunityOverviewViewModel(readVocationOpportunityOverview);
        await OpenedVocationOpportunityOverview.RefreshAsync();
    }

    private async Task LoadMapProjectionAsync()
    {
        if (readVocationMapProjection is null) return;
        OpenedReferenceCapability = null;
        OpenedVocationMapProjection ??= new VocationMapProjectionViewModel(readVocationMapProjection);
        await OpenedVocationMapProjection.RefreshAsync();
    }

    private void RebuildSelectedIntegrationCapabilities(CapabilityIdentity? preferredCapability = null)
    {
        var selectedServiceIdentity = SelectedIntegration?.ServiceIdentity;
        CapabilityPresentationViewModel[] filtered = selectedServiceIdentity is null
            ? []
            : Capabilities.Where(capability => capability.ServiceIdentity == selectedServiceIdentity).ToArray();
        Replace(SelectedIntegrationCapabilities, filtered);

        SelectedCapability = preferredCapability is not null
            ? SelectedIntegrationCapabilities.FirstOrDefault(capability => capability.CapabilityIdentity == preferredCapability) ?? SelectedIntegrationCapabilities.FirstOrDefault()
            : SelectedIntegrationCapabilities.FirstOrDefault();
    }

    private void RebuildAtlas(
        IReadOnlyCollection<ServiceIntegrationListItem> integrations,
        IReadOnlyCollection<CapabilityCatalogEntry> capabilities,
        string? preferredNodeId)
    {
        var projection = buildAtlasProjection.Build(integrations, capabilities);
        var layout = AtlasPresentationLayoutBuilder.Build(projection);
        Replace(AtlasNodes, layout.Nodes);
        Replace(AtlasConnections, layout.Connections);
        SelectedAtlasNode = preferredNodeId is not null
            ? AtlasNodes.FirstOrDefault(node => string.Equals(node.NodeId, preferredNodeId, StringComparison.Ordinal))
            : null;
    }

    private void SelectAtlasNode(AtlasNodePresentationViewModel? node) => SelectedAtlasNode = node;

    private void SearchAtlas()
    {
        var query = AtlasSearchText.Trim();
        if (query.Length == 0) return;
        SelectedAtlasNode = AtlasNodes.FirstOrDefault(node =>
            node.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            node.NodeId.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private void ReturnToAtlas()
    {
        CurrentSurface = ShellSurface.Home;
        OpenedReferenceCapability = null;
    }

    private bool CanManageSelectedIntegration() => SelectedIntegration is not null && CurrentDeviceIdentity is not null;
    private bool CanShowJobs() => readVocationOpportunityOverview is not null && Integrations.Any(integration => integration.ServiceIdentity.Value == "vocation" && integration.IsEffectivelyEnabled);
    private bool CanShowMap() => readVocationMapProjection is not null && Integrations.Any(integration => integration.ServiceIdentity.Value == "vocation" && integration.IsEffectivelyEnabled);
    private bool CanOpenSelectedCapability()
    {
        var selected = SelectedCapability;
        if (selected?.CanOpen != true) return false;
        if (string.Equals(selected.CapabilityIdentity.Value, "vocation.opportunity_overview", StringComparison.Ordinal))
            return readVocationOpportunityOverview is not null;
        if (string.Equals(selected.CapabilityIdentity.Value, "vocation.map_projection", StringComparison.Ordinal))
            return readVocationMapProjection is not null;
        return true;
    }

    private void RefreshCommandStates()
    {
        EnableGloballyCommand.NotifyCanExecuteChanged();
        DisableGloballyCommand.NotifyCanExecuteChanged();
        EnableOnThisDeviceCommand.NotifyCanExecuteChanged();
        DisableOnThisDeviceCommand.NotifyCanExecuteChanged();
        InheritGlobalSettingCommand.NotifyCanExecuteChanged();
        OpenCapabilityCommand.NotifyCanExecuteChanged();
        ShowJobsCommand.NotifyCanExecuteChanged();
        ShowMapCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsJobsAvailable));
        OnPropertyChanged(nameof(IsMapAvailable));
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
    }
}
